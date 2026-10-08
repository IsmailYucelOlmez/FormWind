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
    public static int EmPixels(float sizeInPoints, int dpi) =>
        Math.Max(1, (int)Math.Round(sizeInPoints * dpi / 72f));

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
