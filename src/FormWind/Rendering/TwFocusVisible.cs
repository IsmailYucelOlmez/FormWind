using System;
using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// Son girişin klavyeden mi fareden mi geldiğini izler (tarayıcılardaki <c>:focus-visible</c> gibi). Odak halkaları ve
/// <c>focus-visible:</c> varyantı yalnızca odak klavyeyle (Tab, oklar) geldiğinde gösterilir; fareyle tıklanan kontrolde
/// görünmez. İzleme, ilk FormWind kontrolü ya da sağlayıcısı oluşturulduğunda o thread'in mesaj döngüsüne eklenir.
/// </summary>
public static class TwFocusVisible
{
    [ThreadStatic] private static bool _installed;
    [ThreadStatic] private static bool _keyboard;

    /// <summary>Son giriş klavyeden mi geldi. Başlangıçta <c>false</c>: program açılışında verilen odakta halka çizilmez.</summary>
    public static bool LastInputWasKeyboard
    {
        get => _keyboard;
        set => _keyboard = value;
    }

    internal static void EnsureInstalled()
    {
        if (_installed) return;
        _installed = true;
        Application.AddMessageFilter(new Filter());
    }

    private sealed class Filter : IMessageFilter
    {
        private const int WmKeyDown = 0x0100, WmSysKeyDown = 0x0104;
        private const int WmLButtonDown = 0x0201, WmRButtonDown = 0x0204, WmMButtonDown = 0x0207, WmXButtonDown = 0x020B;
        private const int WmNcLButtonDown = 0x00A1, WmPointerDown = 0x0246;

        public bool PreFilterMessage(ref Message m)
        {
            switch (m.Msg)
            {
                case WmKeyDown:
                case WmSysKeyDown:
                    // Tek başına değiştirici tuşlar (Ctrl, Shift, Alt, Win) gezinme sayılmaz.
                    var key = (Keys)(int)(long)m.WParam;
                    if (key is not (Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)) _keyboard = true;
                    break;
                case WmLButtonDown:
                case WmRButtonDown:
                case WmMButtonDown:
                case WmXButtonDown:
                case WmNcLButtonDown:
                case WmPointerDown:
                    _keyboard = false;
                    break;
            }
            return false;
        }
    }
}
