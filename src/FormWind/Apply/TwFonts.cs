using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace FormWind;

/// <summary>
/// Paylaşılan <see cref="Font"/> örnekleri ve uygulamayla dağıtılan fontlar. Her <c>new Font</c> bir GDI handle'ı
/// tuttuğundan aynı aile/boyut/stil için tek örnek kullanılır; örnekler süreç boyunca yaşar ve dispose edilmemelidir.
/// Kontroller fontları tuttuğundan önbellek küçültülemez; <see cref="CacheWarningThreshold"/> aşılınca bir kez Debug uyarısı yazılır
/// (genelde kodla üretilen <c>text-[…px]</c> class'larının işareti).
/// </summary>
public static class TwFonts
{
    private const uint FrPrivate = 0x10;

    // İlk statik alan: GDI+ başlamadan WinForms DPI ayarı uygulanır (bkz. TwDpi).
    private static readonly bool DpiReady = TwDpi.EnsureInitialized();

    private static readonly ConcurrentDictionary<(string Family, float Size, FontStyle Style), Font> Cache =
        new ConcurrentDictionary<(string Family, float Size, FontStyle Style), Font>();

    /// <summary>Önbellekteki font sayısı bunu aşınca Debug çıktısına bir kez uyarı yazılır.</summary>
    public const int CacheWarningThreshold = 256;

    private static int _warned;

    private static readonly object Gate = new object();
    private static readonly PrivateFontCollection Private = new PrivateFontCollection();
    private static readonly Dictionary<string, FontFamily> PrivateFamilies = new Dictionary<string, FontFamily>(StringComparer.OrdinalIgnoreCase);

    private static readonly Lazy<HashSet<string>> Installed = new Lazy<HashSet<string>>(() =>
    {
        using var collection = new InstalledFontCollection();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var family in collection.Families) names.Add(family.Name);
        return names;
    });

    /// <summary>
    /// Bir font dosyasını kurulum gerektirmeden bu süreç için kaydeder: hem GDI+ (<see cref="PrivateFontCollection"/>) hem
    /// GDI (<c>AddFontResourceEx</c>, FR_PRIVATE) tarafında; böylece TextRenderer ve kontroller de fontu bulur.
    /// Dosyadaki aile adlarını döner. Aynı dosyayı tekrar kaydetmek zararsızdır.
    /// </summary>
    public static IReadOnlyList<string> Register(string path)
    {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Font dosyası bulunamadı.", fullPath);

        string[] names;
        using (var probe = new PrivateFontCollection())
        {
            probe.AddFontFile(fullPath);
            names = probe.Families.Select(f => f.Name).ToArray();
        }

        lock (Gate)
        {
            AddFontResourceEx(fullPath, FrPrivate, IntPtr.Zero);
            Private.AddFontFile(fullPath);
            foreach (var family in Private.Families)
                PrivateFamilies[family.Name] = family;
        }
        return names;
    }

    /// <summary>Kurulu ya da <see cref="Register"/> ile kaydedilmiş bir font ailesi mi.</summary>
    public static bool IsInstalled(string family)
    {
        lock (Gate)
        {
            if (PrivateFamilies.ContainsKey(family)) return true;
        }
        return Installed.Value.Contains(family);
    }

    /// <summary>Hiçbir aile bulunamadığında kullanılan sistem arayüz fontu (genelde Segoe UI).</summary>
    public static string DefaultFamily { get; } = SystemFonts.MessageBoxFont?.FontFamily.Name ?? "Segoe UI";

    /// <summary>
    /// CSS font yığınını çözer: <c>"Quicksand, 'Segoe UI', sans-serif"</c> → kullanılabilen ilk aile.
    /// Hiçbiri yoksa sistem arayüz fontu (Segoe UI) döner.
    /// </summary>
    public static string ResolveFamily(string stack)
    {
        if (string.IsNullOrWhiteSpace(stack)) return DefaultFamily;
        if (stack.IndexOf(',') < 0 && IsInstalled(stack.Trim())) return stack.Trim();

        foreach (var part in stack.Split(','))
        {
            var name = part.Trim().Trim('"', '\'');
            if (name.Length > 0 && IsInstalled(name)) return name;
        }
        return DefaultFamily;
    }

    public static Font Get(string family, float sizeInPoints, FontStyle style)
    {
        var resolved = ResolveFamily(family);
        var key = (resolved, (float)Math.Round(sizeInPoints, 2), style);
        return Cache.GetOrAdd(key, k =>
        {
            if (Cache.Count >= CacheWarningThreshold && System.Threading.Interlocked.Exchange(ref _warned, 1) == 0)
                Debug.WriteLine($"FormWind: font önbelleği {CacheWarningThreshold} örneği aştı; class'larda çok sayıda farklı yazı boyutu kullanılıyor olabilir.");
            FontFamily? privateFamily;
            lock (Gate) PrivateFamilies.TryGetValue(k.Item1, out privateFamily);
            return privateFamily != null && privateFamily.IsStyleAvailable(k.Item3)
                ? new Font(privateFamily, k.Item2, k.Item3, GraphicsUnit.Point)
                : new Font(k.Item1, k.Item2, k.Item3, GraphicsUnit.Point);
        });
    }

    /// <summary>Önbellekteki font sayısı (testler için).</summary>
    internal static int CachedCount => Cache.Count;

    /// <summary>
    /// Stilin font değerlerini <paramref name="baseFont"/> üzerine uygular. Kalınlık, temadaki
    /// ağırlık ailelerine (örn. 600 → "Segoe UI Semibold") çözülür.
    /// </summary>
    public static Font Create(TwStyle style, Font baseFont, TwTheme theme) => Create(style, baseFont, theme, 1f);

    /// <summary><see cref="Create(TwStyle, Font, TwTheme)"/>; <c>text-*</c> boyutları <paramref name="pixelScale"/> ile çarpılır.</summary>
    /// <param name="style">Uygulanacak stil.</param>
    /// <param name="baseFont">Stilde verilmeyen değerlerin alındığı font.</param>
    /// <param name="theme">Kalınlık aileleri için tema.</param>
    /// <param name="pixelScale">
    /// <c>text-*</c> boyutlarının çarpanı: PerMonitorV2'de kontrolün ekranı sistem DPI'ından farklıysa
    /// <see cref="TwDpi.FontScale"/>. Temel fonttan gelen boyut zaten o ekrana göredir ve çarpılmaz.
    /// </param>
    public static Font Create(TwStyle style, Font baseFont, TwTheme theme, float pixelScale)
    {
        var family = ResolveFamily(style.FontFamily ?? baseFont.FontFamily.Name);
        var size = style.FontSizePx is float px ? px * 0.75f * pixelScale : baseFont.SizeInPoints;
        var weight = style.FontWeight ?? (baseFont.Bold ? 700 : 400);
        var (face, bold) = theme.ResolveFontWeight(family, weight);

        var fontStyle = bold ? FontStyle.Bold : FontStyle.Regular;
        if (style.Italic ?? baseFont.Italic) fontStyle |= FontStyle.Italic;
        if (style.Underline ?? baseFont.Underline) fontStyle |= FontStyle.Underline;
        return Get(face, size, fontStyle);
    }

    internal static void TryRegister(string path)
    {
        try
        {
            Register(path);
        }
        catch (Exception ex) when (ex is FileNotFoundException || ex is ArgumentException || ex is ExternalException)
        {
            // Font isteğe bağlıdır; yığındaki bir sonraki aile kullanılır.
            Debug.WriteLine($"FormWind: font yüklenemedi ({path}): {ex.Message}");
        }
    }

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern int AddFontResourceEx(string path, uint flags, IntPtr reserved);
}
