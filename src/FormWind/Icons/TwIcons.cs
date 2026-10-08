using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace FormWind;

/// <summary>
/// Windows'la gelen ikon fontu (Segoe Fluent Icons / Segoe MDL2 Assets) için ad → kod noktası tablosu.
/// İki font bu ikonlar için aynı kod noktalarını kullanır. Ek ikonlar <see cref="Register"/> ya da
/// tema dosyasındaki <c>icons</c> ile eklenebilir; doğrudan kod noktası için <c>#E710</c> yazılabilir.
/// </summary>
public static class TwIcons
{
    private static readonly Dictionary<string, char> Map = new Dictionary<string, char>(StringComparer.OrdinalIgnoreCase)
    {
        // Eylemler
        ["plus"] = '', ["minus"] = '', ["x"] = '', ["check"] = '',
        ["edit"] = '', ["trash"] = '', ["save"] = '', ["search"] = '',
        ["filter"] = '', ["sort"] = '', ["refresh"] = '', ["sync"] = '',
        ["download"] = '', ["upload"] = '', ["print"] = '', ["share"] = '',
        ["copy"] = '', ["cut"] = '', ["paste"] = '', ["undo"] = '',
        ["redo"] = '', ["send"] = '', ["attach"] = '', ["link"] = '',
        ["pin"] = '', ["zoom-in"] = '', ["zoom-out"] = '', ["fullscreen"] = '',
        ["power"] = '', ["external-link"] = '', ["moon"] = '', ["sun"] = '',

        // Gezinme
        ["home"] = '', ["menu"] = '', ["more"] = '', ["settings"] = '',
        ["arrow-left"] = '', ["arrow-right"] = '', ["arrow-up"] = '', ["arrow-down"] = '',
        ["chevron-left"] = '', ["chevron-right"] = '', ["chevron-up"] = '', ["chevron-down"] = '',
        ["list"] = '', ["grid"] = '',

        // Nesneler
        ["user"] = '', ["users"] = '', ["mail"] = '', ["phone"] = '',
        ["calendar"] = '', ["clock"] = '', ["history"] = '', ["bell"] = '',
        ["folder"] = '', ["folder-open"] = '', ["file"] = '', ["image"] = '',
        ["camera"] = '', ["cart"] = '', ["tag"] = '', ["flag"] = '',
        ["globe"] = '', ["map-pin"] = '', ["location"] = '', ["calculator"] = '', ["key"] = '',
        ["lock"] = '', ["unlock"] = '', ["shield"] = '', ["eye"] = '',
        ["eye-off"] = '', ["heart"] = '', ["heart-filled"] = '', ["star"] = '',
        ["star-filled"] = '',

        // Durum
        ["info"] = '', ["warning"] = '', ["error"] = '', ["help"] = '',
    };

    /// <summary>Kurulu ilk ikon fontu; hiçbiri yoksa (Windows 7/8) "Segoe MDL2 Assets" adı döner ve ikonlar çizilmez.</summary>
    public static string DefaultFontFamily { get; } =
        TwFonts.IsInstalled("Segoe Fluent Icons") ? "Segoe Fluent Icons" : "Segoe MDL2 Assets";

    public static IEnumerable<string> Names => Map.Keys.OrderBy(n => n, StringComparer.Ordinal);

    public static void Register(string name, char glyph)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("İkon adı boş olamaz.", nameof(name));
        lock (Map) Map[name] = glyph;
    }

    public static bool TryGetGlyph(string name, out char glyph)
    {
        if (name.StartsWith("#", StringComparison.Ordinal)) return TryParseCodePoint(name.Substring(1), out glyph);
        lock (Map) return Map.TryGetValue(name, out glyph);
    }

    /// <summary>"E710", "#E710" ya da "U+E710" biçimindeki kod noktasını çözer (yalnızca BMP).</summary>
    public static bool TryParseCodePoint(string? value, out char glyph)
    {
        glyph = '\0';
        if (string.IsNullOrWhiteSpace(value)) return false;
        var hex = value!.Trim();
        if (hex.StartsWith("#", StringComparison.Ordinal)) hex = hex.Substring(1);
        else if (hex.StartsWith("U+", StringComparison.OrdinalIgnoreCase)) hex = hex.Substring(2);

        if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code) || code <= 0 || code > 0xFFFF)
            return false;
        glyph = (char)code;
        return true;
    }
}

/// <summary>Property penceresinde ikon adları için açılır liste (serbest metin de yazılabilir).</summary>
public sealed class TwIconNameConverter : StringConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) =>
        new StandardValuesCollection(TwIcons.Names.ToArray());
}
