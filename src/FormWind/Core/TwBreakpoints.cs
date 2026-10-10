using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// Responsive varyantların eşikleri (Tailwind ile aynı, 96 DPI pikseli): <c>sm:</c> 640, <c>md:</c> 768, <c>lg:</c> 1024,
/// <c>xl:</c> 1280, <c>2xl:</c> 1536. Tarayıcıdaki pencere genişliği yerine kontrolün bulunduğu formun istemci genişliği
/// ölçülür; <c>md:flex-row</c> form 768 px ve üzerindeyken uygulanır.
/// </summary>
public static class TwBreakpoints
{
    public static int Sm { get; set; } = 640;
    public static int Md { get; set; } = 768;
    public static int Lg { get; set; } = 1024;
    public static int Xl { get; set; } = 1280;
    public static int Xxl { get; set; } = 1536;

    /// <summary>Genişliğe (96 DPI pikseli) karşılık gelen eşik bayrakları.</summary>
    public static TwState ForWidth(int width)
    {
        var state = TwState.None;
        if (width >= Sm) state |= TwState.Sm;
        if (width >= Md) state |= TwState.Md;
        if (width >= Lg) state |= TwState.Lg;
        if (width >= Xl) state |= TwState.Xl;
        if (width >= Xxl) state |= TwState.Xxl;
        return state;
    }

    /// <summary>Kontrolün formunun (yoksa en üst ebeveyninin) genişliğine göre eşikler.</summary>
    public static TwState For(Control control)
    {
        Control top = control.FindForm() ?? control;
        while (top.Parent != null && top is not Form) top = top.Parent;
        int width = (int)(top.ClientSize.Width * 96f / TwDpi.DeviceDpi(top));
        return ForWidth(width);
    }
}
