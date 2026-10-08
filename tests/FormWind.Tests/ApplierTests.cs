using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Xunit;

namespace FormWind.Tests;

public class ApplierTests
{
    private readonly TwTheme _theme = new TwTheme();

    private void Apply(Control control, TwOriginal original, string classes, TwState state = TwState.None, float scale = 1f)
    {
        var parsed = _theme.Parse(classes);
        var context = new TwApplyContext(parsed, state, scale, _theme, original, designMode: false);
        new WinFormsApplier().Apply(control, parsed.Resolve(state), context);
    }

    private static bool IsExplicit(Control control, string property) => TwOriginal.IsExplicit(control, property);

    [Fact]
    public void Explicit_value_checks_do_not_depend_on_TypeDescriptor()
    {
        Assert.True(TwOriginal.UsesDirectChecks);
    }

    [Fact]
    public void Explicit_value_checks_distinguish_inherited_from_assigned()
    {
        using var label = new Label();
        Assert.False(TwOriginal.IsExplicit(label, nameof(Control.ForeColor)));

        label.ForeColor = Color.Red;
        Assert.True(TwOriginal.IsExplicit(label, nameof(Control.ForeColor)));
    }

    [Fact]
    public void Styles_button_and_makes_it_flat()
    {
        using var button = new Button();
        Apply(button, TwOriginal.Capture(button, false), "bg-blue-600 text-white rounded-md");

        Assert.Equal(ColorTranslator.FromHtml("#2563eb").ToArgb(), button.BackColor.ToArgb());
        Assert.Equal(Color.White.ToArgb(), button.ForeColor.ToArgb());
        Assert.Equal(FlatStyle.Flat, button.FlatStyle);
        Assert.NotNull(button.Region);
    }

    [Fact]
    public void Removing_classes_restores_inherited_values()
    {
        using var button = new Button();
        var original = TwOriginal.Capture(button, false);

        Apply(button, original, "bg-blue-600 text-white rounded-md");
        Apply(button, original, "");

        Assert.False(IsExplicit(button, nameof(Control.BackColor)));
        Assert.False(IsExplicit(button, nameof(Control.ForeColor)));
        Assert.Equal(FlatStyle.Standard, button.FlatStyle);
        Assert.Null(button.Region);
    }

    [Fact]
    public void Leaving_a_state_restores_explicit_original_value()
    {
        using var label = new Label { BackColor = Color.Yellow };
        var original = TwOriginal.Capture(label, false);

        Apply(label, original, "hover:bg-red-500", TwState.Hover);
        Assert.Equal(ColorTranslator.FromHtml("#ef4444").ToArgb(), label.BackColor.ToArgb());

        Apply(label, original, "hover:bg-red-500", TwState.None);
        Assert.Equal(Color.Yellow.ToArgb(), label.BackColor.ToArgb());
    }

    [Fact]
    public void Untouched_properties_are_left_alone()
    {
        using var label = new Label();
        var original = TwOriginal.Capture(label, false);
        Apply(label, original, "p-2");

        label.BackColor = Color.Pink; // uygulama kodunun ataması
        Apply(label, original, "p-2", TwState.Hover);

        Assert.Equal(Color.Pink.ToArgb(), label.BackColor.ToArgb());
    }

    [Fact]
    public void Applies_font_size_in_points()
    {
        using var label = new Label();
        Apply(label, TwOriginal.Capture(label, false), "text-sm font-bold");

        Assert.True(label.Font.Bold);
        Assert.Equal(10.5f, label.Font.SizeInPoints, 2);
    }

    [Fact]
    public void Scales_spacing_with_dpi()
    {
        using var panel = new Panel();
        Apply(panel, TwOriginal.Capture(panel, false), "px-4 py-2", scale: 1.5f);

        Assert.Equal(new Padding(24, 12, 24, 12), panel.Padding);
    }

    [Fact]
    public void Provider_defers_styling_until_EndInit()
    {
        using var provider = new TailwindProvider();
        using var label = new Label();

        provider.BeginInit();
        provider.SetClasses(label, "bg-red-500");
        Assert.False(IsExplicit(label, nameof(Control.BackColor)));

        provider.EndInit();
        Assert.Equal(ColorTranslator.FromHtml("#ef4444").ToArgb(), label.BackColor.ToArgb());
        Assert.Equal("bg-red-500", provider.GetClasses(label));
    }

    [Fact]
    public void Provider_applies_dark_variants()
    {
        using var provider = new TailwindProvider();
        using var label = new Label();
        provider.SetClasses(label, "bg-white dark:bg-gray-900");

        provider.DarkMode = true;
        Assert.Equal(ColorTranslator.FromHtml("#111827").ToArgb(), label.BackColor.ToArgb());

        provider.DarkMode = false;
        Assert.Equal(Color.White.ToArgb(), label.BackColor.ToArgb());
    }

    [Fact]
    public void Provider_reports_unknown_classes()
    {
        using var provider = new TailwindProvider();
        using var label = new Label();
        TwUnknownClassesEventArgs? reported = null;
        provider.UnknownClasses += (_, e) => reported = e;

        provider.SetClasses(label, "bg-white not-a-class");

        Assert.NotNull(reported);
        Assert.Equal(new[] { "not-a-class" }, reported!.Unknown);
    }

    [Fact]
    public void Provider_can_disable_dpi_scaling()
    {
        using var provider = new TailwindProvider { ScaleWithDpi = false };
        using var panel = new Panel();
        provider.SetClasses(panel, "px-4");

        Assert.Equal(16, panel.Padding.Left);
    }

    [Fact]
    public void Owner_draw_controls_receive_chrome_instead_of_region()
    {
        using var panel = new TwPanel();
        Apply(panel, TwOriginal.Capture(panel, false), "rounded-lg border", scale: 2f);

        Assert.Null(panel.Region);
        Assert.Equal(16f, panel.CornerRadius);
        Assert.Equal(2f, panel.BorderWidth);
    }
}
