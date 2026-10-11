using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FormWind;

/// <summary>Hesaplanan stili belirli bir kontrol ailesine uygular.</summary>
public interface IStyleApplier
{
    bool CanApply(Control control);

    /// <param name="control">Stillenecek kontrol.</param>
    /// <param name="style">Kontrolün o anki durumu için birleştirilmiş stil.</param>
    /// <param name="context">Parse sonucu, durum, DPI ölçeği ve kontrolün orijinal değerleri.</param>
    void Apply(Control control, TwStyle style, TwApplyContext context);
}

public sealed class TwApplyContext
{
    public TwApplyContext(TwParsed parsed, TwState state, float scale, TwTheme theme, TwOriginal original, bool designMode)
    {
        Parsed = parsed ?? throw new ArgumentNullException(nameof(parsed));
        State = state;
        Scale = scale;
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));
        Original = original ?? throw new ArgumentNullException(nameof(original));
        DesignMode = designMode;
    }

    public TwParsed Parsed { get; }
    public TwState State { get; }

    /// <summary><c>DeviceDpi / 96</c>.</summary>
    public float Scale { get; }

    public TwTheme Theme { get; }
    public TwOriginal Original { get; }
    public bool DesignMode { get; }

    /// <summary>Koyu temada native kaydırma çubukları da koyulaştırılsın mı (<see cref="TailwindProvider.DarkScrollbars"/>).</summary>
    public bool DarkScrollbars { get; init; } = true;
}

/// <summary>
/// Kayıtlı uygulayıcılar. Sonra kaydedilen önce denenir; hiçbiri uymazsa <see cref="WinFormsApplier"/> kullanılır.
/// </summary>
public static class TwAppliers
{
    private static readonly object Gate = new object();
    private static readonly List<IStyleApplier> Registered = new List<IStyleApplier>
    {
        new DataGridViewApplier(),
        new ToolStripApplier(),
        new FormApplier(),
    };
    private static readonly IStyleApplier Fallback = new WinFormsApplier();
    private static readonly IStyleApplier[] BuiltIn = Registered.ToArray();

    public static void Register(IStyleApplier applier)
    {
        if (applier is null) throw new ArgumentNullException(nameof(applier));
        lock (Gate) Registered.Insert(0, applier);
    }

    public static IStyleApplier Find(Control control)
    {
        lock (Gate)
        {
            foreach (var applier in Registered)
            {
                if (applier.CanApply(control)) return applier;
            }
        }
        return Fallback;
    }

    /// <summary>
    /// FormWind'in kendi uygulayıcısı; kayıtlı uygulayıcılar atlanır. Bir kontrol kütüphanesinin uygulayıcısı yapısal
    /// stillemeyi (örn. DataGridView satır yüksekliği, ızgara çizgileri) buna bırakıp yalnızca kendi paletini doldurabilir.
    /// </summary>
    public static IStyleApplier FindBuiltIn(Control control)
    {
        if (control is null) throw new ArgumentNullException(nameof(control));
        foreach (var applier in BuiltIn)
        {
            if (applier.CanApply(control)) return applier;
        }
        return Fallback;
    }
}
