using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FormWind;

/// <summary>
/// Color palette, component classes and font settings. Parse results are cached per theme;
/// the cache is cleared when the theme changes.
/// </summary>
public sealed class TwTheme
{
    // İlk statik alan: GDI+ başlamadan WinForms DPI ayarı uygulanır (bkz. TwDpi).
    private static readonly bool DpiReady = TwDpi.EnsureInitialized();

    private static readonly Regex ColorNameRx = new Regex("^[a-z][a-z0-9]*$", RegexOptions.CultureInvariant);

    // Paletler bir kez çözülür (v4'ün OKLCH dönüşümü tema başına tekrarlanmasın). Varsayılan tema (Current) bunları
    // kullandığından ondan önce başlatılmalı: statik alanlar yazım sırasıyla başlar.
    private static readonly Lazy<Dictionary<string, Color[]>> V3Colors = new Lazy<Dictionary<string, Color[]>>(() => Resolve(TwPalette.Colors));
    private static readonly Lazy<Dictionary<string, Color[]>> V4Colors = new Lazy<Dictionary<string, Color[]>>(() => Resolve(TwPaletteV4.Colors));

    private readonly Dictionary<string, Dictionary<string, Color>> _colors =
        new Dictionary<string, Dictionary<string, Color>>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _components = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TwParsed> _cache =
        new ConcurrentDictionary<string, TwParsed>(StringComparer.Ordinal);

    private readonly Dictionary<string, SortedDictionary<int, string>> _fontWeights =
        new Dictionary<string, SortedDictionary<int, string>>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, char> _icons = new Dictionary<string, char>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TwSvgIcon> _svgIcons = new Dictionary<string, TwSvgIcon>(StringComparer.OrdinalIgnoreCase);

    private string _fontFamilySans = "Segoe UI";
    private string _fontFamilyMono = "Consolas";
    private int _spacingUnit = 4;

    public TwTheme()
        : this(TwPaletteVersion.V3)
    {
    }

    /// <summary>Creates a theme with the default palette of the given Tailwind version.</summary>
    public TwTheme(TwPaletteVersion palette)
    {
        LoadPalette(palette);
        SetColorCore("white", "", Color.White);
        SetColorCore("black", "", Color.Black);
        SetColorCore("transparent", "", Color.Transparent);

        foreach (var pair in DefaultComponents)
            _components[pair.Key] = pair.Value;

        // Windows'ta Segoe UI ağırlıkları ayrı aileler olarak kurulur. 400 ve 700 ailenin kendisidir.
        SetFontWeight("Segoe UI", 300, "Segoe UI Light");
        SetFontWeight("Segoe UI", 350, "Segoe UI Semilight");
        SetFontWeight("Segoe UI", 600, "Segoe UI Semibold");
        SetFontWeight("Segoe UI", 900, "Segoe UI Black");
    }

    /// <summary>Font the icons are drawn with. Default: Segoe Fluent Icons (Win11), otherwise Segoe MDL2 Assets (Win10).</summary>
    public string IconFontFamily { get; set; } = TwIcons.DefaultFontFamily;

    /// <summary>Defines the installed family for a specific weight of a font family (e.g. 600 → "Inter SemiBold").</summary>
    public void SetFontWeight(string family, int weight, string face)
    {
        if (string.IsNullOrWhiteSpace(family)) throw new ArgumentException("The family name cannot be empty.", nameof(family));
        if (weight < 1 || weight > 1000) throw new ArgumentOutOfRangeException(nameof(weight));
        if (!_fontWeights.TryGetValue(family, out var weights))
        {
            weights = new SortedDictionary<int, string>();
            _fontWeights.Add(family, weights);
        }
        weights[weight] = face ?? throw new ArgumentNullException(nameof(face));
    }

    /// <summary>
    /// Chooses a weight from the installed weight families with CSS's matching rule. Returns the family to use and
    /// whether a bold style is needed (when it falls back to the family itself, as for 400/700).
    /// </summary>
    public (string Face, bool Bold) ResolveFontWeight(string family, int weight) =>
        ResolveFontWeight(family, weight, TwFonts.IsInstalled);

    internal (string Face, bool Bold) ResolveFontWeight(string family, int weight, Func<string, bool> isInstalled)
    {
        var available = new SortedDictionary<int, (string Face, bool Bold)>
        {
            [400] = (family, false),
            [700] = (family, true),
        };
        if (_fontWeights.TryGetValue(family, out var weights))
        {
            foreach (var pair in weights)
                if (isInstalled(pair.Value)) available[pair.Key] = (pair.Value, false);
        }

        if (available.TryGetValue(weight, out var exact)) return exact;

        var keys = new List<int>(available.Keys);
        int? Find(Func<int, bool> predicate, bool ascending)
        {
            if (ascending)
            {
                foreach (var key in keys) if (predicate(key)) return key;
            }
            else
            {
                for (int i = keys.Count - 1; i >= 0; i--) if (predicate(keys[i])) return keys[i];
            }
            return null;
        }

        int? chosen = weight >= 400 && weight <= 500
            ? Find(k => k > weight && k <= 500, true) ?? Find(k => k < weight, false) ?? Find(k => k > 500, true)
            : weight < 400
                ? Find(k => k < weight, false) ?? Find(k => k > weight, true)
                : Find(k => k > weight, true) ?? Find(k => k < weight, false);

        return available[chosen ?? 400];
    }

    /// <summary>Adds a theme-level icon or overrides a built-in one (e.g. for a custom icon font).</summary>
    public void SetIcon(string name, char glyph)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The icon name cannot be empty.", nameof(name));
        _icons[name] = glyph;
    }

    /// <summary>
    /// Adds an SVG icon or overrides the glyph icon with the same name (SVG wins). The file content of sets such as
    /// Lucide/Heroicons can be passed directly; the color is the text color where it is drawn.
    /// </summary>
    public void SetSvgIcon(string name, string svg)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The icon name cannot be empty.", nameof(name));
        _svgIcons[name] = TwSvgIcon.Parse(svg);
    }

    internal bool TryGetSvgIcon(string? name, out TwSvgIcon? icon)
    {
        icon = null;
        return !string.IsNullOrWhiteSpace(name) && _svgIcons.TryGetValue(name!, out icon);
    }

    /// <summary>Whether the name is an SVG or glyph icon.</summary>
    public bool HasIcon(string? name) => TryGetSvgIcon(name, out _) || TryGetIcon(name, out _);

    /// <summary>Converts an icon name to a glyph: theme icons first, then <see cref="TwIcons"/>; the <c>#E710</c> form is accepted too.</summary>
    public bool TryGetIcon(string? name, out char glyph)
    {
        glyph = '\0';
        if (string.IsNullOrWhiteSpace(name)) return false;
        return _icons.TryGetValue(name!, out glyph) || TwIcons.TryGetGlyph(name!, out glyph);
    }

    /// <summary>The theme used by all <see cref="TailwindProvider"/>s without a theme of their own.</summary>
    public static TwTheme Current
    {
        get => _current;
        set
        {
            _current = value ?? throw new ArgumentNullException(nameof(value));
            CurrentChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static event EventHandler? CurrentChanged;

    /// <summary>
    /// Default component classes built into the code. The Designer cannot read the theme file,
    /// so the design-time preview uses these.
    /// </summary>
    public static IReadOnlyDictionary<string, string> DefaultComponents { get; } = new Dictionary<string, string>
    {
        ["btn"] = "px-4 py-2 rounded-md text-sm font-semibold cursor-pointer transition",
        // Koyu temada da blue-600/700: beyaz yazı blue-500 üzerinde 3,7:1, blue-400 üzerinde 2,5:1 ile WCAG AA'yı geçmez.
        ["btn-primary"] = "btn bg-blue-600 text-white hover:bg-blue-700 active:bg-blue-800 disabled:bg-gray-300 disabled:text-gray-500 dark:bg-blue-600 dark:hover:bg-blue-700 dark:disabled:bg-gray-700 dark:disabled:text-gray-400",
        ["btn-ghost"] = "btn bg-white text-gray-700 border border-gray-300 hover:bg-gray-100 active:bg-gray-200 dark:bg-gray-800 dark:text-gray-200 dark:border-gray-600 dark:hover:bg-gray-700 disabled:bg-gray-50 disabled:text-gray-500 disabled:border-gray-200 dark:disabled:bg-gray-800 dark:disabled:text-gray-500 dark:disabled:border-gray-700",
        ["btn-danger"] = "btn bg-red-600 text-white hover:bg-red-700 active:bg-red-800 disabled:bg-gray-300 disabled:text-gray-500 dark:disabled:bg-gray-700 dark:disabled:text-gray-400",
        ["field"] = "label:text-sm label:font-medium label:text-gray-700 hint:text-xs hint:text-gray-500 error:text-xs error:text-red-600 " +
                    "dark:label:text-gray-200 dark:hint:text-gray-400 dark:error:text-red-400",
        ["card"] = "bg-white border border-gray-200 rounded-lg p-4 shadow-sm dark:bg-gray-800 dark:border-gray-700",
        ["input"] = "bg-white text-gray-900 border border-gray-300 rounded-md px-3 py-2 placeholder:text-gray-400 dark:placeholder:text-gray-500 focus:border-blue-500 focus:ring-2 focus:ring-blue-500/30 dark:bg-gray-900 dark:text-gray-100 dark:border-gray-600 dark:focus:border-blue-400 invalid:border-red-500 invalid:ring-2 invalid:ring-red-500/20 dark:invalid:border-red-400",
        ["table"] = "bg-white text-sm text-gray-700 border border-gray-200 divide-y divide-gray-200 " +
                    "header:bg-gray-50 header:text-gray-500 header:text-xs header:font-bold header:h-10 header:px-3 header:whitespace-nowrap " +
                    "cell:px-3 row:h-10 row:hover:bg-gray-50 selected:bg-blue-50 selected:text-blue-900 row-header:hidden " +
                    "dark:bg-gray-800 dark:text-gray-200 dark:border-gray-700 dark:divide-gray-700 " +
                    "dark:header:bg-gray-900 dark:header:text-gray-400 dark:row:hover:bg-gray-700 " +
                    "dark:selected:bg-blue-900 dark:selected:text-blue-100",
    };

    // DefaultComponents'tan sonra bildirilmeli: statik başlatıcılar yazım sırasıyla çalışır.
    private static TwTheme _current = new TwTheme();

    public string FontFamilySans
    {
        get => _fontFamilySans;
        set { _fontFamilySans = value ?? throw new ArgumentNullException(nameof(value)); _cache.Clear(); }
    }

    public string FontFamilyMono
    {
        get => _fontFamilyMono;
        set { _fontFamilyMono = value ?? throw new ArgumentNullException(nameof(value)); _cache.Clear(); }
    }

    /// <summary>Pixel value of one unit in spacing and size classes (Tailwind: 4).</summary>
    public int SpacingUnit
    {
        get => _spacingUnit;
        set
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "SpacingUnit must be positive.");
            _spacingUnit = value;
            _cache.Clear();
        }
    }

    /// <summary>Color drawn when <c>border</c> is used without a color (Tailwind: gray-200).</summary>
    public Color DefaultBorderColor { get; set; } = ColorTranslator.FromHtml("#e5e7eb");

    public IReadOnlyDictionary<string, string> Components => _components;

    public IEnumerable<string> ColorNames => _colors.Keys;

    public void SetColor(string name, Color color) => SetColor(name, "", color);

    /// <summary>Version of the default palette (<see cref="UsePalette"/> or <c>"palette"</c> in the theme file).</summary>
    public TwPaletteVersion Palette { get; private set; }

    /// <summary>
    /// Replaces the 22 colors of the default palette (red … rose, slate … stone) with those of the given version. Custom
    /// colors (such as <c>brand</c>) are kept; default colors overridden under the same name return to the version's values.
    /// </summary>
    public void UsePalette(TwPaletteVersion palette)
    {
        LoadPalette(palette);
        _cache.Clear();
    }

    private void LoadPalette(TwPaletteVersion palette)
    {
        Palette = palette;
        foreach (var pair in palette == TwPaletteVersion.V4 ? V4Colors.Value : V3Colors.Value)
        {
            for (int i = 0; i < TwPalette.Shades.Length; i++)
                SetColorCore(pair.Key, TwPalette.Shades[i], pair.Value[i]);
        }
    }

    private static Dictionary<string, Color[]> Resolve(IReadOnlyDictionary<string, string[]> source)
    {
        var colors = new Dictionary<string, Color[]>(StringComparer.Ordinal);
        foreach (var pair in source)
        {
            var shades = new Color[pair.Value.Length];
            for (int i = 0; i < shades.Length; i++) shades[i] = ParseColor(pair.Value[i], pair.Key + "-" + TwPalette.Shades[i]);
            colors[pair.Key] = shades;
        }
        return colors;
    }

    /// <summary>Adds or overrides a palette color. When <paramref name="shade"/> is empty the color is used without a shade (<c>bg-brand</c>).</summary>
    public void SetColor(string name, string shade, Color color)
    {
        if (name is null || !ColorNameRx.IsMatch(name))
            throw new ArgumentException("A color name must start with a lowercase letter and contain only a-z and 0-9.", nameof(name));
        SetColorCore(name, shade ?? "", color);
        _cache.Clear();
    }

    public bool TryGetColor(string name, string shade, out Color color)
    {
        color = Color.Empty;
        return _colors.TryGetValue(name, out var shades) && shades.TryGetValue(shade ?? "", out color);
    }

    public void SetComponent(string name, string classes)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The component name cannot be empty.", nameof(name));
        _components[name] = classes ?? "";
        _cache.Clear();
    }

    public bool RemoveComponent(string name)
    {
        var removed = _components.Remove(name);
        if (removed) _cache.Clear();
        return removed;
    }

    public string? GetComponent(string name) => _components.TryGetValue(name, out var classes) ? classes : null;

    /// <summary>Parses a class string. Returns the same result instance for the same (normalized) string.</summary>
    public TwParsed Parse(string? classes)
    {
        var key = TwParser.Normalize(classes);
        return key.Length == 0 ? TwParsed.Empty : _cache.GetOrAdd(key, k => TwParser.Parse(k, this));
    }

    public static TwTheme FromJson(string json)
    {
        var theme = new TwTheme();
        theme.ApplyJson(json);
        return theme;
    }

    /// <summary>Loads a theme file; paths in the <c>fonts</c> list are resolved relative to the file's folder.</summary>
    public static TwTheme Load(string path)
    {
        var theme = new TwTheme();
        theme.ApplyJson(File.ReadAllText(path), Path.GetDirectoryName(Path.GetFullPath(path)));
        return theme;
    }

    /// <summary>Loads the file if it exists (throws if it is invalid), otherwise returns the default theme.</summary>
    public static TwTheme LoadIfExists(string path) => File.Exists(path) ? Load(path) : new TwTheme();

    /// <summary>Applies settings in the <c>tw.config.json</c> format on top of this theme.</summary>
    /// <param name="json">Theme content.</param>
    /// <param name="baseDirectory">Folder the <c>fonts</c> paths are relative to (default: the application folder).</param>
    public void ApplyJson(string json, string? baseDirectory = null)
    {
        var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        using var document = JsonDocument.Parse(json, options);
        var root = document.RootElement;

        // "fonts": ["Assets/fonts/Quicksand.ttf"] — kurulum gerektirmeden kaydedilir; bulunamayan dosya atlanır.
        if (root.TryGetProperty("fonts", out var fonts))
        {
            var directory = baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
            foreach (var font in fonts.EnumerateArray())
            {
                var path = font.GetString();
                if (!string.IsNullOrWhiteSpace(path)) TwFonts.TryRegister(Path.Combine(directory, path!));
            }
        }

        // "palette": "v4" — Tailwind v4 renkleri; "colors" bunun üzerine eklenir.
        if (root.TryGetProperty("palette", out var palette))
        {
            var version = palette.GetString();
            UsePalette(string.Equals(version, "v4", StringComparison.OrdinalIgnoreCase) ? TwPaletteVersion.V4
                : string.Equals(version, "v3", StringComparison.OrdinalIgnoreCase) ? TwPaletteVersion.V3
                : throw new FormatException($"tw.config.json: invalid 'palette' value: {version} (v3 or v4)."));
        }

        if (root.TryGetProperty("fontFamily", out var sans))
            FontFamilySans = sans.GetString() ?? FontFamilySans;
        if (root.TryGetProperty("fontFamilyMono", out var mono))
            FontFamilyMono = mono.GetString() ?? FontFamilyMono;
        if (root.TryGetProperty("spacingUnit", out var unit))
            SpacingUnit = unit.GetInt32();
        if (root.TryGetProperty("defaultBorderColor", out var border))
            DefaultBorderColor = ParseColor(border.GetString(), "defaultBorderColor");

        if (root.TryGetProperty("colors", out var colors))
        {
            foreach (var color in colors.EnumerateObject())
            {
                if (color.Value.ValueKind == JsonValueKind.String)
                {
                    SetColor(color.Name, ParseColor(color.Value.GetString(), color.Name));
                }
                else if (color.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var shade in color.Value.EnumerateObject())
                        SetColor(color.Name, shade.Name, ParseColor(shade.Value.GetString(), color.Name + "-" + shade.Name));
                }
                else
                {
                    throw new FormatException($"tw.config.json: 'colors.{color.Name}' must be a color or an object of shades.");
                }
            }
        }

        if (root.TryGetProperty("components", out var components))
        {
            foreach (var component in components.EnumerateObject())
                SetComponent(component.Name, component.Value.GetString() ?? "");
        }

        // "fontWeights": { "Inter": { "300": "Inter Light", "600": "Inter SemiBold" } }
        if (root.TryGetProperty("fontWeights", out var fontWeights))
        {
            foreach (var family in fontWeights.EnumerateObject())
            {
                foreach (var weight in family.Value.EnumerateObject())
                {
                    if (!int.TryParse(weight.Name, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
                        throw new FormatException($"tw.config.json: 'fontWeights.{family.Name}.{weight.Name}' is not a valid weight.");
                    SetFontWeight(family.Name, value, weight.Value.GetString() ?? "");
                }
            }
        }

        if (root.TryGetProperty("iconFont", out var iconFont))
            IconFontFamily = iconFont.GetString() ?? IconFontFamily;

        // "icons": { "invoice": "E8A5", "box": "Assets/icons/box.svg", "dot": "<svg ...>...</svg>" }
        if (root.TryGetProperty("icons", out var icons))
        {
            foreach (var icon in icons.EnumerateObject())
            {
                var value = icon.Value.GetString() ?? "";
                if (value.TrimStart().StartsWith("<", StringComparison.Ordinal))
                {
                    SetSvgIcon(icon.Name, value);
                }
                else if (value.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    var path = Path.Combine(baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory, value);
                    if (!File.Exists(path)) throw new FormatException($"tw.config.json: file for 'icons.{icon.Name}' not found: {path}");
                    SetSvgIcon(icon.Name, File.ReadAllText(path));
                }
                else if (TwIcons.TryParseCodePoint(value, out var glyph))
                {
                    SetIcon(icon.Name, glyph);
                }
                else
                {
                    throw new FormatException($"tw.config.json: invalid value for 'icons.{icon.Name}' (code point, .svg path or <svg>): {value}");
                }
            }
        }
    }

    private void SetColorCore(string name, string shade, Color color)
    {
        if (!_colors.TryGetValue(name, out var shades))
        {
            shades = new Dictionary<string, Color>(StringComparer.Ordinal);
            _colors.Add(name, shades);
        }
        shades[shade] = color;
    }

    private static Color ParseColor(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException($"tw.config.json: empty color value for '{key}'.");
        // Tailwind v4 biçimi: oklch(62.3% 0.214 259.8)
        if (value!.TrimStart().StartsWith("oklch", StringComparison.OrdinalIgnoreCase))
        {
            if (TwOklch.TryParse(value, out var oklch)) return oklch;
            throw new FormatException($"tw.config.json: invalid color for '{key}': {value}");
        }
        try
        {
            return ColorTranslator.FromHtml(value);
        }
        catch (Exception ex)
        {
            throw new FormatException($"tw.config.json: invalid color for '{key}': {value}", ex);
        }
    }
}
