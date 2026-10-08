using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using FormWind;

namespace Rickipedia
{
    internal static class Program
    {
        /// <summary>
        /// Normal çalıştırma: sayfayı açar. <c>--snapshot çıktı.png [--dark]</c>: veriler ve avatarlar yüklendikten
        /// sonra formun görüntüsünü kaydeder.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            // .NET Framework 4.7.2 işletim sistemi varsayılanını kullanır; eski sistemlerde TLS 1.2 açıkça istenir.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.AddMessageFilter(new WheelRedirector());

            TwTheme.Current = TwTheme.LoadIfExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tw.config.json"));

            if (args.Length >= 2 && args[0] == "--snapshot")
            {
                Snapshot(args[1], Array.IndexOf(args, "--dark") >= 0);
                return;
            }

            Application.Run(new MainForm());
        }

        private static void Snapshot(string path, bool dark)
        {
            using (var form = new MainForm { StartPosition = FormStartPosition.Manual, Location = new Point(-10000, -10000), ShowInTaskbar = false })
            {
                form.Show();
                form.DarkMode = dark;

                var deadline = DateTime.UtcNow.AddSeconds(30);
                while (!form.IsReadyForSnapshot && DateTime.UtcNow < deadline)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(20);
                }
                Application.DoEvents();

                Save(form, path);

                // --menu: açık filtre menüsü, --details: ayrıntı penceresi (ayrı dosyalara)
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "--menu") >= 0)
                {
                    var menu = form.OpenStatusMenu();
                    Application.DoEvents();
                    Save(menu, Path.ChangeExtension(path, ".menu.png"));
                    menu.Close();
                }

                if (Array.IndexOf(Environment.GetCommandLineArgs(), "--details") >= 0)
                {
                    using (var details = form.CreateFirstDetails())
                    {
                        details.StartPosition = FormStartPosition.Manual;
                        details.Location = new Point(-10000, -10000);
                        details.Show();
                        var until = DateTime.UtcNow.AddSeconds(5);
                        while (DateTime.UtcNow < until) { Application.DoEvents(); System.Threading.Thread.Sleep(20); }
                        Save(details, Path.ChangeExtension(path, ".details.png"));
                    }
                }
            }
        }

        private static void Save(Control control, string path)
        {
            using (var bitmap = new Bitmap(control.Width, control.Height))
            {
                control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
                bitmap.Save(path, ImageFormat.Png);
            }
        }
    }
}
