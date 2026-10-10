using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace FormWind.Showcase
{
    internal static class Program
    {
        /// <summary>
        /// Normal çalıştırma: vitrin formunu açar (<c>--controls</c>: kontroller formu).
        /// <c>--snapshot çıktı.png [--dark] [--controls] [--focus] [--dpi 192]</c>: formu ekran dışında açıp görüntüsünü kaydeder.
        /// <c>--dpi</c> formu başka DPI'lı bir ekrana taşınmış gibi ölçekler (WM_DPICHANGED); yalnızca uygulama PerMonitorV2
        /// çalışıyorsa etkilidir (App.config'teki DpiAwareness açıklamasına bakın).
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            TwTheme.Current = TwTheme.LoadIfExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tw.config.json"));

            bool controls = Array.IndexOf(args, "--controls") >= 0;
            if (args.Length >= 2 && args[0] == "--snapshot")
            {
                bool dark = Array.IndexOf(args, "--dark") >= 0;
                using var form = controls ? (Form)new ControlsForm { DarkMode = dark } : new MainForm { DarkMode = dark };
                if (Array.IndexOf(args, "--focus") >= 0 && form is MainForm main) main.Shown += (_, _) => main.FocusFirstInput();
                int dpiIndex = Array.IndexOf(args, "--dpi");
                int? dpi = dpiIndex >= 0 && dpiIndex + 1 < args.Length ? int.Parse(args[dpiIndex + 1]) : (int?)null;
                using var bitmap = Snapshots.Render(form, dpi);
                bitmap.Save(args[1], ImageFormat.Png);
                if (dpi != null)
                {
                    // Görsel DPI testleri referansı sürecin sistem DPI'ı ve gerçekleşen form DPI'ı ile seçer.
                    using var screen = Graphics.FromHwnd(IntPtr.Zero);
                    File.WriteAllText(args[1] + ".txt", $"system={screen.DpiX:0};form={form.DeviceDpi}");
                }
                return;
            }

            Application.Run(controls ? (Form)new ControlsForm() : new MainForm());
        }
    }

    /// <summary>Formu ekran dışında açıp görüntüsünü alır. Görsel regresyon testleri de bunu kullanır.</summary>
    public static class Snapshots
    {
        public static Bitmap Render(Form form) => Render(form, null);

        /// <param name="form">Çizilecek form.</param>
        /// <param name="dpi">Verilirse form bu DPI'lı bir ekrana taşınmış gibi ölçeklenir (yalnızca PerMonitorV2).</param>
        public static Bitmap Render(Form form, int? dpi)
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-10000, -10000);
            form.ShowInTaskbar = false;
            form.Show();
            Application.DoEvents();

            if (dpi is int target && target != form.DeviceDpi)
            {
                // WM_DPICHANGED: wParam = yeni DPI (x, y), lParam = önerilen pencere dikdörtgeni.
                float factor = target / (float)form.DeviceDpi;
                var rect = new[] { form.Left, form.Top, form.Left + (int)(form.Width * factor), form.Top + (int)(form.Height * factor) };
                var handle = System.Runtime.InteropServices.GCHandle.Alloc(rect, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
                    SendMessage(form.Handle, 0x02E0, (IntPtr)((target << 16) | target), handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
                Application.DoEvents();
            }

            var bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
            return bitmap;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);
    }
}
