using System;
using System.Drawing;
using System.Windows.Forms;

namespace FormWind;

// Owner-draw kontrollerin erişilebilirlik ağacı (MSAA / UI Automation köprüsü): ekran okuyucular ve UI test araçları
// sekmeleri, liste satırlarını ve takvim günlerini ayrı öğeler olarak görür. Kontrolün AccessibleName/Role/Description
// değerleri verilmişse onlar önceliklidir.

/// <summary>Bir owner-draw kontrolün tek bir öğesi (sekme, satır, gün).</summary>
internal abstract class TwItemAccessibleObject : AccessibleObject
{
    protected TwItemAccessibleObject(Control owner, AccessibleObject parent, int index)
    {
        Owner = owner;
        ParentObject = parent;
        Index = index;
    }

    protected Control Owner { get; }

    protected AccessibleObject ParentObject { get; }

    public int Index { get; }

    public override AccessibleObject Parent => ParentObject;

    /// <summary>Kontrol içindeki (istemci) konum.</summary>
    protected abstract Rectangle ClientBounds { get; }

    public override Rectangle Bounds
    {
        get
        {
            var bounds = Rectangle.Intersect(ClientBounds, Owner.ClientRectangle);
            return Owner.IsHandleCreated && !bounds.IsEmpty ? Owner.RectangleToScreen(bounds) : Rectangle.Empty;
        }
    }

    protected AccessibleStates BaseState
    {
        get
        {
            var state = AccessibleStates.Selectable | AccessibleStates.Focusable;
            if (!Owner.Visible || Rectangle.Intersect(ClientBounds, Owner.ClientRectangle).IsEmpty)
                state |= AccessibleStates.Offscreen | AccessibleStates.Invisible;
            if (!Owner.Enabled) state |= AccessibleStates.Unavailable;
            return state;
        }
    }
}

/// <summary>Kontrolün kendisi; çocukları dizinle üretilir (sanal liste için hepsi önceden oluşturulmaz).</summary>
internal abstract class TwListAccessibleObject : Control.ControlAccessibleObject
{
    protected TwListAccessibleObject(Control owner, AccessibleRole role) : base(owner)
    {
        Host = owner;
        DefaultRole = role;
    }

    // Owner .NET 8'de nullable; kurucuda verilen kontrol.
    protected Control Host { get; }

    private AccessibleRole DefaultRole { get; }

    public override AccessibleRole Role => Host.AccessibleRole != AccessibleRole.Default ? Host.AccessibleRole : DefaultRole;

    protected abstract int Count { get; }

    protected abstract AccessibleObject CreateChild(int index);

    /// <summary>Odaktaki/seçili öğenin dizini; yoksa -1.</summary>
    protected abstract int ActiveIndex { get; }

    /// <summary>İstemci koordinatındaki öğe; yoksa -1.</summary>
    protected abstract int IndexAt(Point client);

    public override int GetChildCount() => Count;

    public override AccessibleObject? GetChild(int index) => index >= 0 && index < Count ? CreateChild(index) : null;

    public override AccessibleObject? GetFocused() =>
        Host.Focused && ActiveIndex >= 0 ? CreateChild(ActiveIndex) : Host.Focused ? this : null;

    public override AccessibleObject? GetSelected() => ActiveIndex >= 0 ? CreateChild(ActiveIndex) : null;

    public override AccessibleObject? HitTest(int x, int y)
    {
        if (!Host.IsHandleCreated) return null;
        var client = Host.PointToClient(new Point(x, y));
        if (!Host.ClientRectangle.Contains(client)) return null;
        int index = IndexAt(client);
        return index >= 0 ? CreateChild(index) : this;
    }
}

internal sealed class TwDataListAccessibleObject : TwListAccessibleObject
{
    private readonly TwDataList _list;

    public TwDataListAccessibleObject(TwDataList list) : base(list, AccessibleRole.List) => _list = list;

    protected override int Count => _list.ItemCount;

    protected override int ActiveIndex => _list.SelectedIndex;

    protected override int IndexAt(Point client) => _list.IndexFromPoint(client);

    protected override AccessibleObject CreateChild(int index) => new Item(_list, this, index);

    private sealed class Item : TwItemAccessibleObject
    {
        private readonly TwDataList _list;

        public Item(TwDataList list, AccessibleObject parent, int index) : base(list, parent, index) => _list = list;

        protected override Rectangle ClientBounds => _list.GetItemBounds(Index);

        public override string? Name
        {
            get => _list.GetAccessibleItemName(Index);
            set { }
        }

        public override AccessibleRole Role => AccessibleRole.ListItem;

        public override AccessibleStates State
        {
            get
            {
                var state = BaseState;
                if (_list.SelectionMode == TwListSelectionMode.MultiExtended) state |= AccessibleStates.MultiSelectable | AccessibleStates.ExtSelectable;
                if (_list.IsSelected(Index)) state |= AccessibleStates.Selected;
                if (_list.SelectedIndex == Index && _list.Focused) state |= AccessibleStates.Focused;
                return state;
            }
        }

        public override string DefaultAction => "Aç";

        public override void DoDefaultAction()
        {
            Select(AccessibleSelection.TakeSelection);
            _list.Activate();
        }

        public override void Select(AccessibleSelection flags)
        {
            if ((flags & AccessibleSelection.TakeSelection) != 0) _list.SelectedIndex = Index;
            else if ((flags & AccessibleSelection.AddSelection) != 0) _list.SetSelected(Index, true);
            else if ((flags & AccessibleSelection.RemoveSelection) != 0) _list.SetSelected(Index, false);
            if ((flags & (AccessibleSelection.TakeSelection | AccessibleSelection.AddSelection)) != 0) _list.EnsureVisible(Index, animate: false);
            if ((flags & AccessibleSelection.TakeFocus) != 0) _list.Focus();
        }
    }
}

internal sealed class TwTabStripAccessibleObject : TwListAccessibleObject
{
    private readonly TwTabStrip _tabs;

    public TwTabStripAccessibleObject(TwTabStrip tabs) : base(tabs, AccessibleRole.PageTabList) => _tabs = tabs;

    protected override int Count => _tabs.Tabs.Count;

    protected override int ActiveIndex => _tabs.SelectedIndex;

    protected override int IndexAt(Point client)
    {
        for (int i = 0; i < _tabs.Tabs.Count; i++)
            if (_tabs.GetTabBounds(i).Contains(client)) return i;
        return -1;
    }

    protected override AccessibleObject CreateChild(int index) => new Tab(_tabs, this, index);

    private sealed class Tab : TwItemAccessibleObject
    {
        private readonly TwTabStrip _tabs;

        public Tab(TwTabStrip tabs, AccessibleObject parent, int index) : base(tabs, parent, index) => _tabs = tabs;

        protected override Rectangle ClientBounds => _tabs.GetTabBounds(Index);

        public override string? Name
        {
            get => Index < _tabs.Tabs.Count ? _tabs.Tabs[Index] : null;
            set { }
        }

        public override AccessibleRole Role => AccessibleRole.PageTab;

        public override AccessibleStates State
        {
            get
            {
                var state = BaseState;
                if (_tabs.SelectedIndex == Index)
                {
                    state |= AccessibleStates.Selected;
                    if (_tabs.Focused) state |= AccessibleStates.Focused;
                }
                return state;
            }
        }

        public override string DefaultAction => "Seç";

        public override void DoDefaultAction() => Select(AccessibleSelection.TakeSelection);

        public override void Select(AccessibleSelection flags)
        {
            if ((flags & (AccessibleSelection.TakeSelection | AccessibleSelection.AddSelection)) != 0) _tabs.SelectedIndex = Index;
            if ((flags & AccessibleSelection.TakeFocus) != 0) _tabs.Focus();
        }
    }
}

/// <summary>Takvim: 0 önceki ay, 1 sonraki ay düğmesi; 2–43 görünen günler.</summary>
internal sealed class TwCalendarAccessibleObject : TwListAccessibleObject
{
    public const int FirstDayChild = 2;

    private readonly TwCalendar _calendar;

    public TwCalendarAccessibleObject(TwCalendar calendar) : base(calendar, AccessibleRole.Table) => _calendar = calendar;

    public override string? Name
    {
        get => _calendar.AccessibleName ?? _calendar.Title;
        set => base.Name = value;
    }

    public override string? Value
    {
        get => _calendar.SelectedDate?.ToString("D", _calendar.Culture) ?? "";
        set { }
    }

    protected override int Count => FirstDayChild + 42;

    protected override int ActiveIndex => FirstDayChild + _calendar.FocusIndex;

    protected override int IndexAt(Point client)
    {
        if (_calendar.PreviousButtonBounds.Contains(client)) return 0;
        if (_calendar.NextButtonBounds.Contains(client)) return 1;
        for (int i = 0; i < 42; i++)
            if (_calendar.GetDayBounds(_calendar.FirstVisibleDate.AddDays(i)).Contains(client)) return FirstDayChild + i;
        return -1;
    }

    protected override AccessibleObject CreateChild(int index) =>
        index < FirstDayChild ? new NavButton(_calendar, this, index) : new Day(_calendar, this, index);

    private sealed class NavButton : TwItemAccessibleObject
    {
        private readonly TwCalendar _calendar;

        public NavButton(TwCalendar calendar, AccessibleObject parent, int index) : base(calendar, parent, index) => _calendar = calendar;

        protected override Rectangle ClientBounds => Index == 0 ? _calendar.PreviousButtonBounds : _calendar.NextButtonBounds;

        public override string? Name
        {
            get => Index == 0 ? "Önceki ay" : "Sonraki ay";
            set { }
        }

        public override AccessibleRole Role => AccessibleRole.PushButton;

        public override AccessibleStates State => BaseState & ~AccessibleStates.Selectable;

        public override string DefaultAction => "Bas";

        public override void DoDefaultAction()
        {
            if (Index == 0) _calendar.ShowPreviousMonth();
            else _calendar.ShowNextMonth();
        }
    }

    private sealed class Day : TwItemAccessibleObject
    {
        private readonly TwCalendar _calendar;

        public Day(TwCalendar calendar, AccessibleObject parent, int index) : base(calendar, parent, index) => _calendar = calendar;

        private DateTime Date => _calendar.FirstVisibleDate.AddDays(Index - FirstDayChild);

        protected override Rectangle ClientBounds => _calendar.GetDayBounds(Date);

        public override string? Name
        {
            get => Date.ToString("D", _calendar.Culture);
            set { }
        }

        public override AccessibleRole Role => AccessibleRole.Cell;

        public override AccessibleStates State
        {
            get
            {
                var state = BaseState;
                if (_calendar.SelectedDate == Date) state |= AccessibleStates.Selected;
                if (_calendar.Focused && Index - FirstDayChild == _calendar.FocusIndex) state |= AccessibleStates.Focused;
                if (!_calendar.IsInRange(Date)) state |= AccessibleStates.Unavailable;
                return state;
            }
        }

        public override string DefaultAction => "Seç";

        public override void DoDefaultAction() => Select(AccessibleSelection.TakeSelection);

        public override void Select(AccessibleSelection flags)
        {
            if ((flags & (AccessibleSelection.TakeSelection | AccessibleSelection.AddSelection)) != 0 && _calendar.IsInRange(Date))
                _calendar.SelectDate(Date);
            if ((flags & AccessibleSelection.TakeFocus) != 0) _calendar.Focus();
        }
    }
}

/// <summary>Açılır liste: ComboBox rolü, değer seçili öğe; varsayılan eylem menüyü açar.</summary>
internal sealed class TwSelectAccessibleObject : Control.ControlAccessibleObject
{
    private readonly TwSelect _select;

    public TwSelectAccessibleObject(TwSelect select) : base(select) => _select = select;

    public override AccessibleRole Role => _select.AccessibleRole != AccessibleRole.Default ? _select.AccessibleRole : AccessibleRole.ComboBox;

    public override string? Value
    {
        get => _select.SelectedItem ?? "";
        set
        {
            int index = value is null ? -1 : _select.Items.IndexOf(value);
            if (index >= 0) _select.SelectedIndex = index;
        }
    }

    public override AccessibleStates State
    {
        get
        {
            var state = base.State | AccessibleStates.HasPopup;
            if (_select.DropDownMenu?.Visible == true) state |= AccessibleStates.Expanded;
            else state |= AccessibleStates.Collapsed;
            return state;
        }
    }

    public override string DefaultAction => "Aç";

    public override void DoDefaultAction() => _select.ShowDropDown();
}

/// <summary>Varsayılan rolü değiştirilmiş kontrol (örn. resim için Graphic).</summary>
internal sealed class TwRoleAccessibleObject : Control.ControlAccessibleObject
{
    private readonly Control _owner;
    private readonly AccessibleRole _role;

    public TwRoleAccessibleObject(Control owner, AccessibleRole role) : base(owner)
    {
        _owner = owner;
        _role = role;
    }

    public override AccessibleRole Role => _owner.AccessibleRole != AccessibleRole.Default ? _owner.AccessibleRole : _role;
}

/// <summary>Ağaç: Outline rolü; düğümler OutlineItem, açık/kapalı durumu ve düzeyiyle.</summary>
internal sealed class TwTreeAccessibleObject : TwListAccessibleObject
{
    private readonly TwTreeView _tree;

    public TwTreeAccessibleObject(TwTreeView tree) : base(tree, AccessibleRole.Outline) => _tree = tree;

    protected override int Count => _tree.VisibleNodes.Count;

    protected override int ActiveIndex => _tree.SelectedIndex;

    protected override int IndexAt(Point client) => _tree.IndexFromPoint(client);

    protected override AccessibleObject CreateChild(int index) => new Node(_tree, this, index);

    private sealed class Node : TwItemAccessibleObject
    {
        private readonly TwTreeView _tree;

        public Node(TwTreeView tree, AccessibleObject parent, int index) : base(tree, parent, index) => _tree = tree;

        private TwTreeNode Item => _tree.VisibleNodes[Index];

        protected override Rectangle ClientBounds => _tree.GetItemBounds(Index);

        public override string? Name
        {
            get => Index < _tree.VisibleNodes.Count ? Item.Text : null;
            set { }
        }

        public override AccessibleRole Role => AccessibleRole.OutlineItem;

        // MSAA'da ağaç düğümünün değeri düzeyidir.
        public override string? Value
        {
            get => Index < _tree.VisibleNodes.Count ? Item.Level.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
            set { }
        }

        public override AccessibleStates State
        {
            get
            {
                var state = BaseState;
                if (Index >= _tree.VisibleNodes.Count) return state;
                if (Item.HasChildren) state |= Item.IsExpanded ? AccessibleStates.Expanded : AccessibleStates.Collapsed;
                if (_tree.SelectedIndex == Index)
                {
                    state |= AccessibleStates.Selected;
                    if (_tree.Focused) state |= AccessibleStates.Focused;
                }
                return state;
            }
        }

        public override string DefaultAction => Item.HasChildren ? (Item.IsExpanded ? "Daralt" : "Genişlet") : "Seç";

        public override void DoDefaultAction()
        {
            if (Item.HasChildren) Item.IsExpanded = !Item.IsExpanded;
            else _tree.SelectedIndex = Index;
        }

        public override void Select(AccessibleSelection flags)
        {
            if ((flags & (AccessibleSelection.TakeSelection | AccessibleSelection.AddSelection)) != 0) _tree.SelectedIndex = Index;
            if ((flags & AccessibleSelection.TakeFocus) != 0) _tree.Focus();
        }
    }
}
