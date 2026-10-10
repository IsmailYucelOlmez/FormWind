using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// .NET Framework'te WinForms, app.config'teki <c>DpiAwareness</c> ayarını ilk <see cref="Control"/> oluşturulurken uygular.
/// GDI+ ise ekran DPI'ını başlatıldığı anda bir kez okur: ondan önce bir <c>Font</c> oluşturulursa (örn. tema yüklenirken)
/// PerMonitorV2 uygulamada GDI+ 96 DPI'da kalır ve native kontrollerin (TextBox) fontları küçük çizilir. FormWind GDI+'ya
/// dokunmadan önce bunu çağırır.
/// </summary>
internal static class TwDpi
{
#if NETFRAMEWORK
    private static bool _initialized;
#endif

    private static int _systemDpi;

    /// <summary>
    /// Sürecin sistem DPI'ı (başlangıçtaki birincil ekran). Font boyutları punto cinsinden bu DPI'a göre piksele çevrilir;
    /// PerMonitorV2'de başka DPI'lı bir ekrandaki kontrol için fontun <see cref="FontScale"/> ile büyütülmesi gerekir.
    /// </summary>
    public static int SystemDpi
    {
        get
        {
            if (_systemDpi == 0)
            {
                EnsureInitialized();
                using var g = System.Drawing.Graphics.FromHwnd(System.IntPtr.Zero);
                _systemDpi = (int)System.Math.Round(g.DpiY);
            }
            return _systemDpi;
        }
    }

    /// <summary>Kontrolün ekranı ile sistem DPI'ı arasındaki oran (DPI-unaware ve system-aware uygulamalarda 1).</summary>
    public static float FontScale(Control control) => DeviceDpi(control) / (float)SystemDpi;

    /// <summary>
    /// Kontrolün bulunduğu ekranın DPI'ı. .NET Framework'te bazı kontroller (örn. Label) ekran değişince kendi
    /// <c>DeviceDpi</c>'ını güncellemez; formun değeri esas alınır.
    /// </summary>
    public static int DeviceDpi(Control control) => control.FindForm()?.DeviceDpi ?? control.DeviceDpi;

    public static bool EnsureInitialized()
    {
#if NETFRAMEWORK
        if (!_initialized)
        {
            _initialized = true;
            // Handle oluşturmaz; yalnızca WinForms'un DPI başlatmasını tetikler.
            using (new Control()) { }
        }
#endif
        return true;
    }
}
