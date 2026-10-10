using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FormWind.Convert;

/// <summary>Bir kontrol için yapılan dönüşüm: üretilen class'lar, kaldırılan satırlar ve elle bakılması gerekenler.</summary>
public sealed class ControlConversion
{
    public ControlConversion(string control) => Control = control;

    /// <summary>Kontrolün alan adı (<c>btnSave</c>) ya da formun kendisi için <c>this</c>.</summary>
    public string Control { get; }

    public List<string> Classes { get; } = new List<string>();

    public List<string> RemovedLines { get; } = new List<string>();

    public List<string> Notes { get; } = new List<string>();
}

public sealed class ConversionResult
{
    public ConversionResult(string source, IReadOnlyList<ControlConversion> controls, string providerName, bool providerAdded)
    {
        Source = source;
        Controls = controls;
        ProviderName = providerName;
        ProviderAdded = providerAdded;
    }

    /// <summary>Dönüştürülmüş <c>.Designer.cs</c> içeriği.</summary>
    public string Source { get; }

    public IReadOnlyList<ControlConversion> Controls { get; }

    public string ProviderName { get; }

    /// <summary>Dosyada TailwindProvider yoktu ve eklendi.</summary>
    public bool ProviderAdded { get; }

    public bool Changed => Controls.Any(c => c.Classes.Count > 0);
}

/// <summary>
/// Designer dosyalarındaki sabit stil atamalarını FormWind class'larına çevirir:
/// <c>BackColor</c>/<c>ForeColor</c> → <c>bg-*</c>/<c>text-*</c> (paletteki en yakın renk, değilse <c>bg-[#hex]</c>),
/// <c>Font</c> → <c>text-sm</c>/<c>text-[13px]</c> + <c>font-bold</c>/<c>font-semibold</c>, <c>Padding</c> → <c>p-*</c>,
/// <c>FlatAppearance</c> → <c>border-*</c>, <c>hover:bg-*</c>, <c>active:bg-*</c>. Çevrilen satırlar silinir ve yerine
/// <c>SetClasses</c> çağrısı yazılır; çevrilemeyenler (sistem renkleri, başka font aileleri) olduğu gibi bırakılıp not edilir.
/// </summary>
public sealed class DesignerConverter
{
    private static readonly Regex AssignmentRx = new Regex(
        @"^(?<indent>\s*)this\.(?:(?<control>\w+)\.)?(?<property>BackColor|ForeColor|Font|Padding|FlatAppearance\.BorderColor|FlatAppearance\.BorderSize|FlatAppearance\.MouseOverBackColor|FlatAppearance\.MouseDownBackColor)\s*=\s*(?<value>.+?);\s*$",
        RegexOptions.CultureInvariant);

    private static readonly Regex ProviderFieldRx = new Regex(@"this\.(?<name>\w+)\s*=\s*new\s+(?:global::)?(?:FormWind\.)?TailwindProvider\(", RegexOptions.CultureInvariant);
    private static readonly Regex SetClassesRx = new Regex(@"^\s*this\.(?<provider>\w+)\.SetClasses\(this(?:\.(?<control>\w+))?,\s*""(?<classes>[^""]*)""\);\s*$", RegexOptions.CultureInvariant);
    private static readonly Regex IntegerRx = new Regex(@"\d+", RegexOptions.CultureInvariant);

    private static readonly (string Name, float Px)[] FontSizes =
    {
        ("xs", 12), ("sm", 14), ("base", 16), ("lg", 18), ("xl", 20), ("2xl", 24), ("3xl", 30), ("4xl", 36),
    };

    private readonly TwTheme _theme;

    public DesignerConverter(TwTheme? theme = null)
    {
        _theme = theme ?? new TwTheme();
    }

    /// <summary>Renk farkı (RGB uzaklığı) bu değerin altındaysa palet rengi kullanılır; üstündeyse <c>[#hex]</c>.</summary>
    public double ColorTolerance { get; set; } = 10;

    public ConversionResult Convert(string source)
    {
        var newline = source.Contains("\r\n") ? "\r\n" : "\n";
        var lines = source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();

        var providerMatch = ProviderFieldRx.Match(source);
        bool providerAdded = !providerMatch.Success;
        string provider = providerMatch.Success ? providerMatch.Groups["name"].Value : "tailwindProvider1";

        var conversions = new Dictionary<string, ControlConversion>(StringComparer.Ordinal);
        var lastLine = new Dictionary<string, int>(StringComparer.Ordinal);
        var indents = new Dictionary<string, string>(StringComparer.Ordinal);
        var remove = new HashSet<int>();

        for (int i = 0; i < lines.Count; i++)
        {
            var match = AssignmentRx.Match(lines[i]);
            if (!match.Success) continue;
            var control = match.Groups["control"].Success ? match.Groups["control"].Value : "this";
            if (!conversions.TryGetValue(control, out var conversion)) conversions[control] = conversion = new ControlConversion(control);

            var classes = Translate(match.Groups["property"].Value, match.Groups["value"].Value, conversion.Notes);
            if (classes is null) continue;
            conversion.Classes.AddRange(classes);
            conversion.RemovedLines.Add(lines[i].Trim());
            remove.Add(i);
            lastLine[control] = i;
            indents[control] = match.Groups["indent"].Value;
        }

        // Var olan SetClasses çağrıları: yeni class'lar sonlarına eklenir.
        var existing = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < lines.Count; i++)
        {
            var match = SetClassesRx.Match(lines[i]);
            if (match.Success && match.Groups["provider"].Value == provider)
                existing[match.Groups["control"].Success ? match.Groups["control"].Value : "this"] = i;
        }

        var insertAfter = new Dictionary<int, List<string>>();
        foreach (var conversion in conversions.Values.Where(c => c.Classes.Count > 0))
        {
            var target = conversion.Control == "this" ? "this" : "this." + conversion.Control;
            if (existing.TryGetValue(conversion.Control, out var index))
            {
                var match = SetClassesRx.Match(lines[index]);
                var merged = Merge(match.Groups["classes"].Value, conversion.Classes);
                lines[index] = lines[index].Replace("\"" + match.Groups["classes"].Value + "\"", "\"" + merged + "\"");
                continue;
            }
            var line = $"{indents[conversion.Control]}this.{provider}.SetClasses({target}, \"{string.Join(" ", Dedupe(conversion.Classes))}\");";
            int after = lastLine[conversion.Control];
            if (!insertAfter.TryGetValue(after, out var list)) insertAfter[after] = list = new List<string>();
            list.Add(line);
        }

        var output = new List<string>(lines.Count + 8);
        for (int i = 0; i < lines.Count; i++)
        {
            if (!remove.Contains(i)) output.Add(lines[i]);
            if (insertAfter.TryGetValue(i, out var added)) output.AddRange(added);
        }

        bool changed = conversions.Values.Any(c => c.Classes.Count > 0);
        if (changed && providerAdded) AddProvider(output, provider);
        else providerAdded = false;

        return new ConversionResult(string.Join(newline, output), conversions.Values.ToList(), provider, providerAdded);
    }

    /// <summary>Bir atamayı class'lara çevirir; çevrilemiyorsa <c>null</c> (satır kalır, not düşülür).</summary>
    internal List<string>? Translate(string property, string value, List<string> notes)
    {
        switch (property)
        {
            case "BackColor":
                return Color("bg", value, notes, property);
            case "ForeColor":
                return Color("text", value, notes, property);
            case "FlatAppearance.BorderColor":
                return Color("border", value, notes, property);
            case "FlatAppearance.MouseOverBackColor":
                return Color("hover:bg", value, notes, property);
            case "FlatAppearance.MouseDownBackColor":
                return Color("active:bg", value, notes, property);
            case "FlatAppearance.BorderSize":
                if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var size)) break;
                return new List<string> { size switch { 0 => "border-0", 1 => "border", 2 => "border-2", 4 => "border-4", 8 => "border-8", _ => "border-2" } };
            case "Padding":
                return Padding(value, notes);
            case "Font":
                return Font(value, notes);
        }
        notes.Add($"{property} = {value}: çevrilemedi.");
        return null;
    }

    private List<string>? Color(string prefix, string value, List<string> notes, string property)
    {
        if (!TryParseColor(value, out var color, out var reason))
        {
            notes.Add($"{property} = {value}: {reason}; satır bırakıldı.");
            return null;
        }
        return new List<string> { prefix + "-" + ColorClass(color, out var approximate) }
            .Also(_ => { if (approximate != null) notes.Add($"{property}: {approximate}"); });
    }

    /// <summary>Paletteki adı (<c>blue-600</c>, <c>white</c>) ya da <c>[#1e40af]</c>.</summary>
    internal string ColorClass(Color color, out string? approximate)
    {
        approximate = null;
        if (color.A == 0) return "transparent";
        if (color.R == 255 && color.G == 255 && color.B == 255) return "white";
        if (color.R == 0 && color.G == 0 && color.B == 0) return "black";

        string? best = null;
        double bestDistance = double.MaxValue;
        foreach (var name in _theme.ColorNames)
        {
            foreach (var shade in TwPalette.Shades)
            {
                if (!_theme.TryGetColor(name, shade, out var candidate)) continue;
                double distance = Math.Sqrt(Math.Pow(candidate.R - color.R, 2) + Math.Pow(candidate.G - color.G, 2) + Math.Pow(candidate.B - color.B, 2));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = name + "-" + shade;
                }
            }
        }

        var hex = $"#{color.R:x2}{color.G:x2}{color.B:x2}";
        if (best != null && bestDistance == 0) return best;
        if (best != null && bestDistance <= ColorTolerance)
        {
            approximate = $"{hex} yerine en yakın palet rengi {best} kullanıldı (fark {bestDistance:0.#}).";
            return best;
        }
        return "[" + hex + "]";
    }

    internal static bool TryParseColor(string value, out Color color, out string reason)
    {
        color = System.Drawing.Color.Empty;
        reason = "";
        value = value.Trim();
        if (value.Contains("SystemColors."))
        {
            reason = "sistem rengi (tema ile değişir)";
            return false;
        }
        if (value.Contains("FromArgb("))
        {
            var numbers = IntegerRx.Matches(value.Substring(value.IndexOf("FromArgb(", StringComparison.Ordinal)))
                .Cast<Match>().Select(m => int.Parse(m.Value, CultureInfo.InvariantCulture)).ToList();
            if (numbers.Count == 3)
            {
                color = System.Drawing.Color.FromArgb(numbers[0], numbers[1], numbers[2]);
                return true;
            }
            if (numbers.Count == 4)
            {
                color = System.Drawing.Color.FromArgb(numbers[0], numbers[1], numbers[2], numbers[3]);
                return true;
            }
            reason = "FromArgb biçimi tanınmadı";
            return false;
        }
        var named = Regex.Match(value, @"Color\.(\w+)$");
        if (named.Success)
        {
            var known = System.Drawing.Color.FromName(named.Groups[1].Value);
            if (known.IsKnownColor)
            {
                color = System.Drawing.Color.FromArgb(known.A, known.R, known.G, known.B);
                return true;
            }
        }
        reason = "renk ifadesi tanınmadı";
        return false;
    }

    private static List<string>? Padding(string value, List<string> notes)
    {
        var numbers = IntegerRx.Matches(value).Cast<Match>().Select(m => int.Parse(m.Value, CultureInfo.InvariantCulture)).ToList();
        if (!value.Contains("Padding(") || (numbers.Count != 1 && numbers.Count != 4))
        {
            notes.Add($"Padding = {value}: tanınmadı.");
            return null;
        }
        if (numbers.Count == 1) return new List<string> { "p-" + Spacing(numbers[0]) };

        int left = numbers[0], top = numbers[1], right = numbers[2], bottom = numbers[3];
        if (left == right && top == bottom)
            return left == top ? new List<string> { "p-" + Spacing(left) } : new List<string> { "px-" + Spacing(left), "py-" + Spacing(top) };
        return new List<string> { "pl-" + Spacing(left), "pt-" + Spacing(top), "pr-" + Spacing(right), "pb-" + Spacing(bottom) };
    }

    /// <summary>Piksel → boşluk ölçeği (1 birim = 4 px): 8 → <c>2</c>, 6 → <c>1.5</c>, 7 → <c>[7px]</c>.</summary>
    internal static string Spacing(int pixels)
    {
        if (pixels % 4 == 0) return (pixels / 4).ToString(CultureInfo.InvariantCulture);
        if (pixels % 2 == 0) return (pixels / 4).ToString(CultureInfo.InvariantCulture) + ".5";
        if (pixels == 1) return "px";
        return "[" + pixels.ToString(CultureInfo.InvariantCulture) + "px]";
    }

    private List<string>? Font(string value, List<string> notes)
    {
        var match = Regex.Match(value, @"Font\(\s*""(?<family>[^""]+)""\s*,\s*(?<size>[\d.]+)F?(?<rest>.*)\)$", RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            notes.Add($"Font = {value}: tanınmadı.");
            return null;
        }

        var family = match.Groups["family"].Value;
        var rest = match.Groups["rest"].Value;
        float size = float.Parse(match.Groups["size"].Value, CultureInfo.InvariantCulture);
        bool pixelUnit = rest.Contains("GraphicsUnit.Pixel");
        float px = pixelUnit ? size : size * 96f / 72f;

        var classes = new List<string>();
        var (name, nearest) = FontSizes.OrderBy(f => Math.Abs(f.Px - px)).First();
        classes.Add(Math.Abs(nearest - px) <= 0.5f ? "text-" + name : "text-[" + Math.Round(px).ToString(CultureInfo.InvariantCulture) + "px]");

        // Ağırlık: aile adındaki kalınlık ("Segoe UI Semibold") ya da FontStyle.Bold.
        var baseFamily = family;
        foreach (var (suffix, weight) in new[] { (" Semibold", "font-semibold"), (" Light", "font-light"), (" Semilight", "font-light"), (" Black", "font-black"), (" Bold", "font-bold") })
        {
            if (family.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                classes.Add(weight);
                baseFamily = family.Substring(0, family.Length - suffix.Length);
                break;
            }
        }
        if (rest.Contains("FontStyle.Bold") && !classes.Any(c => c.StartsWith("font-", StringComparison.Ordinal))) classes.Add("font-bold");
        if (rest.Contains("FontStyle.Italic")) classes.Add("italic");
        if (rest.Contains("FontStyle.Underline")) classes.Add("underline");

        if (!string.Equals(baseFamily, _theme.FontFamilySans, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(baseFamily, "Microsoft Sans Serif", StringComparison.OrdinalIgnoreCase))
        {
            notes.Add($"Font ailesi \"{family}\" temadakinden ({_theme.FontFamilySans}) farklı; satır bırakıldı. Tema dosyasına ekleyip font-sans ile kullanabilirsiniz.");
            return null;
        }
        return classes;
    }

    private static IEnumerable<string> Dedupe(IEnumerable<string> classes) => classes.Distinct(StringComparer.Ordinal);

    private static string Merge(string existing, IEnumerable<string> added) =>
        string.Join(" ", Dedupe(existing.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Concat(added)));

    // Alan ve InitializeComponent başına oluşturma satırı.
    private static void AddProvider(List<string> lines, string provider)
    {
        int init = lines.FindIndex(l => Regex.IsMatch(l, @"void\s+InitializeComponent\s*\("));
        if (init < 0) return;
        int brace = lines.FindIndex(init, l => l.Trim() == "{");
        if (brace < 0) return;
        var indent = Regex.Match(lines[brace], @"^\s*").Value + "    ";

        int components = lines.FindIndex(brace, l => l.Contains("this.components = new"));
        var creation = new List<string>();
        if (components < 0) creation.Add($"{indent}this.components = new System.ComponentModel.Container();");
        creation.Add($"{indent}this.{provider} = new FormWind.TailwindProvider(this.components);");
        lines.InsertRange(components >= 0 ? components + 1 : brace + 1, creation);

        int lastField = lines.FindLastIndex(l => Regex.IsMatch(l, @"^\s*private\s+[\w.]+\s+\w+;\s*$"));
        if (lastField >= 0)
        {
            var fieldIndent = Regex.Match(lines[lastField], @"^\s*").Value;
            lines.Insert(lastField + 1, $"{fieldIndent}private FormWind.TailwindProvider {provider};");
        }
    }
}

internal static class FluentExtensions
{
    public static T Also<T>(this T value, Action<T> action)
    {
        action(value);
        return value;
    }
}
