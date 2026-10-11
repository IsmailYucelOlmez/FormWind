using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FormWind;

/// <summary><see cref="TwTreeView"/> düğümü.</summary>
public class TwTreeNode
{
    private bool _expanded;

    public TwTreeNode() => Nodes = new TwTreeNodeCollection(this);

    public TwTreeNode(string text, string? iconName = null) : this()
    {
        Text = text;
        IconName = iconName;
    }

    public string Text { get; set; } = "";

    /// <summary>İkon adı (<see cref="TwIconRenderer.DrawIcon"/>); <c>null</c> = ikonsuz.</summary>
    public string? IconName { get; set; }

    public object? Tag { get; set; }

    public TwTreeNodeCollection Nodes { get; }

    public TwTreeNode? Parent { get; internal set; }

    internal TwTreeView? Tree { get; set; }

    /// <summary>
    /// Alt düğümleri henüz yüklenmemiş ama açılabilir (ok gösterilir). İlk açılışta <see cref="TwTreeView.BeforeExpand"/>'de
    /// düğümleri ekleyip bunu <c>false</c> yapın.
    /// </summary>
    public bool HasLazyChildren { get; set; }

    public bool HasChildren => Nodes.Count > 0 || HasLazyChildren;

    public int Level => Parent is null ? 0 : Parent.Level + 1;

    public bool IsExpanded
    {
        get => _expanded;
        set
        {
            if (value) Expand();
            else Collapse();
        }
    }

    internal void SetExpanded(bool value) => _expanded = value;

    // Ağaca eklenmemiş düğümde yalnızca durum kaydedilir; eklenince açık görünür.
    public void Expand()
    {
        if (Root?.Tree is TwTreeView tree) tree.ExpandNode(this);
        else if (HasChildren) _expanded = true;
    }

    public void Collapse()
    {
        if (Root?.Tree is TwTreeView tree) tree.CollapseNode(this);
        else _expanded = false;
    }

    /// <summary>Kendisi ve tüm alt düğümleri açar.</summary>
    public void ExpandAll()
    {
        Expand();
        foreach (var child in Nodes) child.ExpandAll();
    }

    /// <summary>Kökten bu düğüme yol ("Belgeler/2026/Ekim").</summary>
    public string FullPath => Parent is null ? Text : Parent.FullPath + "/" + Text;

    private TwTreeNode? Root
    {
        get
        {
            var node = this;
            while (node.Parent != null) node = node.Parent;
            return node;
        }
    }

    public override string ToString() => Text;
}

/// <summary>Düğüm listesi; değişiklikler ağaca hemen yansır.</summary>
public sealed class TwTreeNodeCollection : Collection<TwTreeNode>
{
    private readonly TwTreeNode? _owner;
    private readonly TwTreeView? _tree;

    internal TwTreeNodeCollection(TwTreeNode owner) => _owner = owner;

    internal TwTreeNodeCollection(TwTreeView tree) => _tree = tree;

    public TwTreeNode Add(string text, string? iconName = null)
    {
        var node = new TwTreeNode(text, iconName);
        Add(node);
        return node;
    }

    public void AddRange(IEnumerable<TwTreeNode> nodes)
    {
        foreach (var node in nodes.ToList()) Add(node);
    }

    private TwTreeView? Tree
    {
        get
        {
            if (_tree != null) return _tree;
            var node = _owner;
            while (node?.Parent != null) node = node.Parent;
            return node?.Tree;
        }
    }

    protected override void InsertItem(int index, TwTreeNode item)
    {
        item.Parent = _owner;
        if (_tree != null) item.Tree = _tree;
        base.InsertItem(index, item);
        Tree?.Rebuild();
    }

    protected override void RemoveItem(int index)
    {
        this[index].Parent = null;
        this[index].Tree = null;
        base.RemoveItem(index);
        Tree?.Rebuild();
    }

    protected override void SetItem(int index, TwTreeNode item)
    {
        item.Parent = _owner;
        if (_tree != null) item.Tree = _tree;
        base.SetItem(index, item);
        Tree?.Rebuild();
    }

    protected override void ClearItems()
    {
        foreach (var node in this)
        {
            node.Parent = null;
            node.Tree = null;
        }
        base.ClearItems();
        Tree?.Rebuild();
    }
}

public sealed class TwTreeViewCancelEventArgs : CancelEventArgs
{
    public TwTreeViewCancelEventArgs(TwTreeNode node) => Node = node;

    public TwTreeNode Node { get; }
}

/// <summary>
/// TreeView yerine sanal ağaç: yalnızca açık dalların görünen düğümleri çizilir (binlerce düğüm sorun değil).
/// Klavye: ↑/↓, → açar / ilk çocuğa iner, ← kapatır / üst düğüme çıkar, Enter ya da çift tık açar-kapatır.
/// Alt düğümler <see cref="TwTreeNode.HasLazyChildren"/> ve <see cref="BeforeExpand"/> ile istendiğinde yüklenebilir.
/// Stiller TwDataList ile aynıdır (<c>item:hover:bg-*</c>, <c>selected:bg-*</c> …).
/// </summary>
[ToolboxItem(true)]
[DefaultEvent(nameof(AfterSelect))]
[Description("Stylable virtual tree.")]
public class TwTreeView : TwDataList
{
    private readonly List<TwTreeNode> _visible = new List<TwTreeNode>();
    private int _indent = 18;
    private bool _rebuilding;

    public TwTreeView()
    {
        Nodes = new TwTreeNodeCollection(this);
        ItemHeight = 30;
        ItemPadding = new Padding(8, 0, 8, 0);
        ItemText = i => i < _visible.Count ? _visible[i].Text : "";
        SelectedIndexChanged += (_, _) => { if (!_rebuilding) AfterSelect?.Invoke(this, EventArgs.Empty); };
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TwTreeNodeCollection Nodes { get; }

    /// <summary>Açılmadan önce (iptal edilebilir); tembel yüklenen düğümlerin çocukları burada eklenir.</summary>
    public event EventHandler<TwTreeViewCancelEventArgs>? BeforeExpand;

    public event EventHandler<TwTreeViewCancelEventArgs>? AfterExpand;

    public event EventHandler<TwTreeViewCancelEventArgs>? AfterCollapse;

    public event EventHandler? AfterSelect;

    /// <summary>Düzey başına girinti (96 DPI).</summary>
    [Category("Appearance"), DefaultValue(18)]
    public int Indent
    {
        get => _indent;
        set
        {
            _indent = Math.Max(0, value);
            Invalidate();
        }
    }

    /// <summary>Görünen (açık dallardaki) düğümler, ekrandaki sırayla.</summary>
    [Browsable(false)]
    public IReadOnlyList<TwTreeNode> VisibleNodes => _visible;

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TwTreeNode? SelectedNode
    {
        get => SelectedIndex >= 0 && SelectedIndex < _visible.Count ? _visible[SelectedIndex] : null;
        set
        {
            if (value is null)
            {
                SelectedIndex = -1;
                return;
            }
            // Seçilen düğümün üst dalları açılır.
            for (var parent = value.Parent; parent != null; parent = parent.Parent)
                if (!parent.IsExpanded) ExpandNode(parent);
            SelectedIndex = _visible.IndexOf(value);
            EnsureVisible(SelectedIndex);
        }
    }

    public void ExpandAll()
    {
        foreach (var node in Nodes) node.ExpandAll();
    }

    public void CollapseAll()
    {
        void Walk(IEnumerable<TwTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                node.SetExpanded(false);
                Walk(node.Nodes);
            }
        }
        Walk(Nodes);
        Rebuild();
    }

    internal void ExpandNode(TwTreeNode node)
    {
        if (node.IsExpanded || !node.HasChildren) return;
        var args = new TwTreeViewCancelEventArgs(node);
        BeforeExpand?.Invoke(this, args);
        if (args.Cancel) return;
        node.SetExpanded(true);
        Rebuild();
        AfterExpand?.Invoke(this, args);
        int index = _visible.IndexOf(node);
        if (index >= 0) AccessibilityNotifyClients(AccessibleEvents.StateChange, index);
    }

    internal void CollapseNode(TwTreeNode node)
    {
        if (!node.IsExpanded) return;
        node.SetExpanded(false);
        // Kapanan dalın içindeki seçim dalın kendisine taşınır.
        var selected = SelectedNode;
        for (var parent = selected?.Parent; parent != null; parent = parent.Parent)
            if (parent == node) selected = node;
        Rebuild(selected);
        AfterCollapse?.Invoke(this, new TwTreeViewCancelEventArgs(node));
        int index = _visible.IndexOf(node);
        if (index >= 0) AccessibilityNotifyClients(AccessibleEvents.StateChange, index);
    }

    internal void Rebuild() => Rebuild(SelectedNode);

    private void Rebuild(TwTreeNode? keep)
    {
        _visible.Clear();
        void Walk(IEnumerable<TwTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                _visible.Add(node);
                if (node.IsExpanded) Walk(node.Nodes);
            }
        }
        Walk(Nodes);
        _rebuilding = true;
        try
        {
            ItemCount = _visible.Count;
            SelectedIndex = keep is null ? -1 : _visible.IndexOf(keep);
        }
        finally
        {
            _rebuilding = false;
        }
        Invalidate();
    }

    private int ScaledIndent => ScalePixels(_indent);

    private int ArrowSize => ScalePixels(16);

    /// <summary>Düğümün aç/kapa okunun yeri.</summary>
    public Rectangle GetExpanderBounds(int index)
    {
        if (index < 0 || index >= _visible.Count) return Rectangle.Empty;
        var row = GetItemBounds(index);
        int x = row.X + ScaledItemPadding.Left + _visible[index].Level * ScaledIndent;
        return new Rectangle(x, row.Y + (row.Height - ArrowSize) / 2, ArrowSize, ArrowSize);
    }

    protected override void OnDrawItem(TwDataListDrawEventArgs e)
    {
        var node = _visible[e.Index];
        var arrow = GetExpanderBounds(e.Index);
        var em = TwIconRenderer.EmPixels(Font.SizeInPoints * 0.85f, 96);
        if (node.HasChildren)
            TwIconRenderer.DrawIcon(e.Graphics, node.IsExpanded ? "chevron-down" : "chevron-right", em, arrow, e.MutedColor);

        int x = arrow.Right + ScalePixels(4);
        if (node.IconName != null)
        {
            var icon = new Rectangle(x, e.Bounds.Y, ScalePixels(18), e.Bounds.Height);
            if (TwIconRenderer.DrawIcon(e.Graphics, node.IconName, TwIconRenderer.EmPixels(Font.SizeInPoints * 1.1f, 96), icon, e.Selected ? e.ForeColor : e.MutedColor))
                x = icon.Right + ScalePixels(6);
        }
        e.DrawText(node.Text, new Rectangle(x, e.Bounds.Y, Math.Max(0, e.Bounds.Right - ScaledItemPadding.Right - x), e.Bounds.Height));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        int index = IndexFromPoint(e.Location);
        bool onArrow = index >= 0 && Rectangle.Inflate(GetExpanderBounds(index), ScalePixels(4), ScalePixels(6)).Contains(e.Location);
        base.OnMouseDown(e);
        if (onArrow && e.Button == MouseButtons.Left) Toggle(_visible[index]);
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        int index = IndexFromPoint(e.Location);
        if (index >= 0 && _visible[index].HasChildren && e.Button == MouseButtons.Left)
        {
            Toggle(_visible[index]);
            return;
        }
        base.OnMouseDoubleClick(e);
    }

    protected override bool IsInputKey(Keys keyData) =>
        (keyData & Keys.KeyCode) is Keys.Left or Keys.Right or Keys.Add or Keys.Subtract || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var node = SelectedNode;
        if (node != null)
        {
            switch (e.KeyCode)
            {
                case Keys.Right:
                    if (node.HasChildren && !node.IsExpanded) ExpandNode(node);
                    else if (node.IsExpanded && node.Nodes.Count > 0) SelectedNode = node.Nodes[0];
                    e.Handled = true;
                    return;
                case Keys.Left:
                    if (node.IsExpanded) CollapseNode(node);
                    else if (node.Parent != null) SelectedNode = node.Parent;
                    e.Handled = true;
                    return;
                case Keys.Add:
                    ExpandNode(node);
                    e.Handled = true;
                    return;
                case Keys.Subtract:
                    CollapseNode(node);
                    e.Handled = true;
                    return;
                case Keys.Enter when node.HasChildren:
                    Toggle(node);
                    e.Handled = true;
                    return;
            }
        }
        base.OnKeyDown(e);
    }

    private void Toggle(TwTreeNode node)
    {
        if (node.IsExpanded) CollapseNode(node);
        else ExpandNode(node);
    }

    protected override AccessibleObject CreateAccessibilityInstance() => new TwTreeAccessibleObject(this);
}
