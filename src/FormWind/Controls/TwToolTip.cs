using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// Class'larla stillenen ipucu kutusu (owner-draw ToolTip). <see cref="Provider"/> verilirse onun koyu tema durumunu izler.
/// Varsayılan: <c>bg-gray-900 text-white text-xs px-2 py-1 dark:bg-gray-100 dark:text-gray-900</c>.
/// </summary>
[ToolboxItem(true)]
[Description("Class'larla stillenen ipucu kutusu.")]
public class TwToolTip : ToolTip
{
    public const string DefaultClasses = "bg-gray-900 text-white text-xs px-2 py-1 border-gray-700 dark:bg-gray-100 dark:text-gray-900 dark:border-gray-300";

    public TwToolTip()
    {
        Initialize();
    }

    public TwToolTip(IContainer container) : base(container)
    {
        Initialize();
    }

    [Category("FormWind"), DefaultValue(DefaultClasses)]
    public string Classes { get; set; } = DefaultClasses;

    /// <summary>Koyu tema durumunun alınacağı provider (isteğe bağlı).</summary>
    [Category("FormWind"), DefaultValue(null)]
    public TailwindProvider? Provider { get; set; }

    /// <summary>O anki tema durumuna göre çözülmüş stil.</summary>
    public TwStyle ResolveStyle() =>
        (Provider?.Theme ?? TwTheme.Current).Parse(Classes).Resolve(Provider?.DarkMode == true ? TwState.Dark : TwState.None);

    /// <summary>Metin için ipucu kutusunun boyutu (padding dahil).</summary>
    public Size MeasureTip(string text, int dpi = 96)
    {
        var style = ResolveStyle();
        var font = TwFonts.Create(style, SystemFonts.StatusFont ?? Control.DefaultFont, Provider?.Theme ?? TwTheme.Current);
        var padding = style.Padding.ApplyTo(Padding.Empty, dpi / 96f);
        var size = TextRenderer.MeasureText(text ?? "", font, Size.Empty, TextFormatFlags.NoPadding);
        return new Size(size.Width + padding.Horizontal + 2, size.Height + padding.Vertical + 2);
    }

    private void Initialize()
    {
        OwnerDraw = true;
        Popup += OnPopup;
        Draw += OnDraw;
    }

    private void OnPopup(object? sender, PopupEventArgs e)
    {
        var dpi = e.AssociatedControl is Control associated ? TwDpi.DeviceDpi(associated) : 96;
        e.ToolTipSize = MeasureTip(GetToolTip(e.AssociatedControl) ?? "", dpi);
    }

    private void OnDraw(object? sender, DrawToolTipEventArgs e)
    {
        var style = ResolveStyle();
        var theme = Provider?.Theme ?? TwTheme.Current;
        var font = TwFonts.Create(style, SystemFonts.StatusFont ?? Control.DefaultFont, theme);

        using (var back = new SolidBrush(style.BackColor ?? SystemColors.Info))
            e.Graphics.FillRectangle(back, e.Bounds);
        using (var pen = new Pen(style.BorderColor ?? Color.FromArgb(60, 0, 0, 0)))
            e.Graphics.DrawRectangle(pen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);

        TextRenderer.DrawText(e.Graphics, e.ToolTipText, font, e.Bounds, style.ForeColor ?? SystemColors.InfoText,
            TextFormatFlags.NoPadding | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}
