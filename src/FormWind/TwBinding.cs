using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FormWind;

/// <summary>Bir kontrolün class'larını, durumunu ve olay aboneliklerini tutar.</summary>
internal sealed class TwBinding
{
    private readonly TailwindProvider _owner;
    private readonly Control _control;
    private TwParsed _parsed = TwParsed.Empty;
    private TwOriginal? _original;
    private bool _hovered;
    private bool _pressed;
    private readonly HashSet<Control> _hookedDescendants = new HashSet<Control>();

    private TwParsed? _lastParsed;
    private TwState _lastState;
    private float _lastScale;

    public TwBinding(TailwindProvider owner, Control control)
    {
        _owner = owner;
        _control = control;

        _control.MouseEnter += OnMouseEnter;
        _control.MouseLeave += OnMouseLeave;
        _control.MouseDown += OnMouseDown;
        _control.MouseUp += OnMouseUp;
        _control.Enter += OnStateChanged;
        _control.Leave += OnStateChanged;
        _control.EnabledChanged += OnStateChanged;
        _control.DpiChangedAfterParent += OnStateChanged;
        _control.Disposed += OnDisposed;
        if (_control is CheckBox checkBox) checkBox.CheckedChanged += OnStateChanged;
        if (_control is RadioButton radioButton) radioButton.CheckedChanged += OnStateChanged;
    }

    public Control Control => _control;

    public string Classes { get; private set; } = "";

    public bool Hovered => _hovered;

    /// <summary><c>group</c> class'ı var: alt kontrollerin <c>group-hover:</c> varyantları bu kontrolün hover'ını izler.</summary>
    public bool IsGroup { get; private set; }

    public void SetClasses(string classes, TwTheme theme)
    {
        Classes = classes;
        Reparse(theme);
    }

    public void Reparse(TwTheme theme)
    {
        _parsed = theme.Parse(Classes);
        if (_parsed.Unknown.Count > 0) _owner.ReportUnknown(_control, _parsed);

        IsGroup = _parsed.Resolve(TwState.None).IsGroup == true;
        // İmleç bir konteynere doğrudan çocuğunun üzerinden girerse konteynerin MouseEnter'ı tetiklenmez;
        // hover'a tepki veren konteynerler bu yüzden çocuklarının MouseEnter'ını da dinler.
        if (IsGroup || _parsed.HasCondition(TwState.Hover | TwState.Active)) HookDescendants(_control);
        else UnhookDescendants();
    }

    public void Refresh(bool force = false)
    {
        if (_control.IsDisposed || !_owner.ShouldApply) return;

        _original ??= TwOriginal.Capture(_control, _owner.IsDesignMode);

        var state = CurrentState();
        var scale = _owner.ScaleWithDpi ? _control.DeviceDpi / 96f : 1f;
        if (!force && ReferenceEquals(_parsed, _lastParsed) && state == _lastState && scale == _lastScale) return;

        _lastParsed = _parsed;
        _lastState = state;
        _lastScale = scale;

        var style = _parsed.Resolve(state);
        var context = new TwApplyContext(_parsed, state, scale, _owner.Theme, _original, _owner.IsDesignMode);
        TwAppliers.Find(_control).Apply(_control, style, context);
    }

    public bool IsCursorInside() =>
        _control.IsHandleCreated
        && _control.Visible
        && _control.ClientRectangle.Contains(_control.PointToClient(Control.MousePosition));

    public void SetHovered(bool hovered)
    {
        if (_hovered == hovered) return;
        _hovered = hovered;
        if (!hovered) _pressed = false;
        Refresh();
        if (IsGroup) _owner.RefreshGroupDescendants(_control);
    }

    public void Detach()
    {
        _control.MouseEnter -= OnMouseEnter;
        _control.MouseLeave -= OnMouseLeave;
        _control.MouseDown -= OnMouseDown;
        _control.MouseUp -= OnMouseUp;
        _control.Enter -= OnStateChanged;
        _control.Leave -= OnStateChanged;
        _control.EnabledChanged -= OnStateChanged;
        _control.DpiChangedAfterParent -= OnStateChanged;
        _control.Disposed -= OnDisposed;
        if (_control is CheckBox checkBox) checkBox.CheckedChanged -= OnStateChanged;
        if (_control is RadioButton radioButton) radioButton.CheckedChanged -= OnStateChanged;
        UnhookDescendants();
    }

    private void HookDescendants(Control parent)
    {
        if (parent != _control && !_hookedDescendants.Add(parent)) return;
        if (parent != _control) parent.MouseEnter += OnDescendantMouseEnter;
        parent.ControlAdded -= OnDescendantAdded;
        parent.ControlAdded += OnDescendantAdded;
        foreach (Control child in parent.Controls) HookDescendants(child);
    }

    private void UnhookDescendants()
    {
        foreach (var descendant in _hookedDescendants)
        {
            descendant.MouseEnter -= OnDescendantMouseEnter;
            descendant.ControlAdded -= OnDescendantAdded;
        }
        _hookedDescendants.Clear();
        _control.ControlAdded -= OnDescendantAdded;
    }

    private void OnDescendantAdded(object? sender, ControlEventArgs e)
    {
        if (e.Control != null) HookDescendants(e.Control);
    }

    private void OnDescendantMouseEnter(object? sender, EventArgs e) => OnMouseEnter(sender, e);

    private TwState CurrentState()
    {
        var state = _owner.DarkMode ? TwState.Dark : TwState.None;
        if (_control is CheckBox { Checked: true } || _control is RadioButton { Checked: true })
            state |= TwState.Checked;

        // Devre dışı kontrolde etkileşim durumları uygulanmaz.
        if (!_control.Enabled) return state | TwState.Disabled;

        if (_control.ContainsFocus) state |= TwState.Focus;
        if (_owner.IsInHoveredGroup(_control)) state |= TwState.GroupHover;
        if (_hovered) state |= TwState.Hover;
        if (_pressed) state |= TwState.Active;
        return state;
    }

    private void OnMouseEnter(object? sender, EventArgs e)
    {
        SetHovered(true);
        _owner.StartHoverTracking();
    }

    private void OnMouseLeave(object? sender, EventArgs e)
    {
        if (!IsCursorInside()) SetHovered(false);
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _pressed = true;
        Refresh();
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (!_pressed) return;
        _pressed = false;
        Refresh();
    }

    private void OnStateChanged(object? sender, EventArgs e) => Refresh();

    private void OnDisposed(object? sender, EventArgs e)
    {
        Detach();
        _owner.Remove(_control);
    }
}
