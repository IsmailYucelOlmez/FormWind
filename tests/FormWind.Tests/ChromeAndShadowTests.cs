using System.Drawing;
using System.Windows.Forms;
using FormWind.Tests.Performance;
using Xunit;

namespace FormWind.Tests;

[Collection(UiCollection.Name)]
public class ChromeAndShadowTests
{
    private readonly TwTheme _theme = new TwTheme();

    private TwStyle Base(string classes) => _theme.Parse(classes).Resolve(TwState.None);

    private static int Argb(string hex) => ColorTranslator.FromHtml(hex).ToArgb();

    [Fact]
    public void Opacity_modifier_applies_to_every_color_utility()
    {
        var style = Base("bg-black/50 text-white/25 border-blue-500/100 ring-[#ff0000]/10");

        Assert.Equal(128, style.BackColor!.Value.A);
        Assert.Equal(64, style.ForeColor!.Value.A);
        Assert.Equal(255, style.BorderColor!.Value.A);
        Assert.Equal(26, style.RingColor!.Value.A);
        Assert.Contains("bg-white/101", _theme.Parse("bg-white/101").Unknown);
    }

    [Fact]
    public void Parses_side_borders()
    {
        var style = Base("border-b border-x-2 border-gray-200");

        Assert.Null(style.BorderWidth);
        Assert.Equal(1, style.BorderSides.Bottom);
        Assert.Equal(2, style.BorderSides.Left);
        Assert.Equal(2, style.BorderSides.Right);
        Assert.Null(style.BorderSides.Top);
        Assert.True(style.HasChrome);
    }

    [Fact]
    public void Chrome_combines_all_and_side_widths_with_dpi()
    {
        var chrome = TwChrome.FromStyle(Base("border border-b-4 rounded-md"), 1.5f, Color.Gray);

        Assert.Equal(1.5f, chrome.BorderTop);
        Assert.Equal(6f, chrome.BorderBottom);
        Assert.Equal(9f, chrome.Radius);
        Assert.False(chrome.IsUniformBorder);
    }

    [Theory]
    [InlineData("shadow-sm", TwShadowSize.Sm)]
    [InlineData("shadow", TwShadowSize.Base)]
    [InlineData("shadow-md", TwShadowSize.Md)]
    [InlineData("shadow-2xl", TwShadowSize.Xl2)]
    [InlineData("shadow-none", TwShadowSize.None)]
    public void Parses_shadow_sizes(string classes, TwShadowSize expected) =>
        Assert.Equal(expected, Base(classes).Shadow);

    [Fact]
    public void Shadow_presets_follow_tailwind_and_accept_a_color()
    {
        var md = TwShadowLayer.Preset(TwShadowSize.Md, null);
        Assert.Equal(2, md.Length);
        Assert.Equal(new TwShadowLayer(4, 6, -1, Color.FromArgb(26, 0, 0, 0)), md[0]);

        var colored = TwShadowLayer.Preset(TwShadowSize.Lg, Color.FromArgb(128, 59, 130, 246));
        Assert.All(colored, l => Assert.Equal(Color.FromArgb(128, 59, 130, 246).ToArgb(), l.Color.ToArgb()));
    }

    [Fact]
    public void Ring_defaults_to_three_pixels_and_tailwind_blue()
    {
        var spec = TwOutsetSpec.FromStyle(Base("ring"), 1f);

        Assert.Equal(3f, spec.RingWidth);
        Assert.Equal(TwOutsetSpec.DefaultRingColor.ToArgb(), spec.RingColor.ToArgb());
        Assert.Equal(2f, TwOutsetSpec.FromStyle(Base("ring-2 ring-offset-2"), 1f).RingWidth);
        Assert.True(TwOutsetSpec.FromStyle(Base("ring-2 ring-offset-2"), 1f).Extent >= 4);
    }

    [Fact]
    public void Focus_ring_is_applied_only_in_focus_state()
    {
        using var textBox = new TwTextBox();
        var parsed = _theme.Parse("focus:ring-2 focus:ring-blue-500");
        var original = TwOriginal.Capture(textBox, false);
        var applier = new WinFormsApplier();

        applier.Apply(textBox, parsed.Resolve(TwState.Focus), new TwApplyContext(parsed, TwState.Focus, 1f, _theme, original, false));
        Assert.Equal(2f, TwOutset.For(textBox).Spec!.RingWidth);

        applier.Apply(textBox, parsed.Resolve(TwState.None), new TwApplyContext(parsed, TwState.None, 1f, _theme, original, false));
        Assert.Null(TwOutset.For(textBox).Spec);
    }

    [Fact]
    public void Parent_paints_a_blurred_shadow_below_the_child() => Sta.Run(() =>
    {
        using var provider = new TailwindProvider();
        using var parent = new Panel { Size = new Size(200, 200), BackColor = Color.White };
        var card = new TwPanel { Bounds = new Rectangle(50, 40, 100, 80) };
        parent.Controls.Add(card);
        provider.SetClasses(card, "bg-white rounded-lg shadow-xl");

        using var bitmap = new Bitmap(200, 200);
        parent.DrawToBitmap(bitmap, new Rectangle(0, 0, 200, 200));

        float Brightness(int x, int y) => bitmap.GetPixel(x, y).GetBrightness();
        // Kartın hemen altı gölgeli, uzağı beyaz; gölge alta doğru yumuşakça açılır (bulanık).
        Assert.True(Brightness(100, 125) < 0.97f, $"kart altı {Brightness(100, 125)}");
        Assert.True(Brightness(100, 125) < Brightness(100, 150));
        Assert.Equal(1f, Brightness(100, 195), 2);
        Assert.Equal(1f, Brightness(5, 5), 2);
    });

    [Fact]
    public void Side_border_is_drawn_only_on_that_side() => Sta.Run(() =>
    {
        using var provider = new TailwindProvider();
        using var panel = new TwPanel { Size = new Size(100, 40), BackColor = Color.White };
        provider.SetClasses(panel, "bg-white border-b-2 border-black");

        using var bitmap = new Bitmap(100, 40);
        panel.DrawToBitmap(bitmap, new Rectangle(0, 0, 100, 40));

        Assert.Equal(Color.Black.ToArgb(), bitmap.GetPixel(50, 39).ToArgb());
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(50, 0).ToArgb());
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(0, 20).ToArgb());
    });
}
