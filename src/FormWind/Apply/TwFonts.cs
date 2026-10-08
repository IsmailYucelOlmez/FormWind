using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;

namespace FormWind;

/// <summary>
/// Paylaşılan <see cref="Font"/> örnekleri. Her <c>new Font</c> bir GDI handle'ı tuttuğundan aynı
/// aile/boyut/stil için tek örnek kullanılır. Örnekler süreç boyunca yaşar ve dispose edilmemelidir.
/// </summary>
public static class TwFonts
{
    private static readonly ConcurrentDictionary<(string Family, float Size, FontStyle Style), Font> Cache =
        new ConcurrentDictionary<(string Family, float Size, FontStyle Style), Font>();

    private static readonly Lazy<HashSet<string>> Installed = new Lazy<HashSet<string>>(() =>
    {
        using var collection = new InstalledFontCollection();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var family in collection.Families) names.Add(family.Name);
        return names;
    });

    public static Font Get(string family, float sizeInPoints, FontStyle style)
    {
        var key = (family, (float)Math.Round(sizeInPoints, 2), style);
        return Cache.GetOrAdd(key, k => new Font(k.Family, k.Size, k.Style, GraphicsUnit.Point));
    }

    /// <summary>Font ailesi sistemde kurulu mu (uygulama açıldığındaki liste).</summary>
    public static bool IsInstalled(string family) => Installed.Value.Contains(family);

    /// <summary>
    /// Stilin font değerlerini <paramref name="baseFont"/> üzerine uygular. Kalınlık, temadaki
    /// ağırlık ailelerine (örn. 600 → "Segoe UI Semibold") çözülür.
    /// </summary>
    public static Font Create(TwStyle style, Font baseFont, TwTheme theme)
    {
        var family = style.FontFamily ?? baseFont.FontFamily.Name;
        var size = style.FontSizePx is float px ? px * 0.75f : baseFont.SizeInPoints;
        var weight = style.FontWeight ?? (baseFont.Bold ? 700 : 400);
        var (face, bold) = theme.ResolveFontWeight(family, weight);

        var fontStyle = bold ? FontStyle.Bold : FontStyle.Regular;
        if (style.Italic ?? baseFont.Italic) fontStyle |= FontStyle.Italic;
        if (style.Underline ?? baseFont.Underline) fontStyle |= FontStyle.Underline;
        return Get(face, size, fontStyle);
    }
}
