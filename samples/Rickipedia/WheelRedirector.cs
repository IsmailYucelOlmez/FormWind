using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Rickipedia
{
    /// <summary>
    /// WinForms fare tekerleğini odaktaki kontrole gönderir; liste paneli odaklanamadığı için tekerlek, imlecin
    /// altındaki ilk kaydırılabilir konteynere yönlendirilir (web'deki davranış).
    /// </summary>
    internal sealed class WheelRedirector : IMessageFilter
    {
        private const int WmMouseWheel = 0x020A;

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WmMouseWheel) return false;

            var screen = new Point(unchecked((short)(long)m.LParam), unchecked((short)((long)m.LParam >> 16)));
            for (var control = Control.FromChildHandle(WindowFromPoint(screen)); control != null; control = control.Parent)
            {
                if (control is ScrollableControl scrollable && scrollable.AutoScroll && scrollable.VerticalScroll.Visible)
                {
                    int delta = unchecked((short)((long)m.WParam >> 16));
                    var y = -scrollable.AutoScrollPosition.Y - delta / 120 * 3 * 24;
                    scrollable.AutoScrollPosition = new Point(0, Math.Max(0, y));
                    return true;
                }
            }
            return false;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point point);
    }
}
