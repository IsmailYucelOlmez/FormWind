using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// DataGridView için boş tablo ve yükleniyor durumları:
/// <code>
/// TwGrid.SetEmptyText(grid, "Kayıt bulunamadı");
/// TwGrid.SetLoading(grid, true);   // veri gelene kadar iskelet satırlar
/// </code>
/// FormWind class'ı verilmiş grid'lerde başlıktaki sıralama oku ikonla çizilir ve düzenleme kontrolleri hücre stiline uyar.
/// </summary>
public static class TwGrid
{
    /// <summary>Satır yokken ortada gösterilen metin (<c>null</c> = gösterme). İkon adı <see cref="SetEmptyIcon"/> ile değişir.</summary>
    public static void SetEmptyText(DataGridView grid, string? text)
    {
        TwGridDecorator.For(grid).EmptyText = text;
        grid.Invalidate();
    }

    public static string? GetEmptyText(DataGridView grid) => TwGridDecorator.TryGet(grid)?.EmptyText;

    /// <summary>Boş durum ikonu (varsayılan <c>search</c>; <c>null</c> = ikonsuz).</summary>
    public static void SetEmptyIcon(DataGridView grid, string? icon)
    {
        TwGridDecorator.For(grid).EmptyIcon = icon;
        grid.Invalidate();
    }

    /// <summary>
    /// Yükleniyor durumu: satır yoksa iskelet satırlar çizilir; satır varsa üzerleri soluklaştırılıp "Yükleniyor…" yazılır.
    /// </summary>
    public static void SetLoading(DataGridView grid, bool loading)
    {
        TwGridDecorator.For(grid).Loading = loading;
        grid.Invalidate();
    }

    public static bool IsLoading(DataGridView grid) => TwGridDecorator.TryGet(grid)?.Loading == true;

    /// <summary>Yükleniyor metni (varsayılan "Yükleniyor…").</summary>
    public static void SetLoadingText(DataGridView grid, string text) => TwGridDecorator.For(grid).LoadingText = text;

    /// <summary>
    /// Başlıklarda süzme düğmesi: tıklanınca kolonun farklı değerleri onay kutularıyla listelenir. Grid'in veri kaynağı
    /// <see cref="ITwFilterableList"/> olmalı (örn. <see cref="TwSortableBindingList{T}"/>); yalnızca <c>DataPropertyName</c>'i olan
    /// kolonlarda görünür. Süzülen kolonun düğmesi <c>accent-*</c> rengindedir.
    /// </summary>
    public static void EnableFilterMenu(DataGridView grid, bool enable = true)
    {
        TwGridDecorator.For(grid).FilterMenu = enable;
        grid.Invalidate();
    }

    /// <summary>Başlığa sağ tıklayınca kolonları gösterip gizleme menüsü (en az bir kolon görünür kalır).</summary>
    public static void EnableColumnChooser(DataGridView grid, bool enable = true) => TwGridDecorator.For(grid).ColumnChooser = enable;

    /// <summary>Açık süzme ya da kolon menüsü (yoksa <c>null</c>).</summary>
    public static ContextMenuStrip? OpenMenu(DataGridView grid) => TwGridDecorator.TryGet(grid)?.Menu;

    /// <summary>Kolonun süzme menüsünü kodla açar (klavye kısayolu ya da testler için).</summary>
    public static void ShowFilterMenu(DataGridView grid, DataGridViewColumn column) => TwGridDecorator.For(grid).ShowFilterMenu(column);

    /// <summary>Kolon seçme menüsünü kodla açar.</summary>
    public static void ShowColumnChooser(DataGridView grid) => TwGridDecorator.For(grid).ShowColumnChooser(grid.PointToClient(Control.MousePosition));

    /// <summary>Kullanıcıya görünen (yeni satır hariç) satır sayısı.</summary>
    public static int DataRowCount(DataGridView grid) => grid.Rows.Count - (grid.AllowUserToAddRows && grid.Rows.Count > 0 ? 1 : 0);
}

internal sealed class TwGridDecorator
{
    private static readonly ConditionalWeakTable<DataGridView, TwGridDecorator> Table = new ConditionalWeakTable<DataGridView, TwGridDecorator>();

    private readonly DataGridView _grid;
    private bool _styled;

    private TwGridDecorator(DataGridView grid)
    {
        _grid = grid;
        grid.Paint += OnPaint;
        grid.CellPainting += OnCellPainting;
        grid.EditingControlShowing += OnEditingControlShowing;
        grid.RowsAdded += (_, e) => InvalidateOverlay(TwGrid.DataRowCount(grid) == e.RowCount);
        grid.RowsRemoved += (_, _) => InvalidateOverlay(TwGrid.DataRowCount(grid) == 0);
        grid.CellMouseDown += OnCellMouseDown;
        grid.ColumnHeaderMouseClick += OnColumnHeaderMouseClick;
        grid.Disposed += (_, _) => Menu?.Dispose();
    }

    // Boş durum metni yalnızca satır sayısı sıfırdan çıkınca/sıfıra inince, yükleniyor örtüsü her değişimde yeniden
    // çizilir. Her RowsAdded'da tüm grid'i geçersiz kılmak, satırları parça parça ekleyen grid'lerde (VirtualMode)
    // yükleme boyunca yüzlerce tam yeniden çizim demekti.
    private void InvalidateOverlay(bool emptyChanged)
    {
        if (Loading || (emptyChanged && !string.IsNullOrEmpty(EmptyText))) _grid.Invalidate();
    }

    public bool FilterMenu { get; set; }

    public bool ColumnChooser { get; set; }

    public ContextMenuStrip? Menu { get; private set; }

    private ITwFilterableList? FilterSource => _grid.DataSource switch
    {
        ITwFilterableList list => list,
        BindingSource { List: ITwFilterableList list } => list,
        _ => null,
    };

    private bool CanFilter(DataGridViewColumn column) =>
        FilterMenu && FilterSource != null && !string.IsNullOrEmpty(column.DataPropertyName);

    /// <summary>Başlıktaki süzme düğmesinin yeri.</summary>
    internal Rectangle FilterIconBounds(int columnIndex)
    {
        var cell = _grid.GetCellDisplayRectangle(columnIndex, -1, false);
        var style = _grid.ColumnHeadersDefaultCellStyle;
        var font = style.Font ?? _grid.Font;
        int em = TwIconRenderer.EmPixels(font.SizeInPoints, 96);
        int right = cell.Right - Math.Max(4, style.Padding.Right);
        return new Rectangle(right - em, cell.Y, em, cell.Height);
    }

    private void OnCellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.RowIndex != -1 || e.ColumnIndex < 0 || e.Button != MouseButtons.Left) return;
        var column = _grid.Columns[e.ColumnIndex];
        if (!CanFilter(column)) return;
        var cell = _grid.GetCellDisplayRectangle(e.ColumnIndex, -1, false);
        var point = new Point(cell.X + e.X, cell.Y + e.Y);
        if (!FilterIconBounds(e.ColumnIndex).Contains(point)) return;

        // Düğmeye tıklama sıralamasın: bu tık için sıralama kapatılır.
        var mode = column.SortMode;
        column.SortMode = DataGridViewColumnSortMode.Programmatic;
        _grid.BeginInvoke((Action)(() => column.SortMode = mode));
        ShowFilterMenu(column);
    }

    private void OnColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (!ColumnChooser || e.Button != MouseButtons.Right) return;
        var cell = _grid.GetCellDisplayRectangle(e.ColumnIndex, -1, false);
        ShowColumnChooser(new Point(cell.X + e.X, cell.Y + e.Y));
    }

    private ContextMenuStrip NewMenu()
    {
        Menu?.Dispose();
        bool dark = TwGridState.IsDark(_grid);
        var colors = dark
            ? new TwMenuColors
            {
                Back = ColorTranslator.FromHtml("#1f2937"), MenuBack = ColorTranslator.FromHtml("#1f2937"), Fore = ColorTranslator.FromHtml("#e5e7eb"),
                MenuBorder = ColorTranslator.FromHtml("#374151"), ItemHoverBack = ColorTranslator.FromHtml("#374151"), ItemHoverFore = Color.White,
                Separator = ColorTranslator.FromHtml("#374151"), Accent = ColorTranslator.FromHtml("#60a5fa"),
            }
            : new TwMenuColors { Accent = Accent };
        Menu = new ContextMenuStrip { Renderer = new TwToolStripRenderer(colors), ShowImageMargin = false, ShowCheckMargin = true, Font = _grid.Font };
        return Menu;
    }

    internal void ShowFilterMenu(DataGridViewColumn column)
    {
        var source = FilterSource;
        if (source is null || string.IsNullOrEmpty(column.DataPropertyName)) return;
        var property = column.DataPropertyName;
        var values = source.DistinctValues(property);
        var allowed = source.GetColumnFilter(property);

        var menu = NewMenu();
        var all = new ToolStripMenuItem("(Tümünü seç)") { Checked = allowed is null };
        menu.Items.Add(all);
        menu.Items.Add(new ToolStripSeparator());
        var items = new List<ToolStripMenuItem>();
        foreach (var value in values)
        {
            var item = new ToolStripMenuItem(value) { CheckOnClick = true, Checked = allowed is null || allowed.Contains(value), Tag = value };
            items.Add(item);
            menu.Items.Add(item);
        }
        menu.Items.Add(new ToolStripSeparator());
        var clear = new ToolStripMenuItem("Süzmeyi kaldır") { Enabled = allowed != null };
        menu.Items.Add(clear);

        void Apply()
        {
            var selected = items.Where(i => i.Checked).Select(i => (string)i.Tag!).ToList();
            source.SetColumnFilter(property, selected.Count == items.Count ? null : selected);
            all.Checked = selected.Count == items.Count;
            clear.Enabled = selected.Count != items.Count;
            _grid.Invalidate();
        }

        foreach (var item in items) item.CheckedChanged += (_, _) => Apply();
        all.Click += (_, _) =>
        {
            bool check = !all.Checked;
            foreach (var item in items) item.Checked = check;
        };
        clear.Click += (_, _) =>
        {
            source.SetColumnFilter(property, null);
            _grid.Invalidate();
            menu.Close();
        };
        // Onay kutularına tıklamak menüyü kapatmasın.
        menu.Closing += (_, e) =>
        {
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked && clear.Selected == false) e.Cancel = true;
        };

        var icon = FilterIconBounds(column.Index);
        menu.Show(_grid, new Point(icon.Left, icon.Bottom));
    }

    internal void ShowColumnChooser(Point location)
    {
        var menu = NewMenu();
        foreach (DataGridViewColumn column in _grid.Columns)
        {
            var title = string.IsNullOrEmpty(column.HeaderText) ? column.Name : column.HeaderText;
            var item = new ToolStripMenuItem(title) { CheckOnClick = true, Checked = column.Visible, Tag = column };
            item.CheckedChanged += (_, _) =>
            {
                // Son görünen kolon gizlenemez.
                if (!item.Checked && _grid.Columns.GetColumnCount(DataGridViewElementStates.Visible) <= 1)
                {
                    item.Checked = true;
                    return;
                }
                column.Visible = item.Checked;
            };
            menu.Items.Add(item);
        }
        menu.Closing += (_, e) =>
        {
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked) e.Cancel = true;
        };
        menu.Show(_grid, location);
    }

    public static TwGridDecorator For(DataGridView grid) => Table.GetValue(grid, g => new TwGridDecorator(g));

    public static TwGridDecorator? TryGet(DataGridView grid) => Table.TryGetValue(grid, out var decorator) ? decorator : null;

    public string? EmptyText { get; set; }

    public string? EmptyIcon { get; set; } = "search";

    public bool Loading { get; set; }

    public string LoadingText { get; set; } = "Yükleniyor…";

    /// <summary>Sıralama okunun ve düzenleme kontrollerinin rengi; FormWind class'ları verildiğinde açılır.</summary>
    public Color Accent { get; set; } = ColorTranslator.FromHtml("#2563eb");

    /// <summary>Başlık sıralama oku ikonla çizilsin mi (class'ı olan grid'lerde açık).</summary>
    public bool Styled
    {
        get => _styled;
        set
        {
            if (_styled == value) return;
            _styled = value;
            _grid.Invalidate();
        }
    }

    private float Scale => TwDpi.DeviceDpi(_grid) / 96f;

    private Color Muted
    {
        get
        {
            var fore = _grid.DefaultCellStyle.ForeColor;
            var back = _grid.BackgroundColor;
            return Color.FromArgb((fore.R + back.R) / 2, (fore.G + back.G) / 2, (fore.B + back.B) / 2);
        }
    }

    /// <summary>Satırların altındaki boş alan (başlık hariç).</summary>
    internal Rectangle BodyBounds
    {
        get
        {
            var rect = _grid.DisplayRectangle;
            int top = rect.Top + (_grid.ColumnHeadersVisible ? _grid.ColumnHeadersHeight : 0);
            return Rectangle.FromLTRB(rect.Left, top, rect.Right, Math.Max(top, rect.Bottom));
        }
    }

    private void OnPaint(object? sender, PaintEventArgs e)
    {
        int rows = TwGrid.DataRowCount(_grid);
        var body = BodyBounds;
        if (body.Height <= 0) return;

        bool skeleton = Loading && rows == 0;
        UpdatePulse(skeleton);
        if (skeleton) PaintSkeleton(e.Graphics, body);
        else if (Loading) PaintBusyOverlay(e.Graphics, body);
        else if (rows == 0 && !string.IsNullOrEmpty(EmptyText)) PaintEmpty(e.Graphics, body);
    }

    private void PaintEmpty(Graphics g, Rectangle body)
    {
        var font = _grid.DefaultCellStyle.Font ?? _grid.Font;
        var color = Muted;
        var textSize = TextRenderer.MeasureText(g, EmptyText, font, new Size(body.Width - 32, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        int em = TwIconRenderer.EmPixels(font.SizeInPoints * 2f, TwDpi.DeviceDpi(_grid));
        bool icon = TwTheme.Current.HasIcon(EmptyIcon);
        int gap = icon ? (int)(10 * Scale) : 0;
        int total = (icon ? em : 0) + gap + textSize.Height;
        int y = body.Top + Math.Max((int)(16 * Scale), (body.Height - total) / 2);

        if (icon) TwIconRenderer.DrawIcon(g, EmptyIcon, em, new Rectangle(body.Left, y, body.Width, em), color);
        TextRenderer.DrawText(g, EmptyText, font, new Rectangle(body.Left + 16, y + (icon ? em : 0) + gap, body.Width - 32, textSize.Height), color,
            TextFormatFlags.WordBreak | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
    }

    private void PaintSkeleton(Graphics g, Rectangle body)
    {
        bool dark = TwGridState.IsDark(_grid);
        var bar = TwMotion.Blend(_grid.BackgroundColor, ColorTranslator.FromHtml(dark ? "#374151" : "#e5e7eb"), TwDataList.PulseFactor(_grid));
        int rowHeight = Math.Max((int)(24 * Scale), _grid.RowTemplate.Height);
        int barHeight = Math.Max(4, (int)Math.Round(8 * Scale));
        int pad = (int)(12 * Scale);
        var columns = VisibleColumnBounds(body);
        if (columns.Count == 0) columns.Add((body.Left, body.Width));

        int row = 0;
        for (int y = body.Top; y + rowHeight <= body.Bottom + rowHeight / 2 && row < 50; y += rowHeight, row++)
        {
            for (int c = 0; c < columns.Count; c++)
            {
                var (x, width) = columns[c];
                // Sabit ama satırdan satıra değişen genişlikler (rastgele görünüm, her çizimde aynı).
                float ratio = 0.45f + ((row * 7 + c * 13) % 10) * 0.05f;
                int w = Math.Max(barHeight, (int)((width - pad * 2) * ratio));
                if (width - pad * 2 <= 0) continue;
                TwPainter.FillRounded(g, new Rectangle(x + pad, y + (rowHeight - barHeight) / 2, w, barHeight), barHeight / 2f, bar);
            }
        }
    }

    private Timer? _pulse;

    private void UpdatePulse(bool active)
    {
        if (active && TwMotion.CanAnimate(_grid))
        {
            if (_pulse is null)
            {
                _pulse = new Timer { Interval = 50 };
                _pulse.Tick += (_, _) => _grid.Invalidate(BodyBounds);
                _grid.Disposed += (_, _) => _pulse.Dispose();
            }
            _pulse.Start();
        }
        else _pulse?.Stop();
    }

    private void PaintBusyOverlay(Graphics g, Rectangle body)
    {
        using (var veil = new SolidBrush(Color.FromArgb(150, _grid.BackgroundColor)))
            g.FillRectangle(veil, body);
        var font = _grid.DefaultCellStyle.Font ?? _grid.Font;
        TextRenderer.DrawText(g, LoadingText, font, body, _grid.DefaultCellStyle.ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    private List<(int X, int Width)> VisibleColumnBounds(Rectangle body)
    {
        var result = new List<(int, int)>();
        int x = body.Left + (_grid.RowHeadersVisible ? _grid.RowHeadersWidth : 0) - _grid.HorizontalScrollingOffset;
        foreach (DataGridViewColumn column in _grid.Columns)
        {
            if (!column.Visible) continue;
            if (x + column.Width > body.Left && x < body.Right) result.Add((x, column.Width));
            x += column.Width;
        }
        return result;
    }

    // Başlıkta sıralama oku: native üçgen yerine tema ikonu, metnin yanında.
    private void OnCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex != -1 || e.ColumnIndex < 0 || e.CellStyle is null || e.Graphics is null) return;
        var column = _grid.Columns[e.ColumnIndex];
        var direction = _styled ? column.HeaderCell.SortGlyphDirection : SortOrder.None;
        bool filter = CanFilter(column);
        if (direction == SortOrder.None && !filter) return;
        if (direction == SortOrder.None)
        {
            // Yalnızca süzme düğmesi: DataGridView başlığı çizer, düğme üstüne eklenir.
            e.Paint(e.ClipBounds, DataGridViewPaintParts.All);
            PaintFilterIcon(e.Graphics, column);
            e.Handled = true;
            return;
        }

        e.PaintBackground(e.ClipBounds, false);
        var style = e.CellStyle;
        var font = style.Font ?? _grid.Font;
        int em = TwIconRenderer.EmPixels(font.SizeInPoints, TwDpi.DeviceDpi(_grid));
        var content = new Rectangle(e.CellBounds.X + style.Padding.Left + 2, e.CellBounds.Y + style.Padding.Top,
            Math.Max(0, e.CellBounds.Width - style.Padding.Horizontal - 4), Math.Max(0, e.CellBounds.Height - style.Padding.Vertical));
        int gap = (int)(4 * Scale);
        var text = e.FormattedValue as string ?? column.HeaderText;
        var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
        // Ölçüm ellipsis'siz yapılır; çizimde 1–2 px yuvarlama farkı metni kesmesin diye pay bırakılır.
        int measured = TextRenderer.MeasureText(e.Graphics, text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width + 2;
        if (filter) content.Width = Math.Max(0, content.Width - em - gap);
        var textWidth = Math.Min(measured, Math.Max(0, content.Width - em - gap));

        // Metin + ok birlikte hücre hizalamasına göre yerleşir.
        var block = TwCellPainting.Align(content, new Size(textWidth + gap + em, content.Height), style.Alignment);
        TextRenderer.DrawText(e.Graphics, text, font, new Rectangle(block.X, content.Y, textWidth, content.Height), style.ForeColor, flags);
        TwIconRenderer.DrawIcon(e.Graphics, direction == SortOrder.Ascending ? "arrow-up" : "arrow-down", (int)(em * 0.85f),
            new Rectangle(block.X + textWidth + gap, content.Y, em, content.Height), Accent);
        if (filter) PaintFilterIcon(e.Graphics, column);
        e.Handled = true;
    }

    private void PaintFilterIcon(Graphics g, DataGridViewColumn column)
    {
        var bounds = FilterIconBounds(column.Index);
        bool active = FilterSource?.GetColumnFilter(column.DataPropertyName) != null;
        var fore = _grid.ColumnHeadersDefaultCellStyle.ForeColor;
        var color = active ? Accent : Color.FromArgb(150, fore);
        TwIconRenderer.DrawIcon(g, "filter", (int)(bounds.Width * 0.85f), bounds, color);
    }

    // Düzenleme kontrolleri: kenarlıksız, hücre renkleriyle; koyu temada koyu kaydırma çubukları.
    private void OnEditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (!_styled || e.Control is null) return;
        var style = e.CellStyle;
        bool dark = TwGridState.IsDark(_grid);
        switch (e.Control)
        {
            case TextBox box:
                box.BorderStyle = BorderStyle.None;
                box.BackColor = style.BackColor;
                box.ForeColor = style.ForeColor;
                break;
            case ComboBox combo:
                combo.FlatStyle = FlatStyle.Flat;
                combo.BackColor = style.BackColor;
                combo.ForeColor = style.ForeColor;
                break;
        }
        if (e.Control.Parent != null) e.Control.Parent.BackColor = style.BackColor;
        TwNativeTheme.Apply(e.Control, dark);
    }
}
