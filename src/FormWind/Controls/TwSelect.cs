using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// Stillenebilir açılır liste: seçili metin, ince bir ayraç ve aşağı ok. Tıklanınca tema renderer'lı bir menü açar.
/// Kutu temel class'larla (<c>rounded-full border px-3 text-xs</c>), menü <c>menu:</c> ve <c>item:</c> ile stillenir.
/// Native ComboBox'ın kutusu stillenemediği için yerine kullanılır; klavye ile ↑/↓ seçimi değiştirir, Enter/Boşluk menüyü açar,
/// harf yazmak o harflerle başlayan öğeyi seçer. <see cref="Searchable"/> açıksa menünün üstünde bir arama kutusu çıkar.
/// </summary>
[ToolboxItem(true)]
[DefaultEvent(nameof(SelectedIndexChanged))]
[DefaultProperty(nameof(Items))]
[Description("Stylable drop-down list.")]
public class TwSelect : Control, ITwChrome
{
    private readonly List<string> _items = new List<string>();
    private TwChrome _chrome = TwChrome.None;
    private int _selectedIndex = -1;
    private ContextMenuStrip? _menu;
    private ToolStripTextBox? _search;
    private string _typeAhead = "";
    private DateTime _typeAheadTime;

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
            AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? SelectedItem => _selectedIndex >= 0 ? _items[_selectedIndex] : null;

    /// <summary>Menünün üstünde, öğeleri yazdıkça süzen bir arama kutusu gösterir (uzun listeler için).</summary>
    [Category("Behavior"), DefaultValue(false)]
    public bool Searchable { get; set; }

    private string? _searchPlaceholder;

    /// <summary>Arama kutusunun ipucu; atanmamışsa <see cref="TwTexts.Search"/>.</summary>
    [Category("Appearance"), Localizable(true)]
    public string SearchPlaceholder
    {
        get => _searchPlaceholder ?? TwTexts.Current.Search;
        set => _searchPlaceholder = value;
    }

    private bool ShouldSerializeSearchPlaceholder() => _searchPlaceholder != null;

    private void ResetSearchPlaceholder() => _searchPlaceholder = null;

    /// <summary>Açık menüdeki arama kutusu (kapalıyken ya da <see cref="Searchable"/> kapalıyken <c>null</c>).</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ToolStripTextBox? SearchBox => _menu?.Visible == true ? _search : null;

    /// <summary>Son açılan menü (açılmadıysa <c>null</c>).</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ContextMenuStrip? DropDownMenu => _menu;

    /// <summary>Menü renkleri; FormWind <c>menu:</c> / <c>item:</c> class'larından doldurur.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal TwMenuColors MenuColors { get; set; } = new TwMenuColors();

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

    protected override AccessibleObject CreateAccessibilityInstance() => new TwSelectAccessibleObject(this);

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
        _search = null;
        if (Searchable)
        {
            _search = new ToolStripTextBox
            {
                AutoSize = false,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = MenuColors.MenuBack,
                ForeColor = MenuColors.Fore,
                Margin = new Padding(4, 4, 4, 2),
            };
            _search.Size = new Size(Math.Max(Width, 120) - 8, Font.Height + 8);
            var box = _search;
            box.TextChanged += (_, _) => ApplyFilter(box.Text);
            box.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    // Süzülen ilk öğe seçilir.
                    foreach (var index in VisibleItemIndices())
                    {
                        SelectedIndex = index;
                        _menu?.Close();
                        break;
                    }
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    _menu?.Close();
                    e.SuppressKeyPress = true;
                }
            };
            SetCueBanner(box.TextBox, SearchPlaceholder);
            _menu.Items.Insert(0, box);
            _menu.Items.Insert(1, new ToolStripSeparator());
            _menu.Opened += (_, _) => box.Focus();
        }
        _menu.MinimumSize = new Size(Width, 0);
        _menu.Show(this, new Point(0, Height + 2));
    }

    /// <summary>Menüde yalnızca <paramref name="filter"/>'ı (büyük/küçük harf ve aksan farkı gözetmeden) içeren öğeleri gösterir.</summary>
    public void ApplyFilter(string? filter)
    {
        if (_menu is null) return;
        int offset = _search is null ? 0 : 2;
        for (int i = 0; i < _items.Count && i + offset < _menu.Items.Count; i++)
            _menu.Items[i + offset].Visible = Matches(_items[i], filter);
    }

    /// <summary>Açık menüde görünen öğelerin dizinleri.</summary>
    public IEnumerable<int> VisibleItemIndices()
    {
        if (_menu is null) yield break;
        int offset = _search is null ? 0 : 2;
        for (int i = 0; i < _items.Count && i + offset < _menu.Items.Count; i++)
            if (_menu.Items[i + offset].Available) yield return i;
    }

    internal static bool Matches(string item, string? filter) =>
        string.IsNullOrWhiteSpace(filter)
        || CultureInfo.InvariantCulture.CompareInfo.IndexOf(Fold(item), Fold(filter!.Trim()), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    private static bool StartsWith(string item, string prefix) =>
        CultureInfo.InvariantCulture.CompareInfo.IsPrefix(Fold(item), Fold(prefix), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);

    // Aksanlar IgnoreNonSpace ile yok sayılır; ı/İ ise ayrı harf olduğundan elle i'ye katlanır ("sanli" → "Şanlıurfa").
    private static string Fold(string text) => text.Replace('ı', 'i').Replace('İ', 'i').ToLowerInvariant();

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        if (char.IsControl(e.KeyChar) || e.KeyChar == ' ' && _typeAhead.Length == 0) return;

        // Yazarak seçme: 1 sn içinde yazılan harfler birikir; aynı harfi tekrar yazmak o harfle başlayan sonrakine geçer.
        var now = DateTime.UtcNow;
        if ((now - _typeAheadTime).TotalMilliseconds > 1000) _typeAhead = "";
        _typeAheadTime = now;
        bool repeat = _typeAhead.Length == 1 && char.ToUpper(_typeAhead[0], CultureInfo.CurrentCulture) == char.ToUpper(e.KeyChar, CultureInfo.CurrentCulture);
        if (!repeat) _typeAhead += e.KeyChar;

        int start = repeat ? _selectedIndex + 1 : Math.Max(0, _selectedIndex);
        for (int n = 0; n < _items.Count; n++)
        {
            int i = (start + n) % _items.Count;
            if (StartsWith(_items[i], _typeAhead))
            {
                SelectedIndex = i;
                break;
            }
        }
        e.Handled = true;
    }

    // EM_SETCUEBANNER: boş kutuda ipucu metni.
    private static void SetCueBanner(TextBox box, string text)
    {
        void Apply() => SendMessage(box.Handle, 0x1501, (IntPtr)1, text);
        if (box.IsHandleCreated) Apply();
        else box.HandleCreated += (_, _) => Apply();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wParam, string lParam);

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
        // Sağdan sola: ok solda, metin sağa yaslı.
        bool rtl = TwRtl.NeedsMirroring(this);
        var textBounds = rtl
            ? new Rectangle(chevron, 0, Math.Max(0, Width - Padding.Left - chevron), Height)
            : new Rectangle(Padding.Left, 0, Math.Max(0, Width - Padding.Left - chevron), Height);
        var textFlags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
        TextRenderer.DrawText(g, SelectedItem ?? "", Font, textBounds, fore, rtl ? TwRtl.Mirror(textFlags) : textFlags);

        // Ayraç: kenarlık rengiyle, yüksekliğin yarısı kadar.
        var dividerColor = _chrome.HasBorder ? _chrome.BorderColor : Color.FromArgb(80, fore);
        int x = rtl ? chevron : Width - chevron;
        using (var pen = new Pen(dividerColor, Math.Max(1f, TwDpi.DeviceDpi(this) / 96f)))
            g.DrawLine(pen, x, Height * 0.28f, x, Height * 0.72f);

        {
            int em = TwIconRenderer.EmPixels(Font.SizeInPoints, TwDpi.DeviceDpi(this));
            TwIconRenderer.DrawIcon(g, "chevron-down", em, new Rectangle(rtl ? 0 : x, 0, chevron, Height), fore);
        }
    }
}
