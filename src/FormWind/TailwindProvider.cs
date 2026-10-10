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
[ProvideProperty("ColumnClasses", typeof(DataGridViewColumn))]
[ToolboxItemFilter("System.Windows.Forms")]
[Description("Kontrollere Tailwind benzeri 'Classes' property'si ekler.")]
public class TailwindProvider : Component, IExtenderProvider, ISupportInitialize
{
    private readonly Dictionary<Control, TwBinding> _bindings = new Dictionary<Control, TwBinding>();
    private readonly Dictionary<DataGridViewColumn, TwColumnBinding> _columns = new Dictionary<DataGridViewColumn, TwColumnBinding>();
    private readonly Dictionary<DataGridView, TwRowRule> _rowRules = new Dictionary<DataGridView, TwRowRule>();
    private readonly Timer _hoverTimer;
    private TwTheme? _theme;
    private bool _darkMode;
    private bool _designTimePreview = true;
    private bool _scaleWithDpi = true;
    private bool _darkScrollbars = true;
    private bool _darkToolTips = true;
    private bool _respectHighContrast = true;
    private bool _initializing;

    public TailwindProvider()
    {
        _hoverTimer = new Timer { Interval = 100 };
        _hoverTimer.Tick += OnHoverTick;
        TwTheme.CurrentChanged += OnCurrentThemeChanged;
        TwHighContrast.Changed += OnHighContrastChanged;
        TwFocusVisible.EnsureInstalled();
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
            UpdateNativeToolTips();
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

    /// <summary>
    /// Koyu temada native kaydırma çubuklarını (panel, TextBox, ListBox, TreeView, ListView, DataGridView) da
    /// koyulaştırır. Windows 10 1809+ gerekir ve belgelenmemiş bir tema adı kullanır; sorun olursa kapatılabilir.
    /// </summary>
    /// <summary>
    /// Windows Yüksek Karşıtlık modu açıkken class renkleri yerine sistem renklerini kullanır (zemin ve yazı özgün değerlerine
    /// döner, seçili ve hover parçaları vurgu rengini alır, gölge ve gradient çizilmez); <c>forced-colors:</c> varyantları
    /// yine uygulanır. Boşluk, yazı boyutu, köşe ve yerleşim değişmez. Mod çalışırken değişirse stiller yeniden uygulanır.
    /// </summary>
    [Category("FormWind"), DefaultValue(true), Description("Yüksek Karşıtlık modunda sistem renklerini kullanır.")]
    public bool RespectHighContrast
    {
        get => _respectHighContrast;
        set
        {
            if (_respectHighContrast == value) return;
            _respectHighContrast = value;
            RefreshAll();
        }
    }

    /// <summary>Sistem renkleri kullanılıyor mu (Yüksek Karşıtlık açık ve <see cref="RespectHighContrast"/>).</summary>
    [Browsable(false)]
    public bool HighContrastActive => _respectHighContrast && !DesignMode && TwHighContrast.IsActive;

    /// <summary>Kontrolden bağımsız durum: koyu tema ve Yüksek Karşıtlık.</summary>
    internal TwState BaseState =>
        (_darkMode ? TwState.Dark : TwState.None) | (HighContrastActive ? TwState.HighContrast : TwState.None);

    private void OnHighContrastChanged(object? sender, EventArgs e)
    {
        if (_respectHighContrast) RefreshAll();
    }

    [Category("FormWind"), DefaultValue(true), Description("Koyu temada native kaydırma çubuklarını da koyulaştırır.")]
    public bool DarkScrollbars
    {
        get => _darkScrollbars;
        set
        {
            if (_darkScrollbars == value) return;
            _darkScrollbars = value;
            RefreshAll();
        }
    }

    /// <summary>
    /// Koyu temada native ipucu pencerelerini (ToolTip bileşenleri, ToolStrip öğe ipuçları) da koyulaştırır. İpuçları
    /// uygulamanın bileşenlerine ait olduğundan etki iş parçacığı genelindedir. Windows 10 1809+ gerekir ve belgelenmemiş
    /// bir tema adı kullanır; sorun olursa kapatılabilir.
    /// </summary>
    [Category("FormWind"), DefaultValue(true), Description("Koyu temada native ipuçlarını da koyulaştırır.")]
    public bool DarkToolTips
    {
        get => _darkToolTips;
        set
        {
            if (_darkToolTips == value) return;
            _darkToolTips = value;
            UpdateNativeToolTips();
        }
    }

    private void UpdateNativeToolTips()
    {
        if (!DesignMode) TwNativeToolTips.SetDark(this, _darkMode && _darkToolTips);
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

    public bool CanExtend(object extendee) => extendee is Control || extendee is DataGridViewColumn;

    [Category("FormWind")]
    [DefaultValue("")]
    [Description("Kolon class'ları: hücreler (text-right font-semibold px-3), başlık (header:…), w-32, min-w-20, flex-1, hidden.")]
    [Editor(typeof(ClassesEditor), typeof(UITypeEditor))]
    [TypeConverter(typeof(ClassesConverter))]
    public string GetColumnClasses(DataGridViewColumn column) =>
        column != null && _columns.TryGetValue(column, out var binding) ? binding.Classes : "";

    public void SetColumnClasses(DataGridViewColumn column, string? value)
    {
        if (column is null) throw new ArgumentNullException(nameof(column));
        var classes = TwParser.Normalize(value);
        if (!_columns.TryGetValue(column, out var binding))
        {
            if (classes.Length == 0) return;
            binding = new TwColumnBinding(this, column);
            _columns.Add(column, binding);
        }
        binding.SetClasses(classes, Theme);
        if (!_initializing) binding.Refresh();
    }

    /// <summary>
    /// Satır bazlı koşullu stil. Kural her satır için class'lar döndürür (null = değişiklik yok); koyu tema
    /// varyantları da çalışır. <c>null</c> kural mevcut kuralı kaldırır.
    /// </summary>
    public void SetRowClasses(DataGridView grid, Func<DataGridViewRow, string?>? rule)
    {
        if (grid is null) throw new ArgumentNullException(nameof(grid));
        if (_rowRules.TryGetValue(grid, out var existing))
        {
            existing.Detach();
            _rowRules.Remove(grid);
        }
        if (rule != null) _rowRules[grid] = new TwRowRule(this, grid, rule);
    }

    internal void RemoveColumn(DataGridViewColumn column) => _columns.Remove(column);

    [Category("FormWind")]
    [DefaultValue("")]
    [Description("Tailwind benzeri utility class'lar. Örn: bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700")]
    [Editor(typeof(ClassesEditor), typeof(UITypeEditor))]
    [TypeConverter(typeof(ClassesConverter))]
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

    internal TwParsed? GetParsed(Control control) =>
        _bindings.TryGetValue(control, out var binding) ? binding.Parsed : null;

    /// <summary>Kontrolün stilini yeniden hesaplar (örn. uygulama kodu bir property'yi değiştirdikten sonra).</summary>
    public void Refresh(Control control)
    {
        if (control != null && _bindings.TryGetValue(control, out var binding))
            binding.Refresh(force: true);
    }

    private readonly Dictionary<Control, string> _errors = new Dictionary<Control, string>();
    private readonly HashSet<Control> _clearOnEdit = new HashSet<Control>();

    /// <summary>
    /// Kontrolün doğrulama hatasını verir (<c>null</c>/boş temizler). Hata varken kontrole <c>invalid:</c> varyantı uygulanır;
    /// kontrol bir <see cref="TwField"/> içindeyse hata metni alanın altında gösterilir. Kullanıcı değeri değiştirince hata
    /// kendiliğinden temizlenir.
    /// </summary>
    public void SetError(Control control, string? message)
    {
        if (control is null) throw new ArgumentNullException(nameof(control));
        var editor = control is TwField field ? field.Editor ?? control : control;
        if (string.IsNullOrWhiteSpace(message)) _errors.Remove(editor);
        else _errors[editor] = message!;

        if (editor.Parent is TwField owner) owner.ErrorText = message;
        if (_clearOnEdit.Add(editor))
        {
            void Clear(object? sender, EventArgs e)
            {
                if (_errors.ContainsKey(editor)) SetError(editor, null);
            }
            editor.TextChanged += Clear;
            if (editor is TwSelect select) select.SelectedIndexChanged += Clear;
            if (editor is TwDatePicker picker) picker.ValueChanged += Clear;
            if (editor is CheckBox check) check.CheckedChanged += Clear;
            editor.Disposed += (_, _) =>
            {
                _errors.Remove(editor);
                _clearOnEdit.Remove(editor);
            };
        }

        Refresh(editor);
        if (editor.Parent is TwField parentField) Refresh(parentField);
        ErrorsChanged?.Invoke(this, EventArgs.Empty);
    }

    public string? GetError(Control control)
    {
        var editor = control is TwField field ? field.Editor ?? control : control;
        return _errors.TryGetValue(editor, out var message) ? message : null;
    }

    [Browsable(false)]
    public bool HasErrors => _errors.Count > 0;

    /// <summary>Hata eklendiğinde ya da temizlendiğinde.</summary>
    public event EventHandler? ErrorsChanged;

    public void ClearErrors()
    {
        foreach (var control in _errors.Keys.ToList()) SetError(control, null);
    }

    /// <summary>
    /// <paramref name="container"/> içindeki tüm <see cref="TwField"/>'ların kurallarını denetler, hataları gösterir ve ilk
    /// hatalı alana odaklanır. Hepsi geçerliyse <c>true</c>.
    /// </summary>
    public bool Validate(Control container)
    {
        TwField? first = null;
        foreach (var field in Descendants(container).OfType<TwField>())
        {
            var message = field.Validate();
            SetError(field, message);
            if (message != null && first is null) first = field;
        }
        (first?.Editor ?? first)?.Focus();
        return first is null;
    }

    private void RefreshBreakpoints(Form form)
    {
        var affected = _bindings.Values.Where(b => b.UsesBreakpoints && b.Control.FindForm() == form).ToList();
        if (affected.Count == 0) return;
        form.SuspendLayout();
        try
        {
            foreach (var binding in affected) binding.Refresh(force: true);
        }
        finally
        {
            form.ResumeLayout(true);
        }
        BreakpointChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Bir formun genişlik eşiği değiştiğinde (sm:/md:… varyantları yeniden uygulandıktan sonra).</summary>
    public event EventHandler? BreakpointChanged;

    internal bool HasError(Control control) =>
        _errors.ContainsKey(control) || (control is TwField field && field.Editor is Control editor && _errors.ContainsKey(editor));

    private static System.Collections.Generic.IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    private bool _dpiRefreshPending;
    private readonly HashSet<Form> _hookedForms = new HashSet<Form>();

    internal void HookForm(Control control)
    {
        var form = control as Form ?? control.FindForm();
        if (form is null || !_hookedForms.Add(form)) return;
        form.DpiChanged += (_, _) => ScheduleDpiRefresh(form);
        // Genişlik eşiği değişince sm:/md:… varyantlı kontroller yeniden uygulanır.
        var last = TwBreakpoints.For(form);
        form.Resize += (_, _) =>
        {
            var current = TwBreakpoints.For(form);
            if (current == last) return;
            last = current;
            RefreshBreakpoints(form);
        };
        form.Disposed += (_, _) => _hookedForms.Remove(form);
    }

    internal void ScheduleDpiRefresh(Control control)
    {
        if (_dpiRefreshPending) return;
        var target = control.FindForm() ?? control;
        if (!target.IsHandleCreated) return;
        _dpiRefreshPending = true;
        target.BeginInvoke((Action)(() =>
        {
            _dpiRefreshPending = false;
            RefreshAll();
        }));
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
            foreach (var column in _columns.Values.ToList())
                column.Refresh();
            foreach (var grid in _rowRules.Keys)
                grid.Invalidate();
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
            TwHighContrast.Changed -= OnHighContrastChanged;
            TwNativeToolTips.SetDark(this, false);
            _hoverTimer.Dispose();
            foreach (var binding in _bindings.Values.ToList())
                binding.Detach();
            _bindings.Clear();
            foreach (var rule in _rowRules.Values) rule.Detach();
            _rowRules.Clear();
            _columns.Clear();
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
        foreach (var column in _columns.Values.ToList())
            column.Reparse(theme);
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
