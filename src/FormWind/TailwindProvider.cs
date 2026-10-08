using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;
using FormWind.Design;

namespace FormWind;

/// <summary>
/// Forma eklendiğinde her kontrole <c>Classes</c> property'si ekler (ToolTip ile aynı mekanizma).
/// Class'lar parse edilir, kontrolün durumuna (hover, focus, active, disabled, dark) göre birleştirilir
/// ve uygun <see cref="IStyleApplier"/> ile uygulanır.
/// </summary>
[ProvideProperty("Classes", typeof(Control))]
[ToolboxItemFilter("System.Windows.Forms")]
[Description("Kontrollere Tailwind benzeri 'Classes' property'si ekler.")]
public class TailwindProvider : Component, IExtenderProvider, ISupportInitialize
{
    private readonly Dictionary<Control, TwBinding> _bindings = new Dictionary<Control, TwBinding>();
    private readonly Timer _hoverTimer;
    private TwTheme? _theme;
    private bool _darkMode;
    private bool _designTimePreview = true;
    private bool _scaleWithDpi = true;
    private bool _initializing;

    public TailwindProvider()
    {
        _hoverTimer = new Timer { Interval = 100 };
        _hoverTimer.Tick += OnHoverTick;
        TwTheme.CurrentChanged += OnCurrentThemeChanged;
    }

    public TailwindProvider(IContainer container)
        : this()
    {
        if (container is null) throw new ArgumentNullException(nameof(container));
        container.Add(this);
    }

    /// <summary>Bir kontrolün class'larında tanınmayan değer bulunduğunda tetiklenir. Dinleyici yoksa Debug çıktısına yazılır.</summary>
    public event EventHandler<TwUnknownClassesEventArgs>? UnknownClasses;

    [Category("FormWind"), DefaultValue(false), Description("dark: varyantlarını etkinleştirir.")]
    public bool DarkMode
    {
        get => _darkMode;
        set
        {
            if (_darkMode == value) return;
            _darkMode = value;
            RefreshAll();
        }
    }

    [Category("FormWind"), DefaultValue(true), Description("Stilleri Designer'da da uygular.")]
    public bool DesignTimePreview
    {
        get => _designTimePreview;
        set
        {
            if (_designTimePreview == value) return;
            _designTimePreview = value;
            RefreshAll();
        }
    }

    /// <summary>
    /// Boşluk, boyut, köşe ve kenarlık değerlerini kontrolün DPI'ına göre ölçekler (96 DPI = 1).
    /// Form yerleşimi DPI ile ölçeklenmeyen (AutoScale çalışmayan) uygulamalarda kapatılmalıdır;
    /// aksi halde class değerleri büyürken kontrol boyutları sabit kalır.
    /// </summary>
    [Category("FormWind"), DefaultValue(true), Description("Class piksel değerlerini kontrolün DPI'ına göre ölçekler.")]
    public bool ScaleWithDpi
    {
        get => _scaleWithDpi;
        set
        {
            if (_scaleWithDpi == value) return;
            _scaleWithDpi = value;
            RefreshAll();
        }
    }

    /// <summary>Bu provider'ın kullandığı tema. Atanmamışsa <see cref="TwTheme.Current"/>.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TwTheme Theme
    {
        get => _theme ?? TwTheme.Current;
        set
        {
            _theme = value;
            ReparseAll();
        }
    }

    internal bool IsDesignMode => DesignMode;

    public bool CanExtend(object extendee) => extendee is Control;

    [Category("FormWind")]
    [DefaultValue("")]
    [Description("Tailwind benzeri utility class'lar. Örn: bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700")]
    [Editor(typeof(ClassesEditor), typeof(UITypeEditor))]
    public string GetClasses(Control control) =>
        control != null && _bindings.TryGetValue(control, out var binding) ? binding.Classes : "";

    public void SetClasses(Control control, string? value)
    {
        if (control is null) throw new ArgumentNullException(nameof(control));

        var classes = TwParser.Normalize(value);
        if (!_bindings.TryGetValue(control, out var binding))
        {
            if (classes.Length == 0) return;
            binding = new TwBinding(this, control);
            _bindings.Add(control, binding);
        }

        binding.SetClasses(classes, Theme);
        if (!_initializing) binding.Refresh(force: true);
    }

    /// <summary>Kontrolün stilini yeniden hesaplar (örn. uygulama kodu bir property'yi değiştirdikten sonra).</summary>
    public void Refresh(Control control)
    {
        if (control != null && _bindings.TryGetValue(control, out var binding))
            binding.Refresh(force: true);
    }

    public void RefreshAll()
    {
        if (_initializing) return;

        // Font ve padding değişiklikleri her seferinde ebeveynin yerleşimini tetikler (kontrol sayısıyla karesel);
        // toplu güncellemede yerleşim bir kez, sonda yapılır.
        var bindings = _bindings.Values.ToList();
        var parents = bindings
            .Select(b => b.Control.Parent)
            .Where(p => p != null)
            .Distinct()
            .ToList();

        foreach (var parent in parents) parent!.SuspendLayout();
        try
        {
            foreach (var binding in bindings)
                binding.Refresh(force: true);
        }
        finally
        {
            foreach (var parent in parents) parent!.ResumeLayout(true);
        }
    }

    public void BeginInit() => _initializing = true;

    public void EndInit()
    {
        _initializing = false;
        RefreshAll();
    }

    internal bool ShouldApply => !DesignMode || _designTimePreview;

    internal void StartHoverTracking()
    {
        if (!_hoverTimer.Enabled) _hoverTimer.Start();
    }

    internal void Remove(Control control) => _bindings.Remove(control);

    internal bool IsInHoveredGroup(Control control)
    {
        for (var parent = control.Parent; parent != null; parent = parent.Parent)
        {
            if (_bindings.TryGetValue(parent, out var binding) && binding.IsGroup && binding.Hovered) return true;
        }
        return false;
    }

    internal void RefreshGroupDescendants(Control group)
    {
        foreach (var binding in _bindings.Values.ToList())
        {
            if (binding.Control != group && group.Contains(binding.Control)) binding.Refresh();
        }
    }

    internal void ReportUnknown(Control control, TwParsed parsed)
    {
        var handler = UnknownClasses;
        if (handler != null)
        {
            handler(this, new TwUnknownClassesEventArgs(control, parsed.Unknown));
            return;
        }
        Debug.WriteLine($"FormWind: '{control.Name}' için bilinmeyen class: {string.Join(", ", parsed.Unknown)}");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            TwTheme.CurrentChanged -= OnCurrentThemeChanged;
            _hoverTimer.Dispose();
            foreach (var binding in _bindings.Values.ToList())
                binding.Detach();
            _bindings.Clear();
        }
        base.Dispose(disposing);
    }

    // MouseLeave, imleç alt kontrol üzerinden dışarı çıktığında tetiklenmez; hover durumu burada kapatılır.
    private void OnHoverTick(object? sender, EventArgs e)
    {
        bool anyHovered = false;
        foreach (var binding in _bindings.Values.ToList())
        {
            if (!binding.Hovered) continue;
            if (binding.IsCursorInside()) anyHovered = true;
            else binding.SetHovered(false);
        }
        if (!anyHovered) _hoverTimer.Stop();
    }

    private void OnCurrentThemeChanged(object? sender, EventArgs e)
    {
        if (_theme is null) ReparseAll();
    }

    private void ReparseAll()
    {
        var theme = Theme;
        foreach (var binding in _bindings.Values.ToList())
            binding.Reparse(theme);
        RefreshAll();
    }
}

public sealed class TwUnknownClassesEventArgs : EventArgs
{
    public TwUnknownClassesEventArgs(Control control, IReadOnlyList<string> unknown)
    {
        Control = control;
        Unknown = unknown;
    }

    public Control Control { get; }

    public IReadOnlyList<string> Unknown { get; }
}
