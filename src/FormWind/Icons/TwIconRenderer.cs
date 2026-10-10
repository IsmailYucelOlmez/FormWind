using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FormWind;

/// <summary>
/// İkon glyph'lerini font metriklerinden bağımsız çizer. İkon fontlarının satır yüksekliği glyph boyutuyla
/// uyuşmadığından (Segoe Fluent Icons 15 pt'de 11 px yükseklik ölçer) metin çizimi ikonu kırpar; burada glyph
/// bir yola çevrilir ve gerçek mürekkep sınırlarına göre kutunun ortasına yerleştirilir.
/// </summary>
public static class TwIconRenderer
{
    /// <summary>Font boyutuna karşılık gelen kare ikon kutusu (web'deki gibi: 20 px font = 20 px ikon).</summary>
    /// <remarks>
    /// GDI fontları puntoyu sürecin sistem DPI'ıyla piksele çevirir; PerMonitorV2'de başka DPI'lı bir ekrandaki kontrolün
    /// fontu zaten büyütülmüş olduğundan ikon da sistem DPI'ıyla hesaplanır. <paramref name="dpi"/> artık kullanılmıyor
    /// (eski çağrılar için duruyor).
    /// </remarks>
    public static int EmPixels(float sizeInPoints, int dpi) =>
        Math.Max(1, (int)Math.Round(sizeInPoints * TwDpi.SystemDpi / 72f));

    /// <summary>
    /// Adı verilen ikonu çizer: önce temadaki SVG ikon, yoksa ikon fontundaki glyph. İkon bulunamazsa <c>false</c>.
    /// </summary>
    public static bool DrawIcon(Graphics graphics, string? name, int emPixels, Rectangle bounds, Color color, TwTheme? theme = null)
    {
        theme ??= TwTheme.Current;
        if (theme.TryGetSvgIcon(name, out var svg))
        {
            svg!.Draw(graphics, emPixels, bounds, color);
            return true;
        }
        if (!theme.TryGetIcon(name, out var glyph)) return false;
        Draw(graphics, glyph, theme.IconFontFamily, emPixels, bounds, color);
        return true;
    }

    public static void Draw(Graphics graphics, char glyph, string fontFamily, int emPixels, Rectangle bounds, Color color)
    {
        if (color.A == 0 || bounds.Width <= 0 || bounds.Height <= 0) return;

        FontFamily family;
        try
        {
            family = new FontFamily(fontFamily);
        }
        catch (ArgumentException)
        {
            return; // ikon fontu kurulu değil
        }

        using (family)
        using (var path = new GraphicsPath())
        {
            path.AddString(glyph.ToString(), family, (int)FontStyle.Regular, emPixels, PointF.Empty, StringFormat.GenericTypographic);
            var ink = path.GetBounds();
            if (ink.Width <= 0 || ink.Height <= 0) return;

            // Kare em kutusunu bounds içinde ortala, glyph'in mürekkebini de em kutusunun ortasına taşı.
            float boxX = bounds.X + (bounds.Width - emPixels) / 2f;
            float boxY = bounds.Y + (bounds.Height - emPixels) / 2f;
            float dx = boxX + (emPixels - ink.Width) / 2f - ink.X;
            float dy = boxY + (emPixels - ink.Height) / 2f - ink.Y;

            using (var matrix = new Matrix())
            {
                matrix.Translate(dx, dy);
                path.Transform(matrix);
            }

            var state = graphics.Save();
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using (var brush = new SolidBrush(color))
                graphics.FillPath(brush, path);
            graphics.Restore(state);
        }
    }
}
