using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// Kenarlıksız bir <see cref="TextBox"/>'ı yuvarlak köşeli bir panelin içine yerleştirir. Native TextBox
/// kenarlığı stillenemediği için <c>input</c> tarzı alanlar bu sarmalayıcıyla yapılır.
/// </summary>
[ToolboxItem(true)]
[DefaultEvent(nameof(TextChanged))]
[DefaultProperty(nameof(Text))]
[Description("Yuvarlak köşeli, stillenebilir metin kutusu.")]
public class TwTextBox : TwPanel
{
    private readonly PlaceholderTextBox _inner;

    public TwTextBox()
    {
        _inner = new PlaceholderTextBox { BorderStyle = BorderStyle.None, Dock = DockStyle.Fill };
        _inner.TextChanged += (_, e) => OnTextChanged(e);
        Controls.Add(_inner);

        BackColor = SystemColors.Window;
        Padding = new Padding(8, 6, 8, 6);
        Cursor = Cursors.IBeam;
        UpdateHeight();
    }

    /// <summary>İçteki TextBox. Burada sunulmayan property ve olaylar için.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox InnerTextBox => _inner;

#pragma warning disable CS8765 // Control.Text .NET 8'de [AllowNull] ile işaretli.
    [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), Bindable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public override string Text
    {
        get => _inner.Text;
        set => _inner.Text = value;
    }
#pragma warning restore CS8765

    /// <summary>Kutu boşken gösterilen ipucu metni. Rengi <c>placeholder:text-*</c> ile verilir (varsayılan gray-400).</summary>
    [Category("Appearance"), DefaultValue(""), Localizable(true)]
    public string PlaceholderText
    {
        get => _inner.Placeholder;
        set
        {
            _inner.Placeholder = value ?? "";
            _inner.Invalidate();
        }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color? PlaceholderColor
    {
        get => _inner.PlaceholderColor;
        set
        {
            _inner.PlaceholderColor = value;
            _inner.Invalidate();
        }
    }

    [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
    public new event EventHandler? TextChanged
    {
        add => base.TextChanged += value;
        remove => base.TextChanged -= value;
    }

    [Category("Behavior"), DefaultValue(false)]
    public bool ReadOnly
    {
        get => _inner.ReadOnly;
        set => _inner.ReadOnly = value;
    }

    [Category("Behavior"), DefaultValue(false)]
    public bool UseSystemPasswordChar
    {
        get => _inner.UseSystemPasswordChar;
        set => _inner.UseSystemPasswordChar = value;
    }

    [Category("Behavior"), DefaultValue(32767)]
    public int MaxLength
    {
        get => _inner.MaxLength;
        set => _inner.MaxLength = value;
    }

    [Category("Behavior"), DefaultValue(false)]
    public bool Multiline
    {
        get => _inner.Multiline;
        set
        {
            _inner.Multiline = value;
            UpdateHeight();
        }
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        _inner.BackColor = BackColor.A == 255 ? BackColor : (Parent?.BackColor ?? SystemColors.Window);
    }

    protected override void OnForeColorChanged(EventArgs e)
    {
        base.OnForeColorChanged(e);
        _inner.ForeColor = ForeColor;
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        UpdateHeight();
    }

    protected override void OnPaddingChanged(EventArgs e)
    {
        base.OnPaddingChanged(e);
        UpdateHeight();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _inner.Focus();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _inner.Focus();
    }

    private void UpdateHeight()
    {
        if (!_inner.Multiline)
            Height = _inner.PreferredHeight + Padding.Vertical;
    }

    /// <summary>
    /// Boşken ipucu metnini kendi boyamasının üzerine çizen TextBox. Native ipucu (EM_SETCUEBANNER) rengi
    /// değiştirilemediğinden çizim WM_PAINT sonrasında yapılır.
    /// </summary>
    private sealed class PlaceholderTextBox : TextBox
    {
        private const int WmPaint = 0x000F;
        private const int WmPrint = 0x0317;
        private const int WmPrintClient = 0x0318;
        private static readonly Color DefaultColor = ColorTranslator.FromHtml("#9ca3af"); // gray-400

        public string Placeholder { get; set; } = "";

        public Color? PlaceholderColor { get; set; }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            if (TextLength <= 1) Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            // Önce mesaj türü: TextLength kendisi WM_GETTEXTLENGTH gönderir ve buraya geri döner.
            if (m.Msg != WmPaint && m.Msg != WmPrint && m.Msg != WmPrintClient) return;
            if (Placeholder.Length == 0 || TextLength > 0) return;

            // WM_PRINT/WM_PRINTCLIENT: DrawToBitmap ve yazdırma; çizim verilen HDC'ye yapılır.
            if (m.Msg == WmPaint)
            {
                using var graphics = Graphics.FromHwnd(Handle);
                DrawPlaceholder(graphics);
            }
            else if ((m.Msg == WmPrint || m.Msg == WmPrintClient) && m.WParam != IntPtr.Zero)
            {
                using var graphics = Graphics.FromHdc(m.WParam);
                DrawPlaceholder(graphics);
            }
        }

        private void DrawPlaceholder(Graphics graphics)
        {
            // Native TextBox metni küçük bir sol kenar boşluğuyla çizer; ipucu aynı konuma hizalanır.
            var bounds = new Rectangle(1, 0, ClientSize.Width - 1, ClientSize.Height);
            TextRenderer.DrawText(graphics, Placeholder, Font, bounds, PlaceholderColor ?? DefaultColor, BackColor,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}
