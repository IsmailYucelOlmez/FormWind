using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;

namespace FormWind.Design;

/// <summary>
/// Designer support for <see cref="TailwindProvider"/>: adds a FormWind section to the smart tag of every control on the form
/// (standard controls included). The section edits the classes and applies components suited to the control type (button →
/// <c>btn-primary</c>, text box → <c>input</c>, panel → <c>card</c>, grid → <c>table</c>) with one click. Changes go through the
/// extender property, so they are written to Designer.cs and can be undone.
/// <para>
/// The preview (<see cref="TailwindProvider.DesignTimePreview"/>) applies the classes' colors, fonts and spacing to the control;
/// so that these values are not written to Designer.cs, a type descriptor is added per instance to every control: properties
/// managed by the classes (BackColor, ForeColor, Font, Padding, Margin, Cursor) are not serialized.
/// </para>
/// </summary>
public class TailwindProviderDesigner : ComponentDesigner
{
    private readonly Dictionary<IComponent, DesignerActionListCollection> _lists = new Dictionary<IComponent, DesignerActionListCollection>();
    private readonly Dictionary<Control, PreviewTypeProvider> _previews = new Dictionary<Control, PreviewTypeProvider>();
    private DesignerActionService? _actions;
    private IComponentChangeService? _changes;

    private TailwindProvider Provider => (TailwindProvider)Component;

    public override void Initialize(IComponent component)
    {
        base.Initialize(component);
        _actions = GetService(typeof(DesignerActionService)) as DesignerActionService;
        _changes = GetService(typeof(IComponentChangeService)) as IComponentChangeService;
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
        if (e.Component is Control control && _previews.TryGetValue(control, out var preview))
        {
            TypeDescriptor.RemoveProvider(preview, control);
            _previews.Remove(control);
        }
        if (e.Component is null || !_lists.TryGetValue(e.Component, out var lists)) return;
        foreach (DesignerActionList list in lists) _actions?.Remove(e.Component, list);
        _lists.Remove(e.Component);
    }

    private void Attach(IComponent component)
    {
        if (component is not Control control || !Provider.CanExtend(control)) return;
        if (!_previews.ContainsKey(control))
        {
            var preview = new PreviewTypeProvider(TypeDescriptor.GetProvider(control), Provider);
            TypeDescriptor.AddProvider(preview, control);
            _previews[control] = preview;
        }
        if (_actions is null || _lists.ContainsKey(component)) return;
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
            foreach (var pair in _previews) TypeDescriptor.RemoveProvider(pair.Value, pair.Key);
            _previews.Clear();
        }
        base.Dispose(disposing);
    }

    /// <summary>Type descriptor that does not serialize the class-managed properties applied by the preview.</summary>
    private sealed class PreviewTypeProvider : TypeDescriptionProvider
    {
        private readonly TailwindProvider _provider;

        public PreviewTypeProvider(TypeDescriptionProvider parent, TailwindProvider provider) : base(parent) => _provider = provider;

        public override ICustomTypeDescriptor? GetTypeDescriptor(Type objectType, object? instance)
        {
            var descriptor = base.GetTypeDescriptor(objectType, instance);
            return instance is Control control && descriptor != null ? new Descriptor(descriptor, _provider, control) : descriptor;
        }

        private sealed class Descriptor : CustomTypeDescriptor
        {
            private readonly TailwindProvider _provider;
            private readonly Control _control;

            public Descriptor(ICustomTypeDescriptor parent, TailwindProvider provider, Control control) : base(parent)
            {
                _provider = provider;
                _control = control;
            }

            public override PropertyDescriptorCollection GetProperties() => Wrap(base.GetProperties());

            public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes) => Wrap(base.GetProperties(attributes));

            private PropertyDescriptorCollection Wrap(PropertyDescriptorCollection properties)
            {
                var wrapped = new PropertyDescriptor[properties.Count];
                for (int i = 0; i < properties.Count; i++)
                    wrapped[i] = IsStyleProperty(properties[i].Name) ? new ManagedProperty(properties[i], _provider, _control) : properties[i];
                return new PropertyDescriptorCollection(wrapped, true);
            }
        }

        private static bool IsStyleProperty(string name) =>
            name is nameof(Control.BackColor) or nameof(Control.ForeColor) or nameof(Control.Font)
                or nameof(Control.Padding) or nameof(Control.Margin) or nameof(Control.Cursor);
    }

    /// <summary>If the classes manage this property (and the preview is on), its value is not written to Designer.cs.</summary>
    private sealed class ManagedProperty : PropertyDescriptor
    {
        private readonly PropertyDescriptor _inner;
        private readonly TailwindProvider _provider;
        private readonly Control _control;

        public ManagedProperty(PropertyDescriptor inner, TailwindProvider provider, Control control) : base(inner)
        {
            _inner = inner;
            _provider = provider;
            _control = control;
        }

        internal static bool IsManaged(TailwindProvider provider, Control control, string property)
        {
            if (!provider.DesignTimePreview || provider.GetParsed(control) is not TwParsed parsed) return false;
            var style = parsed.Resolve(provider.BaseState);
            return property switch
            {
                nameof(Control.BackColor) => style.BackColor.HasValue,
                nameof(Control.ForeColor) => style.ForeColor.HasValue,
                nameof(Control.Font) => style.HasFont,
                nameof(Control.Padding) => !style.Padding.IsEmpty,
                nameof(Control.Margin) => !style.Margin.IsEmpty,
                nameof(Control.Cursor) => style.Cursor != null,
                _ => false,
            };
        }

        public override bool ShouldSerializeValue(object component) =>
            !IsManaged(_provider, _control, Name) && _inner.ShouldSerializeValue(component);

        public override Type ComponentType => _inner.ComponentType;
        public override bool IsReadOnly => _inner.IsReadOnly;
        public override Type PropertyType => _inner.PropertyType;
        public override bool CanResetValue(object component) => _inner.CanResetValue(component);
        public override object? GetValue(object? component) => _inner.GetValue(component);
        public override void ResetValue(object component) => _inner.ResetValue(component);
        public override void SetValue(object? component, object? value) => _inner.SetValue(component, value);
        public override TypeConverter Converter => _inner.Converter;
    }

    /// <summary>The FormWind section in a control's smart tag.</summary>
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

        /// <summary>Components suited to the control type (including those in the theme file, when the theme has them).</summary>
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
                new DesignerActionPropertyItem(nameof(Classes), "Classes", "FormWind", "Tailwind-style classes (e.g. bg-blue-600 text-white px-4 py-2)."),
            };
            foreach (var component in SuggestedComponents())
                items.Add(new ComponentItem(this, component));
            if (Classes.Length > 0)
                items.Add(new DesignerActionMethodItem(this, nameof(Clear), "Clear classes", "FormWind", true));
            return items;
        }

        // Bileşen başına bir komut; DesignerActionMethodItem yalnızca parametresiz metot çağırabildiği için adı taşır.
        private sealed class ComponentItem : DesignerActionMethodItem
        {
            private readonly ClassesActionList _owner;
            private readonly string _component;

            public ComponentItem(ClassesActionList owner, string component)
                : base(owner, nameof(Clear), component, "FormWind", $"Sets the classes to '{component}'.", true)
            {
                _owner = owner;
                _component = component;
            }

            public override void Invoke() => _owner.Apply(_component);
        }
    }
}
