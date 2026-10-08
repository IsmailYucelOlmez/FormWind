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
        /// Normal çalıştırma: vitrin formunu açar.
        /// <c>--snapshot çıktı.png [--dark]</c>: formu ekran dışında açıp görüntüsünü kaydeder (görsel testler için).
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

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
            using var form = new MainForm
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-10000, -10000),
                ShowInTaskbar = false,
            };
            form.Show();
            form.DarkMode = dark;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--focus") >= 0) form.FocusFirstInput();
            Application.DoEvents();

            using var bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
            bitmap.Save(path, ImageFormat.Png);
        }
    }
}
