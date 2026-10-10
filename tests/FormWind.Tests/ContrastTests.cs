using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FormWind.Tests.Performance;
using Xunit;

namespace FormWind.Tests;

public class ContrastTests
{
    [Fact]
    public void Ratio_matches_wcag_reference_values()
    {
        Assert.Equal(21.0, TwContrast.Ratio(Color.Black, Color.White), 2);
        Assert.Equal(1.0, TwContrast.Ratio(Color.Red, Color.Red), 2);
        // #767676 beyaz üzerinde AA eşiğini tam geçen gri olarak bilinir.
        Assert.Equal(4.54, TwContrast.Ratio(ColorTranslator.FromHtml("#767676"), Color.White), 2);
        Assert.Equal(TwContrast.Ratio(Color.Navy, Color.White), TwContrast.Ratio(Color.White, Color.Navy), 6);
    }

    [Fact]
    public void Audit_reports_unreadable_text_and_skips_disabled_controls()
    {
        using var panel = new Panel { BackColor = Color.White };
        var unreadable = new Label { Name = "unreadable", ForeColor = ColorTranslator.FromHtml("#eeeeee") };
        var disabled = new Label { Name = "disabled", ForeColor = ColorTranslator.FromHtml("#eeeeee"), Enabled = false };
        var readable = new Label { Name = "readable", ForeColor = Color.Black };
        panel.Controls.AddRange(new Control[] { unreadable, disabled, readable });

        var issue = Assert.Single(TwContrast.Audit(panel));

        Assert.Same(unreadable, issue.Control);
        Assert.Equal("text", issue.Part);
        Assert.True(issue.Ratio < TwContrast.TextMinimum);
    }

    [Fact]
    public void Large_text_uses_the_lower_threshold()
    {
        using var panel = new Panel { BackColor = Color.White };
        var gray = ColorTranslator.FromHtml("#949494"); // ~3:1
        panel.Controls.Add(new Label { ForeColor = gray, Font = new Font("Segoe UI", 9f) });
        panel.Controls.Add(new Label { ForeColor = gray, Font = new Font("Segoe UI", 20f) });

        var issue = Assert.Single(TwContrast.Audit(panel));
        Assert.Equal(9f, issue.Control.Font.SizeInPoints, 1);
    }

    [Fact]
    public void Transparent_background_is_composited_over_the_parent()
    {
        using var panel = new Panel { BackColor = Color.Black };
        panel.Controls.Add(new Label { BackColor = Color.Transparent, ForeColor = Color.Black });

        var issue = Assert.Single(TwContrast.Audit(panel));
        Assert.Equal(Color.Black.ToArgb(), issue.Background.ToArgb());
    }

    [Fact]
    public void Audit_checks_toolstrip_hover_colors() => Sta.Run(() =>
    {
        using var provider = new TailwindProvider();
        using var form = new Form();
        var derived = new ToolStrip { Name = "derived" };
        var whiteHover = new ToolStrip { Name = "whiteHover" };
        form.Controls.Add(derived);
        form.Controls.Add(whiteHover);
        provider.SetClasses(derived, "bg-gray-800 text-gray-100");
        provider.SetClasses(whiteHover, "bg-gray-800 text-gray-100 item:hover:bg-white");

        var issues = TwContrast.Audit(form);

        Assert.DoesNotContain(issues, i => i.Control == derived);
        Assert.Contains(issues, i => i.Control == whiteHover && i.Part == "item:hover");
    });

    [Fact]
    public void Audit_with_provider_checks_hover_variants() => Sta.Run(() =>
    {
        using var provider = new TailwindProvider();
        using var form = new Form { BackColor = Color.White };
        var button = new Button { Text = "Kaydet" };
        form.Controls.Add(button);
        provider.SetClasses(button, "bg-white text-gray-900 hover:bg-gray-900");

        Assert.Empty(TwContrast.Audit(form));
        var issue = Assert.Single(TwContrast.Audit(form, provider));
        Assert.Equal("hover", issue.Part);
    });

    [Fact]
    public void Audit_checks_the_final_color_of_a_running_transition() => Sta.Run(() =>
    {
        using var provider = new TailwindProvider();
        using var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-10000, -10000), ShowInTaskbar = false };
        var button = new Button { Text = "Kaydet" };
        form.Controls.Add(button);
        form.Show();
        provider.SetClasses(button, "transition bg-white text-gray-900 dark:text-gray-100");
        TwAnimator.Step(DateTime.UtcNow.AddSeconds(10));

        provider.DarkMode = true; // yazı rengi geçişte; henüz koyu
        Assert.True(TwAnimator.IsAnimating(button, TwColorChannel.Fore));

        var issue = Assert.Single(TwContrast.Audit(form));
        Assert.Equal(ColorTranslator.FromHtml("#f3f4f6").ToArgb(), issue.Foreground.ToArgb());
        form.Close();
    });

    // Kütüphanenin varsayılan bileşen class'ları açık ve koyu temada, iki palet sürümüyle de okunabilir olmalı.
    [Theory]
    [InlineData(false, TwPaletteVersion.V3)]
    [InlineData(true, TwPaletteVersion.V3)]
    [InlineData(false, TwPaletteVersion.V4)]
    [InlineData(true, TwPaletteVersion.V4)]
    public void Default_component_classes_pass_the_audit(bool dark, TwPaletteVersion palette) => Sta.Run(() =>
    {
        using var provider = new TailwindProvider { Theme = new TwTheme(palette) };
        using var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-10000, -10000), ShowInTaskbar = false };
        var card = new TwPanel { Name = "card", Size = new Size(400, 300) };
        var title = new Label { Name = "title", Text = "Müşteri", AutoSize = true };
        var input = new TwTextBox { Name = "input", Size = new Size(200, 36) };
        var primary = new TwButton { Name = "primary", Text = "Kaydet", Size = new Size(100, 36) };
        var ghost = new TwButton { Name = "ghost", Text = "İptal", Size = new Size(100, 36) };
        var grid = new DataGridView { Name = "grid", ColumnCount = 2, RowCount = 2 };
        var strip = new MenuStrip { Name = "menu", Items = { "Dosya" } };
        card.Controls.AddRange(new Control[] { title, input, primary, ghost, grid });
        form.Controls.Add(card);
        form.Controls.Add(strip);
        form.Show();

        provider.SetClasses(form, "bg-gray-50 text-gray-900 dark:bg-gray-900 dark:text-gray-100");
        provider.SetClasses(card, "card");
        provider.SetClasses(title, "text-gray-900 font-semibold dark:text-gray-100");
        provider.SetClasses(input, "input");
        provider.SetClasses(primary, "btn-primary");
        provider.SetClasses(ghost, "btn-ghost");
        provider.SetClasses(grid, "table");
        provider.SetClasses(strip, "bg-white text-gray-700 dark:bg-gray-800 dark:text-gray-100");
        provider.DarkMode = dark;
        TwAnimator.Step(DateTime.UtcNow.AddSeconds(10)); // btn'deki transition geçişleri tamamlanır

        var issues = TwContrast.Audit(form, provider);
        form.Close();
        Assert.True(issues.Count == 0, string.Join(Environment.NewLine, issues.Select(i => i.ToString())));
    });
}
