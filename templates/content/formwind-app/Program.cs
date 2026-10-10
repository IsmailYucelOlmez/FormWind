using System;
using System.IO;
using System.Windows.Forms;
using FormWind;

namespace FormWindApp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
#if NET
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
#endif
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Load the theme (palette, fonts, component classes) before any form is created.
        TwTheme.Current = TwTheme.LoadIfExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tw.config.json"));

        using (var signIn = new SignInForm())
        {
            if (signIn.ShowDialog() != DialogResult.OK) return;
        }
        Application.Run(new MainForm());
    }
}
