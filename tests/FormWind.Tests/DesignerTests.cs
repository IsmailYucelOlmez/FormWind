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
        Assert.Equal(new[] { "Add page", "Remove selected page" }, designer.Verbs.Cast<DesignerVerb>().Select(v => v.Text));

        designer.Verbs[0]!.Invoke();
        designer.Verbs[0]!.Invoke();
        Assert.Equal(2, tabs.TabPages.Count);
        Assert.All(tabs.TabPages, p => Assert.NotNull(p.Site));            // Designer.cs'e yazılacak bileşenler
        Assert.Equal(new[] { "Page 1", "Page 2" }, tabs.Strip.Tabs);
        Assert.Same(tabs.TabPages[1], tabs.SelectedTab);

        Assert.True(designer.CanParent(new TwTabPage()));
        Assert.False(designer.CanParent(new Button()));                     // diğer kontroller sayfaya bırakılır

        // Bir sayfayı seçmek o sayfayı gösterir.
        var selection = (ISelectionService)surface.GetService(typeof(ISelectionService))!;
        selection.SetSelectedComponents(new[] { tabs.TabPages[0] });
        Assert.Equal(0, tabs.SelectedIndex);

        designer.Verbs[1]!.Invoke();
        Assert.Single(tabs.TabPages);
        Assert.Equal("Page 2", tabs.TabPages[0].Text);
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

    // Standart kontrollerin akıllı etiketine provider'ın tasarımcısı bir FormWind bölümü ekler.
    [Fact]
    public void Smart_tag_of_standard_controls_offers_components() => Sta.Run(() =>
    {
        using var surface = new DesignSurface(typeof(Form));
        var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var form = (Form)host.RootComponent;
        // Visual Studio akıllı etiket servisini kendisi sağlar; düz DesignSurface'te oluşturulur (kendini kaydeder).
        using var actions = new DesignerActionService(surface);
        var button = (Button)host.CreateComponent(typeof(Button), "btn");
        form.Controls.Add(button);
        var provider = (TailwindProvider)host.CreateComponent(typeof(TailwindProvider), "tw");
        Assert.IsType<TailwindProviderDesigner>(host.GetDesigner(provider));
        var textBox = (TextBox)host.CreateComponent(typeof(TextBox), "txt");     // provider'dan sonra eklenen
        form.Controls.Add(textBox);

        DesignerActionItem[] Items(System.ComponentModel.IComponent component) => actions.GetComponentActions(component)
            .Cast<DesignerActionList>().OfType<TailwindProviderDesigner.ClassesActionList>()
            .SelectMany(l => l.GetSortedActionItems().Cast<DesignerActionItem>()).ToArray();

        var buttonItems = Items(button);
        Assert.Equal(new[] { "btn-primary", "btn-ghost", "btn-danger" },
            buttonItems.OfType<DesignerActionMethodItem>().Select(i => i.DisplayName));
        Assert.Contains("input", Items(textBox).Select(i => i.DisplayName));

        int changes = 0;
        ((IComponentChangeService)surface.GetService(typeof(IComponentChangeService))!).ComponentChanged += (_, _) => changes++;
        buttonItems.OfType<DesignerActionMethodItem>().First(i => i.DisplayName == "btn-ghost").Invoke();
        Assert.Equal("btn-ghost", provider.GetClasses(button));
        Assert.True(changes > 0);                                                 // Designer.cs'e yazılır, geri alınabilir

        var clear = Items(button).OfType<DesignerActionMethodItem>().Single(i => i.DisplayName == "Clear classes");
        clear.Invoke();
        Assert.Equal("", provider.GetClasses(button));

        host.DestroyComponent(textBox);
        Assert.Empty(actions.GetComponentActions(textBox).Cast<DesignerActionList>().OfType<TailwindProviderDesigner.ClassesActionList>());
    });

    // Önizleme class renklerini kontrole uygular; bu değerler Designer.cs'e yazılmamalı (çalışma anında class'lar zaten verir).
    [Fact]
    public void Preview_values_managed_by_classes_are_not_serialized() => Sta.Run(() =>
    {
        using var surface = new DesignSurface(typeof(Form));
        var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var form = (Form)host.RootComponent;
        var provider = (TailwindProvider)host.CreateComponent(typeof(TailwindProvider), "tw");
        var button = (Button)host.CreateComponent(typeof(Button), "btn");
        form.Controls.Add(button);
        button.Font = new System.Drawing.Font("Segoe UI", 12f);                    // class'ların yönetmediği, elle atanmış

        var properties = System.ComponentModel.TypeDescriptor.GetProperties(button);
        properties["Classes"]!.SetValue(button, "bg-blue-600 text-white p-2");
        Assert.Equal(System.Drawing.ColorTranslator.FromHtml("#2563eb").ToArgb(), button.BackColor.ToArgb());   // önizleme
        Assert.False(properties["BackColor"]!.ShouldSerializeValue(button));
        Assert.False(properties["ForeColor"]!.ShouldSerializeValue(button));
        Assert.False(properties["Padding"]!.ShouldSerializeValue(button));
        Assert.True(properties["Font"]!.ShouldSerializeValue(button));

        provider.DesignTimePreview = false;                                         // önizleme kapalıyken dokunulmaz
        button.BackColor = System.Drawing.Color.Red;
        Assert.True(System.ComponentModel.TypeDescriptor.GetProperties(button)["BackColor"]!.ShouldSerializeValue(button));
    });
}
