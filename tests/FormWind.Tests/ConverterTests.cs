#if !NETFRAMEWORK
using System.Collections.Generic;
using System.Drawing;
using FormWind.Convert;
using Xunit;

namespace FormWind.Tests;

public class ConverterTests
{
    private const string Designer = """
        namespace Demo
        {
            partial class OrderForm
            {
                private System.ComponentModel.IContainer components = null;

                private void InitializeComponent()
                {
                    this.btnSave = new System.Windows.Forms.Button();
                    this.lblTitle = new System.Windows.Forms.Label();
                    this.SuspendLayout();
                    //
                    // btnSave
                    //
                    this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
                    this.btnSave.FlatAppearance.BorderSize = 0;
                    this.btnSave.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(78)))), ((int)(((byte)(216)))));
                    this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                    this.btnSave.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
                    this.btnSave.ForeColor = System.Drawing.Color.White;
                    this.btnSave.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
                    this.btnSave.Text = "Kaydet";
                    //
                    // lblTitle
                    //
                    this.lblTitle.BackColor = System.Drawing.SystemColors.Control;
                    this.lblTitle.Font = new System.Drawing.Font("Georgia", 14F);
                    this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(140)))), ((int)(((byte)(60)))));
                    this.lblTitle.Text = "Siparişler";
                    //
                    // OrderForm
                    //
                    this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(249)))), ((int)(((byte)(250)))), ((int)(((byte)(251)))));
                    this.ResumeLayout(false);
                }

                private System.Windows.Forms.Button btnSave;
                private System.Windows.Forms.Label lblTitle;
            }
        }
        """;

    [Fact]
    public void Converts_colors_fonts_padding_and_flat_appearance()
    {
        var result = new DesignerConverter().Convert(Designer);
        var save = result.Controls[0];
        Assert.Equal("btnSave", save.Control);
        Assert.Equal(new[] { "bg-blue-600", "border-0", "hover:bg-blue-700", "text-sm", "font-semibold", "text-white", "px-3", "py-1.5" }, save.Classes);
        Assert.Empty(save.Notes);

        Assert.Contains("this.tailwindProvider1.SetClasses(this.btnSave, \"bg-blue-600 border-0 hover:bg-blue-700 text-sm font-semibold text-white px-3 py-1.5\");", result.Source);
        Assert.DoesNotContain("this.btnSave.BackColor", result.Source);
        Assert.Contains("this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;", result.Source);   // davranış satırları kalır
        Assert.Contains("this.btnSave.Text = \"Kaydet\";", result.Source);
    }

    [Fact]
    public void Leaves_system_colors_and_other_font_families_with_notes()
    {
        var result = new DesignerConverter().Convert(Designer);
        var title = result.Controls[1];
        Assert.Equal(new[] { "text-[#5a8c3c]" }, title.Classes);                      // palette uzak: keyfi renk
        Assert.Contains(title.Notes, n => n.Contains("sistem rengi"));
        Assert.Contains(title.Notes, n => n.Contains("Georgia"));
        Assert.Contains("this.lblTitle.BackColor = System.Drawing.SystemColors.Control;", result.Source);
        Assert.Contains("this.lblTitle.Font = new System.Drawing.Font(\"Georgia\", 14F);", result.Source);

        var form = result.Controls[2];
        Assert.Equal("this", form.Control);
        Assert.Equal(new[] { "bg-gray-50" }, form.Classes);
        Assert.Contains("this.tailwindProvider1.SetClasses(this, \"bg-gray-50\");", result.Source);
    }

    [Fact]
    public void Adds_the_provider_once_and_merges_into_existing_calls()
    {
        var result = new DesignerConverter().Convert(Designer);
        Assert.True(result.ProviderAdded);
        Assert.Contains("this.components = new System.ComponentModel.Container();", result.Source);
        Assert.Contains("this.tailwindProvider1 = new FormWind.TailwindProvider(this.components);", result.Source);
        Assert.Contains("private FormWind.TailwindProvider tailwindProvider1;", result.Source);
        Assert.True(result.Source.IndexOf("new FormWind.TailwindProvider") < result.Source.IndexOf("SetClasses"));

        // İkinci çalıştırma: değişecek bir şey yok, sağlayıcı tekrar eklenmez.
        var again = new DesignerConverter().Convert(result.Source);
        Assert.False(again.ProviderAdded);
        Assert.False(again.Changed);

        var withExisting = result.Source.Replace("this.btnSave.Text = \"Kaydet\";",
            "this.btnSave.Text = \"Kaydet\";\n            this.btnSave.ForeColor = System.Drawing.Color.Black;");
        var merged = new DesignerConverter().Convert(withExisting);
        Assert.Contains("SetClasses(this.btnSave, \"bg-blue-600 border-0 hover:bg-blue-700 text-sm font-semibold text-white px-3 py-1.5 text-black\")", merged.Source);
    }

    [Theory]
    [InlineData(8, "2")]
    [InlineData(6, "1.5")]
    [InlineData(1, "px")]
    [InlineData(7, "[7px]")]
    [InlineData(0, "0")]
    public void Maps_pixels_to_spacing(int pixels, string expected) => Assert.Equal(expected, DesignerConverter.Spacing(pixels));

    [Fact]
    public void Nearby_colors_snap_to_the_palette_with_a_note()
    {
        var converter = new DesignerConverter();
        Assert.Equal("blue-600", converter.ColorClass(Color.FromArgb(37, 99, 235), out var exact));
        Assert.Null(exact);
        Assert.Equal("blue-600", converter.ColorClass(Color.FromArgb(40, 100, 230), out var near));
        Assert.NotNull(near);
        Assert.Equal("transparent", converter.ColorClass(Color.Transparent, out _));

        // Üretilen keyfi class'lar parser'da çalışır.
        var style = TwParser.Parse("text-[#5a8c3c] text-[13px] py-1.5", TwTheme.Current).Resolve(TwState.None);
        Assert.Equal(Color.FromArgb(90, 140, 60), style.ForeColor);
        Assert.Equal(13f, style.FontSizePx);
    }
}
#endif
