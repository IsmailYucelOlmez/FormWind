using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Forms;
using FormWind.Design;
using FormWind.Tests.Performance;
using Xunit;

namespace FormWind.Tests;

[Collection(UiCollection.Name)]
public class DesignerTests
{
    [Fact]
    public void Tab_control_designer_adds_and_removes_sited_pages() => Sta.Run(() =>
    {
        using var surface = new DesignSurface(typeof(Form));
        var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var form = (Form)host.RootComponent;
        var tabs = (TwTabControl)host.CreateComponent(typeof(TwTabControl), "tabs");
        form.Controls.Add(tabs);

        var designer = Assert.IsType<TwTabControlDesigner>(host.GetDesigner(tabs));
        Assert.Equal(new[] { "Sayfa ekle", "Seçili sayfayı kaldır" }, designer.Verbs.Cast<DesignerVerb>().Select(v => v.Text));

        designer.Verbs[0]!.Invoke();
        designer.Verbs[0]!.Invoke();
        Assert.Equal(2, tabs.TabPages.Count);
        Assert.All(tabs.TabPages, p => Assert.NotNull(p.Site));            // Designer.cs'e yazılacak bileşenler
        Assert.Equal(new[] { "Sayfa 1", "Sayfa 2" }, tabs.Strip.Tabs);
        Assert.Same(tabs.TabPages[1], tabs.SelectedTab);

        Assert.True(designer.CanParent(new TwTabPage()));
        Assert.False(designer.CanParent(new Button()));                     // diğer kontroller sayfaya bırakılır

        // Bir sayfayı seçmek o sayfayı gösterir.
        var selection = (ISelectionService)surface.GetService(typeof(ISelectionService))!;
        selection.SetSelectedComponents(new[] { tabs.TabPages[0] });
        Assert.Equal(0, tabs.SelectedIndex);

        designer.Verbs[1]!.Invoke();
        Assert.Single(tabs.TabPages);
        Assert.Equal("Sayfa 2", tabs.TabPages[0].Text);
        Assert.Single(designer.ActionLists);
    });

    [Fact]
    public void Classes_property_suggests_components() => Sta.Run(() =>
    {
        using var surface = new DesignSurface(typeof(Form));
        var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var form = (Form)host.RootComponent;
        host.CreateComponent(typeof(TailwindProvider), "tw");
        var button = (Button)host.CreateComponent(typeof(Button), "btn");
        form.Controls.Add(button);

        var property = System.ComponentModel.TypeDescriptor.GetProperties(button)["Classes"]!;
        var converter = Assert.IsType<ClassesConverter>(property.Converter);
        Assert.True(converter.GetStandardValuesSupported());
        Assert.False(converter.GetStandardValuesExclusive());              // serbest metin de yazılır
        var values = converter.GetStandardValues()!.Cast<string>().ToArray();
        Assert.Contains("btn-primary", values);
        Assert.Contains("card", values);
    });
}
