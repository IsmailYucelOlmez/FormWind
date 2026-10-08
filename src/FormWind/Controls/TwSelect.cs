using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// Stillenebilir açılır liste: seçili metin, ince bir ayraç ve aşağı ok. Tıklanınca tema renderer'lı bir menü açar.
/// Kutu temel class'larla (<c>rounded-full border px-3 text-xs</c>), menü <c>menu:</c> ve <c>item:</c> ile stillenir.
/// Native ComboBox'ın kutusu stillenemediği için yerine kullanılır; klavye ile ↑/↓ seçimi değiştirir, Enter/Boşluk menüyü açar.
/// </summary>
[ToolboxItem(true)]
[DefaultEvent(nameof(SelectedIndexChanged))]
[DefaultProperty(nameof(Items))]
[Description("Stillenebilir açılır liste.")]
public class TwSelect : Control, ITwChrome
{
    private readonly List<string> _items = new List<string>();
    private TwChrome _chrome = TwChrome.None;
    private int _selectedIndex = -1;
    private ContextMenuStrip? _menu;

    public TwSelect()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        Padding = new Padding(10, 4, 8, 4);
    }

    public event EventHandler? SelectedIndexChanged;

    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design", typeof(UITypeEditor))]
    public List<string> Items => _items;

    [Category("Behavior"), DefaultValue(-1)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var index = value < -1 || value >= _items.Count ? -1 : value;
            if (index == _selectedIndex) return;
            _selectedIndex = index;
            Invalidate();
            if (AutoSize && SizeToSelection) Parent?.PerformLayout(this, nameof(SelectedIndex));
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? SelectedItem => _selectedIndex >= 0 ? _items[_selectedIndex] : null;

    /// <summary>Son açılan menü (açılmadıysa <c>null</c>).</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ContextMenuStrip? DropDownMenu => _menu;

    /// <summary>Menü renkleri; FormWind <c>menu:</c> / <c>item:</c> class'larından doldurur.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TwMenuColors MenuColors { get; set; } = new TwMenuColors();

    [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), DefaultValue(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public override bool AutoSize
    {
        get => base.AutoSize;
        set => base.AutoSize = value;
    }

    protected override Size DefaultSize => new Size(96, 30);

    private int ChevronArea => (int)Math.Round(Font.Height * 1.6f);

    void ITwChrome.SetChrome(TwChrome chrome)
    {
        if (_chrome.Equals(chrome)) return;
        _chrome = chrome;
        Invalidate();
    }

    /// <summary>AutoSize genişliği: seçili öğeye göre (true) ya da en uzun öğeye göre (false, native ComboBox gibi).</summary>
    [Category("Layout"), DefaultValue(true)]
    public bool SizeToSelection { get; set; } = true;

    public override Size GetPreferredSize(Size proposedSize)
    {
        int text = 0;
        if (SizeToSelection)
        {
            text = TextRenderer.MeasureText(SelectedItem ?? "", Font, Size.Empty, TextFormatFlags.NoPadding).Width;
        }
        else
        {
            foreach (var item in _items)
                text = Math.Max(text, TextRenderer.MeasureText(item, Font, Size.Empty, TextFormatFlags.NoPadding).Width);
        }
        return new Size(Padding.Horizontal + text + ChevronArea + 1, Padding.Vertical + Font.Height);
    }

    public void ShowDropDown()
    {
        if (_items.Count == 0) return;
        _menu?.Dispose();
        _menu = new ContextMenuStrip { Renderer = new TwToolStripRenderer(MenuColors), ShowImageMargin = false, ShowCheckMargin = true };
        _menu.Font = Font;
        for (int i = 0; i < _items.Count; i++)
        {
            int index = i;
            var menuItem = new ToolStripMenuItem(_items[i]) { Checked = i == _selectedIndex };
            menuItem.Click += (_, _) => SelectedIndex = index;
            _menu.Items.Add(menuItem);
        }
        _menu.MinimumSize = new Size(Width, 0);
        _menu.Show(this, new Point(0, Height + 2));
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Focus();
        ShowDropDown();
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Up or Keys.Down || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.KeyCode)
        {
            case Keys.Down: SelectedIndex = Math.Min(_items.Count - 1, _selectedIndex + 1); e.Handled = true; break;
            case Keys.Up: SelectedIndex = Math.Max(0, _selectedIndex - 1); e.Handled = true; break;
            case Keys.Enter:
            case Keys.Space: ShowDropDown(); e.Handled = true; break;
        }
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (AutoSize) Parent?.PerformLayout(this, nameof(Font));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _menu?.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var bounds = ClientRectangle;
        TwPainter.PaintParentBackground(this, g);
        TwPainter.FillSurface(g, bounds, _chrome.Radius, BackColor, _chrome.Gradient);
        TwPainter.StrokeChrome(g, bounds, _chrome);

        int chevron = ChevronArea;
        var fore = Enabled ? ForeColor : Color.FromArgb(128, ForeColor);
        var textBounds = new Rectangle(Padding.Left, 0, Math.Max(0, Width - Padding.Left - chevron), Height);
        TextRenderer.DrawText(g, SelectedItem ?? "", Font, textBounds, fore,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        // Ayraç: kenarlık rengiyle, yüksekliğin yarısı kadar.
        var dividerColor = _chrome.HasBorder ? _chrome.BorderColor : Color.FromArgb(80, fore);
        int x = Width - chevron;
        using (var pen = new Pen(dividerColor, Math.Max(1f, DeviceDpi / 96f)))
            g.DrawLine(pen, x, Height * 0.28f, x, Height * 0.72f);

        if (TwTheme.Current.TryGetIcon("chevron-down", out var glyph))
        {
            int em = TwIconRenderer.EmPixels(Font.SizeInPoints, DeviceDpi);
            TwIconRenderer.Draw(g, glyph, TwTheme.Current.IconFontFamily, em, new Rectangle(x, 0, chevron, Height), fore);
        }
    }
}
