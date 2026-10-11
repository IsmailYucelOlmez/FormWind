using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;

namespace FormWind.Design;

/// <summary>
/// <see cref="TailwindProvider"/> için Designer desteği: formdaki her kontrolün akıllı etiketine (standart kontroller dahil)
/// bir FormWind bölümü ekler. Bölümde class'lar düzenlenir ve kontrol tipine uygun bileşenler (buton → <c>btn-primary</c>,
/// metin kutusu → <c>input</c>, panel → <c>card</c>, grid → <c>table</c>) tek tıkla verilir. Değişiklikler extender
/// özelliği üzerinden yapıldığından Designer.cs'e yazılır ve geri alınabilir.
/// </summary>
public class TailwindProviderDesigner : ComponentDesigner
{
    private readonly Dictionary<IComponent, DesignerActionListCollection> _lists = new Dictionary<IComponent, DesignerActionListCollection>();
    private DesignerActionService? _actions;
    private IComponentChangeService? _changes;

    private TailwindProvider Provider => (TailwindProvider)Component;

    public override void Initialize(IComponent component)
    {
        base.Initialize(component);
        _actions = GetService(typeof(DesignerActionService)) as DesignerActionService;
        _changes = GetService(typeof(IComponentChangeService)) as IComponentChangeService;
        if (_actions is null) return;
        if (GetService(typeof(IDesignerHost)) is IDesignerHost host)
            foreach (IComponent existing in host.Container.Components) Attach(existing);
        if (_changes != null)
        {
            _changes.ComponentAdded += OnComponentAdded;
            _changes.ComponentRemoved += OnComponentRemoved;
        }
    }

    private void OnComponentAdded(object? sender, ComponentEventArgs e)
    {
        if (e.Component != null) Attach(e.Component);
    }

    private void OnComponentRemoved(object? sender, ComponentEventArgs e)
    {
        if (e.Component is null || !_lists.TryGetValue(e.Component, out var lists)) return;
        foreach (DesignerActionList list in lists) _actions?.Remove(e.Component, list);
        _lists.Remove(e.Component);
    }

    private void Attach(IComponent component)
    {
        if (_actions is null || component is not Control control || _lists.ContainsKey(component) || !Provider.CanExtend(control)) return;
        var lists = new DesignerActionListCollection { new ClassesActionList(Provider, control) };
        _lists[component] = lists;
        _actions.Add(component, lists);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_changes != null)
            {
                _changes.ComponentAdded -= OnComponentAdded;
                _changes.ComponentRemoved -= OnComponentRemoved;
            }
            foreach (var pair in _lists)
                foreach (DesignerActionList list in pair.Value) _actions?.Remove(pair.Key, list);
            _lists.Clear();
        }
        base.Dispose(disposing);
    }

    /// <summary>Bir kontrolün akıllı etiketindeki FormWind bölümü.</summary>
    internal sealed class ClassesActionList : DesignerActionList
    {
        private readonly TailwindProvider _provider;
        private readonly Control _control;

        public ClassesActionList(TailwindProvider provider, Control control) : base(control)
        {
            _provider = provider;
            _control = control;
        }

        [Editor(typeof(ClassesEditor), typeof(UITypeEditor))]
        public string Classes
        {
            get => _provider.GetClasses(_control);
            set => ClassesProperty().SetValue(_control, value ?? "");
        }

        /// <summary>Kontrol tipine uygun bileşenler (tema dosyasındakiler dahil, temada varsa).</summary>
        internal IEnumerable<string> SuggestedComponents()
        {
            string[] candidates = _control switch
            {
                ButtonBase => new[] { "btn-primary", "btn-ghost", "btn-danger" },
                TextBoxBase or TwTextBox or TwSelect or TwNumberBox or TwDatePicker => new[] { "input" },
                DataGridView => new[] { "table" },
                Panel or GroupBox or UserControl => new[] { "card" },
                _ => Array.Empty<string>(),
            };
            return candidates.Where(c => TwTheme.Current.Components.ContainsKey(c));
        }

        internal void Apply(string component) => Classes = component;

        public void Clear() => Classes = "";

        // Extender özelliği: başka bir provider varsa onunkiyle karışmasın diye bu provider'ınki seçilir.
        private PropertyDescriptor ClassesProperty() =>
            TypeDescriptor.GetProperties(_control).Cast<PropertyDescriptor>().First(p =>
                p.Name == "Classes"
                && p.Attributes[typeof(ExtenderProvidedPropertyAttribute)] is ExtenderProvidedPropertyAttribute { Provider: var owner }
                && ReferenceEquals(owner, _provider));

        public override DesignerActionItemCollection GetSortedActionItems()
        {
            var items = new DesignerActionItemCollection
            {
                new DesignerActionHeaderItem("FormWind"),
                new DesignerActionPropertyItem(nameof(Classes), "Classes", "FormWind", "Tailwind class'ları (örn. bg-blue-600 text-white px-4 py-2)."),
            };
            foreach (var component in SuggestedComponents())
                items.Add(new ComponentItem(this, component));
            if (Classes.Length > 0)
                items.Add(new DesignerActionMethodItem(this, nameof(Clear), "Class'ları temizle", "FormWind", true));
            return items;
        }

        // Bileşen başına bir komut; DesignerActionMethodItem yalnızca parametresiz metot çağırabildiği için adı taşır.
        private sealed class ComponentItem : DesignerActionMethodItem
        {
            private readonly ClassesActionList _owner;
            private readonly string _component;

            public ComponentItem(ClassesActionList owner, string component)
                : base(owner, nameof(Clear), component, "FormWind", $"Class'ları '{component}' yapar.", true)
            {
                _owner = owner;
                _component = component;
            }

            public override void Invoke() => _owner.Apply(_component);
        }
    }
}
