using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FormWind.Convert;

/// <summary>
/// <code>
/// formwind-convert &lt;dosya.Designer.cs | klasör&gt; [--write] [--config tw.config.json] [--report rapor.md] [--tolerance 10]
/// </code>
/// Varsayılan kuru çalıştırmadır: ne değişeceğini yazar, dosyaya dokunmaz. <c>--write</c> dosyaları yerinde günceller
/// (önce değişiklikleri commit'leyin; geri dönüş git ile yapılır).
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine("Kullanım: formwind-convert <dosya.Designer.cs | klasör> [--write] [--config tw.config.json] [--report rapor.md] [--tolerance 10]");
            Console.WriteLine("Sabit BackColor/ForeColor/Font/Padding/FlatAppearance atamalarını TailwindProvider class'larına çevirir.");
            Console.WriteLine("Varsayılan kuru çalıştırmadır; --write dosyaları günceller.");
            return args.Length == 0 ? 1 : 0;
        }

        string target = args[0];
        bool write = args.Contains("--write");
        string? config = Option(args, "--config");
        string? report = Option(args, "--report");
        double tolerance = double.TryParse(Option(args, "--tolerance"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var t) ? t : 10;

        var files = Directory.Exists(target)
            ? Directory.GetFiles(target, "*.Designer.cs", SearchOption.AllDirectories).Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)).ToArray()
            : File.Exists(target) ? new[] { target } : Array.Empty<string>();
        if (files.Length == 0)
        {
            Console.Error.WriteLine($"Designer dosyası bulunamadı: {target}");
            return 2;
        }

        var theme = config != null ? TwTheme.Load(config) : new TwTheme();
        var converter = new DesignerConverter(theme) { ColorTolerance = tolerance };
        var markdown = new StringBuilder("# FormWind dönüştürme raporu\n\n");
        int changedFiles = 0, classCount = 0, noteCount = 0;

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            var result = converter.Convert(source);
            var relevant = result.Controls.Where(c => c.Classes.Count > 0 || c.Notes.Count > 0).ToList();
            if (relevant.Count == 0) continue;

            Console.WriteLine($"\n{file}");
            markdown.Append($"## {Path.GetFileName(file)}\n\n");
            if (result.ProviderAdded)
            {
                Console.WriteLine($"  + TailwindProvider eklendi ({result.ProviderName})");
                markdown.Append($"TailwindProvider eklendi: `{result.ProviderName}`.\n\n");
            }
            foreach (var control in relevant)
            {
                var classes = string.Join(" ", control.Classes.Distinct());
                Console.WriteLine($"  {control.Control}: {(classes.Length > 0 ? classes : "(çevrilen yok)")}");
                markdown.Append($"- **{control.Control}**: `{classes}`\n");
                foreach (var note in control.Notes)
                {
                    Console.WriteLine($"    ! {note}");
                    markdown.Append($"  - ⚠ {note}\n");
                }
                classCount += control.Classes.Count;
                noteCount += control.Notes.Count;
            }
            markdown.Append('\n');

            if (result.Changed)
            {
                changedFiles++;
                if (write) File.WriteAllText(file, result.Source, new UTF8Encoding(true));
            }
        }

        Console.WriteLine($"\n{changedFiles} dosya, {classCount} class, {noteCount} not. {(write ? "Dosyalar güncellendi." : "Kuru çalıştırma: dosyalar değişmedi (--write ile uygulayın).")}");
        if (report != null) File.WriteAllText(report, markdown.ToString(), new UTF8Encoding(true));
        return 0;
    }

    private static string? Option(IReadOnlyList<string> args, string name)
    {
        for (int i = 0; i < args.Count - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }
}
