using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// Arka planını, köşelerini ve metnini kendisi çizen buton. <see cref="ButtonBase.Image"/> şu an çizilmez;
/// <c>Paint</c> olayı tetiklenmez.
/// </summary>
[ToolboxItem(true)]
[Description("Yuvarlak köşeli, owner-draw buton.")]
public class TwButton : Button, ITwChrome
{
    private TwChrome _chrome = TwChrome.None;
    private string _iconName = "";
    private TwIconPosition _iconPosition = TwIconPosition.Left;
    private int _iconSpacing = 8;

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _focusVisible = TwFocusVisible.LastInputWasKeyboard;
        Invalidate();
    }

    // Odak halkası yalnızca odak klavyeyle geldiyse çizilir (bkz. TwFocusVisible).
    private bool _focusVisible;

    public TwButton()
    {
        TwFocusVisible.EnsureInstalled();
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor,
            true);
    }

    [Category("FormWind"), DefaultValue(0f), Description("Köşe yarıçapı (piksel). Classes tanımlıysa onun değeri kullanılır.")]
    public float CornerRadius
    {
        get => _chrome.Radius;
        set => SetUniform(Math.Max(0f, value), _chrome.BorderTop, _chrome.BorderColor);
    }

    [Category("FormWind"), DefaultValue(0f), Description("Kenarlık kalınlığı (piksel, tüm kenarlar).")]
    public float BorderWidth
    {
        get => _chrome.BorderTop;
        set => SetUniform(_chrome.Radius, Math.Max(0f, value), _chrome.BorderColor);
    }

    [Category("FormWind"), Description("Kenarlık rengi.")]
    public Color BorderColor
    {
        get => _chrome.BorderColor;
        set => SetUniform(_chrome.Radius, _chrome.BorderTop, value);
    }

    private void SetUniform(float radius, float width, Color color)
    {
        _chrome = new TwChrome(radius, width, width, width, width, color, _chrome.Gradient);
        Invalidate();
    }

    [Category("FormWind"), DefaultValue(""), TypeConverter(typeof(TwIconNameConverter))]
    [Description("Metnin yanında çizilen ikon (örn. plus, save) ya da kod noktası (#E710).")]
    public string IconName
    {
        get => _iconName;
        set { _iconName = value ?? ""; Invalidate(); }
    }

    [Category("FormWind"), DefaultValue(TwIconPosition.Left)]
    public TwIconPosition IconPosition
    {
        get => _iconPosition;
        set { _iconPosition = value; Invalidate(); }
    }

    [Category("FormWind"), DefaultValue(8), Description("İkon ile metin arası (96 DPI piksel).")]
    public int IconSpacing
    {
        get => _iconSpacing;
        set { _iconSpacing = Math.Max(0, value); Invalidate(); }
    }

    private bool ShouldSerializeBorderColor() => !_chrome.BorderColor.IsEmpty;

    private void ResetBorderColor() => BorderColor = Color.Empty;

    void ITwChrome.SetChrome(TwChrome chrome)
    {
        if (_chrome.Equals(chrome)) return;
        _chrome = chrome;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        TwPainter.PaintParentBackground(this, graphics);
        TwPainter.FillSurface(graphics, ClientRectangle, _chrome.Radius, BackColor, _chrome.Gradient);
        TwPainter.StrokeChrome(graphics, ClientRectangle, _chrome);

        var textBounds = new Rectangle(
            Padding.Left,
            Padding.Top,
            Math.Max(0, Width - Padding.Horizontal),
            Math.Max(0, Height - Padding.Vertical));

        var flags = TwPainter.ToTextFormatFlags(TextAlign);
        if (TwRtl.NeedsMirroring(this)) flags = TwRtl.Mirror(flags);
        if (!UseMnemonic) flags |= TextFormatFlags.NoPrefix;
        else if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;

        if (TwTheme.Current.HasIcon(_iconName))
            DrawIconAndText(graphics, _iconName, textBounds, flags);
        else
            TextRenderer.DrawText(graphics, Text, Font, textBounds, ForeColor, flags);

        if (Focused && _focusVisible)
        {
            var inset = (int)Math.Ceiling(TwPainter.EffectiveRadius(_chrome.Radius, Size) / 2) + 3;
            var focus = Rectangle.Inflate(ClientRectangle, -inset, -3);
            ControlPaint.DrawFocusRectangle(graphics, focus, ForeColor, BackColor);
        }
    }

    private static Rectangle AlignVertically(Rectangle column, int size, TextFormatFlags vertical)
    {
        int y = (vertical & TextFormatFlags.VerticalCenter) != 0 ? column.Y + (column.Height - size) / 2
            : (vertical & TextFormatFlags.Bottom) != 0 ? column.Bottom - size
            : column.Y;
        return new Rectangle(column.X, y, column.Width, size);
    }

    // İkon ve metin tek bir grup olarak TextAlign'a göre hizalanır; metin sığmazsa kısaltılır.
    private void DrawIconAndText(Graphics graphics, string icon, Rectangle bounds, TextFormatFlags textFlags)
    {
        const TextFormatFlags measure = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        int em = TwIconRenderer.EmPixels(Font.SizeInPoints, TwDpi.DeviceDpi(this));
        var iconSize = new Size(em, em);

        bool hasText = !string.IsNullOrEmpty(Text);
        int gap = hasText ? (int)Math.Round(IconSpacing * TwDpi.DeviceDpi(this) / 96f) : 0;
        int textWidth = hasText ? TextRenderer.MeasureText(graphics, Text, Font, Size.Empty, measure).Width : 0;
        textWidth = Math.Min(textWidth, Math.Max(0, bounds.Width - iconSize.Width - gap));
        int total = iconSize.Width + gap + textWidth;

        int x = (textFlags & TextFormatFlags.HorizontalCenter) != 0 ? bounds.X + (bounds.Width - total) / 2
            : (textFlags & TextFormatFlags.Right) != 0 ? bounds.Right - total
            : bounds.X;

        var vertical = textFlags & (TextFormatFlags.Top | TextFormatFlags.VerticalCenter | TextFormatFlags.Bottom);
        // Sağdan sola: ikon metnin öbür yanına geçer (IconPosition okuma yönüne göredir).
        bool iconFirst = (IconPosition == TwIconPosition.Left) != TwRtl.NeedsMirroring(this);
        var iconRect = new Rectangle(iconFirst ? x : x + textWidth + gap, bounds.Y, iconSize.Width, bounds.Height);
        var textRect = new Rectangle(iconFirst ? x + iconSize.Width + gap : x, bounds.Y, textWidth, bounds.Height);

        TwIconRenderer.DrawIcon(graphics, icon, em, AlignVertically(iconRect, em, vertical), ForeColor);
        if (hasText)
        {
            var flags = (textFlags & ~(TextFormatFlags.HorizontalCenter | TextFormatFlags.Right)) | TextFormatFlags.NoPadding;
            TextRenderer.DrawText(graphics, Text, Font, textRect, ForeColor, flags);
        }
    }
}

public enum TwIconPosition
{
    Left,
    Right,
}
