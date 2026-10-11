using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace FormWind;

/// <summary>Kolon değerine göre süzülebilen liste (<see cref="TwGrid.EnableFilterMenu"/> bunu kullanır).</summary>
public interface ITwFilterableList
{
    /// <summary>Özelliğin tüm (süzülmemiş) satırlardaki farklı değerleri, görünen metinleriyle, sıralı.</summary>
    IReadOnlyList<string> DistinctValues(string property);

    /// <summary>Kolonda gösterilen değerler; süzme yoksa <c>null</c>.</summary>
    IReadOnlyCollection<string>? GetColumnFilter(string property);

    /// <summary>Kolonda yalnızca verilen değerleri gösterir (<c>null</c> süzmeyi kaldırır).</summary>
    void SetColumnFilter(string property, IEnumerable<string>? allowed);

    bool HasFilters { get; }

    void ClearFilters();
}

/// <summary>
/// Sıralanabilen ve kolon değerine göre süzülebilen <see cref="BindingList{T}"/>. <c>List&lt;T&gt;</c> ya da düz
/// <c>BindingList&lt;T&gt;</c> ile bağlanan DataGridView başlığa tıklanınca sıralamaz; bu liste ile kolonların
/// <c>SortMode = Automatic</c> sıralaması, başlıktaki ok ve <see cref="TwGrid.EnableFilterMenu"/> süzme menüsü çalışır.
/// Metinler geçerli kültüre göre (Türkçe harf sırası dahil), diğer değerler <see cref="IComparable"/> ile karşılaştırılır.
/// </summary>
public class TwSortableBindingList<T> : BindingList<T>, ITwFilterableList
{
    /// <summary>
    /// Boş (<c>null</c> ya da boş metin) değerlerin süzme anahtarı (<see cref="SetColumnFilter"/>). Dilden bağımsızdır;
    /// süzme menüsünde <see cref="TwTexts.Blank"/> olarak görünür.
    /// </summary>
    public const string EmptyValue = "(blank)";

    private readonly List<T> _all;       // geçerli sırada tüm satırlar (sıralamalar üst üste uygulanır)
    private readonly List<T> _original;  // eklenme sırası (sıralama kaldırılınca)
    private readonly Dictionary<string, HashSet<string>> _filters = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    private readonly PropertyDescriptorCollection _properties = TypeDescriptor.GetProperties(typeof(T));
    private PropertyDescriptor? _sortProperty;
    private ListSortDirection _sortDirection;
    private bool _sorted;
    private bool _rebuilding;

    public TwSortableBindingList() : this(Enumerable.Empty<T>())
    {
    }

    public TwSortableBindingList(IEnumerable<T> items) : base(items.ToList())
    {
        _all = new List<T>(Items);
        _original = new List<T>(Items);
    }

    /// <summary>Süzmeden bağımsız tüm satırlar.</summary>
    public IReadOnlyList<T> AllItems => _all;

    protected override bool SupportsSortingCore => true;

    protected override bool IsSortedCore => _sorted;

    protected override PropertyDescriptor? SortPropertyCore => _sortProperty;

    protected override ListSortDirection SortDirectionCore => _sortDirection;

    protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
    {
        _sortProperty = prop;
        _sortDirection = direction;
        _sorted = true;
        SortAll(prop, direction);
        Rebuild();
    }

    protected override void RemoveSortCore()
    {
        _sorted = false;
        _sortProperty = null;
        _all.Clear();
        _all.AddRange(_original);   // eklenme sırası
        Rebuild();
    }

    // Kararlı sıralama, önceki sıranın üzerine: eşit değerler önceki sıralamadaki yerini korur (önce ada, sonra yaşa göre
    // sıralanan listede aynı yaştakiler ada göre kalır).
    private void SortAll(PropertyDescriptor prop, ListSortDirection direction)
    {
        var compare = CultureInfo.CurrentCulture.CompareInfo;
        int Compare(T a, T b)
        {
            object? x = prop.GetValue(a), y = prop.GetValue(b);
            int result = x is null ? (y is null ? 0 : -1)
                : y is null ? 1
                : x is string sx && y is string sy ? compare.Compare(sx, sy, CompareOptions.IgnoreCase)
                : x is IComparable comparable ? comparable.CompareTo(y)
                : string.Compare(x.ToString(), y.ToString(), StringComparison.CurrentCulture);
            return direction == ListSortDirection.Ascending ? result : -result;
        }

        var indexed = _all.Select((item, index) => (Item: item, Index: index)).ToList();
        indexed.Sort((p, q) =>
        {
            int c = Compare(p.Item, q.Item);
            return c != 0 ? c : p.Index.CompareTo(q.Index);
        });
        _all.Clear();
        _all.AddRange(indexed.Select(p => p.Item));
    }

    public bool HasFilters => _filters.Count > 0;

    public IReadOnlyList<string> DistinctValues(string property)
    {
        var descriptor = Property(property);
        var compare = CultureInfo.CurrentCulture.CompareInfo;
        var values = _all.Select(item => Display(descriptor.GetValue(item))).Distinct(StringComparer.Ordinal).ToList();
        values.Sort((a, b) => a == EmptyValue ? -1 : b == EmptyValue ? 1 : compare.Compare(a, b, CompareOptions.IgnoreCase));
        return values;
    }

    public IReadOnlyCollection<string>? GetColumnFilter(string property) =>
        _filters.TryGetValue(property, out var allowed) ? allowed : null;

    public void SetColumnFilter(string property, IEnumerable<string>? allowed)
    {
        Property(property);
        if (allowed is null) _filters.Remove(property);
        else _filters[property] = new HashSet<string>(allowed, StringComparer.Ordinal);
        Rebuild();
    }

    public void ClearFilters()
    {
        _filters.Clear();
        Rebuild();
    }

    // Görünen liste: süzmeden geçen satırlar, geçerli sırada.
    private void Rebuild()
    {
        if (Items is not List<T> items) return;
        var visible = _all.Where(Passes).ToList();

        _rebuilding = true;
        try
        {
            items.Clear();
            items.AddRange(visible);
        }
        finally
        {
            _rebuilding = false;
        }
        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }

    private bool Passes(T item)
    {
        foreach (var filter in _filters)
            if (!filter.Value.Contains(Display(Property(filter.Key).GetValue(item)))) return false;
        return true;
    }

    private PropertyDescriptor Property(string name) =>
        _properties.Find(name, false) ?? throw new ArgumentException($"Type {typeof(T).Name} has no property '{name}'.", nameof(name));

    internal static string Display(object? value) => value switch
    {
        null => EmptyValue,
        string text when text.Length == 0 => EmptyValue,
        IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture),
        _ => value.ToString() ?? EmptyValue,
    };

    // Görünen listeye yapılan eklemeler/silmeler tüm satırlara da yansır.
    protected override void InsertItem(int index, T item)
    {
        base.InsertItem(index, item);
        if (_rebuilding) return;
        _all.Add(item);
        _original.Add(item);
    }

    protected override void RemoveItem(int index)
    {
        if (!_rebuilding)
        {
            _all.Remove(this[index]);
            _original.Remove(this[index]);
        }
        base.RemoveItem(index);
    }

    protected override void SetItem(int index, T item)
    {
        if (!_rebuilding)
        {
            int at = _all.IndexOf(this[index]);
            if (at >= 0) _all[at] = item;
            at = _original.IndexOf(this[index]);
            if (at >= 0) _original[at] = item;
        }
        base.SetItem(index, item);
    }

    protected override void ClearItems()
    {
        if (!_rebuilding)
        {
            _all.Clear();
            _original.Clear();
        }
        base.ClearItems();
    }
}
