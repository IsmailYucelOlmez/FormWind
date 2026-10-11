using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// DataGridView'u VirtualMode'da veri kaynağına bağlar: satırlar görüldükçe sayfa sayfa yüklenir, son kullanılan
/// sayfalar önbellekte tutulur. Milyon satırlı bir tablo yalnızca görünen sayfaları belleğe alır.
/// <code>
/// var rows = new TwVirtualRows&lt;Order&gt;(grid, (start, count) =&gt; db.Orders.Skip(start).Take(count).ToList());
/// rows.Reset(db.Orders.Count());
/// </code>
/// Hücre değeri kolonun <see cref="DataGridViewColumn.DataPropertyName"/>'inden (yoksa <see cref="DataGridViewColumn.Name"/>'inden)
/// okunur; <see cref="CellValue"/> ile değiştirilebilir. <c>loadPage</c> UI iş parçacığında çağrılır, hızlı olmalıdır
/// (örn. veritabanında OFFSET/FETCH). VirtualMode'da grid kendisi sıralayamaz; sıralama sorgu tarafında yapılıp
/// <see cref="Reset"/> çağrılmalı.
/// </summary>
public sealed class TwVirtualRows<T> : IDisposable
{
    private readonly DataGridView _grid;
    private readonly Func<int, int, IReadOnlyList<T>> _loadPage;
    private readonly Dictionary<int, LinkedListNode<(int Page, IReadOnlyList<T> Rows)>> _pages = new Dictionary<int, LinkedListNode<(int, IReadOnlyList<T>)>>();
    private readonly LinkedList<(int Page, IReadOnlyList<T> Rows)> _recent = new LinkedList<(int, IReadOnlyList<T>)>();
    private readonly Dictionary<string, PropertyInfo?> _properties = new Dictionary<string, PropertyInfo?>(StringComparer.Ordinal);

    /// <param name="grid">Bağlanacak grid; VirtualMode açılır, kullanıcı satır ekleyemez.</param>
    /// <param name="loadPage"><c>(start, count)</c> aralığındaki satırlar; son sayfada daha az dönebilir.</param>
    /// <param name="pageSize">Bir sayfadaki satır sayısı.</param>
    /// <param name="cachedPages">Bellekte tutulan sayfa sayısı (en az 2: kaydırırken sayfa sınırında gidip gelme).</param>
    public TwVirtualRows(DataGridView grid, Func<int, int, IReadOnlyList<T>> loadPage, int pageSize = 100, int cachedPages = 8)
    {
        _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        _loadPage = loadPage ?? throw new ArgumentNullException(nameof(loadPage));
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));
        PageSize = pageSize;
        CachedPages = Math.Max(2, cachedPages);

        _grid.VirtualMode = true;
        _grid.AllowUserToAddRows = false;
        _grid.CellValueNeeded += OnCellValueNeeded;
    }

    public int PageSize { get; }

    public int CachedPages { get; }

    /// <summary>Satır sayısı (<see cref="Reset"/> ile değişir).</summary>
    public int RowCount => _grid.RowCount;

    /// <summary>Bir satırın bir kolondaki değeri; verilmezse kolonun <c>DataPropertyName</c>'indeki özellik okunur.</summary>
    public Func<T, DataGridViewColumn, object?>? CellValue { get; set; }

    /// <summary>Yüklenen sayfa sayısı (önbellekten verilenler sayılmaz); tanı ve testler için.</summary>
    public int PagesLoaded { get; private set; }

    /// <summary>Önbelleği boşaltır ve satır sayısını ayarlar (veri ya da sıralama değişince).</summary>
    public void Reset(int rowCount)
    {
        _pages.Clear();
        _recent.Clear();
        _grid.RowCount = Math.Max(0, rowCount);
        _grid.Invalidate();
    }

    /// <summary>Satırın verisi (gerekirse sayfası yüklenir); satır yoksa <c>default</c>.</summary>
    public T? GetItem(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _grid.RowCount) return default;
        var rows = Page(rowIndex / PageSize);
        int offset = rowIndex % PageSize;
        return offset < rows.Count ? rows[offset] : default;
    }

    private IReadOnlyList<T> Page(int page)
    {
        if (_pages.TryGetValue(page, out var node))
        {
            _recent.Remove(node);
            _recent.AddFirst(node);
            return node.Value.Rows;
        }
        int start = page * PageSize;
        var rows = _loadPage(start, Math.Min(PageSize, _grid.RowCount - start)) ?? Array.Empty<T>();
        PagesLoaded++;
        _pages[page] = _recent.AddFirst((page, rows));
        while (_recent.Count > CachedPages)
        {
            _pages.Remove(_recent.Last!.Value.Page);
            _recent.RemoveLast();
        }
        return rows;
    }

    private void OnCellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.ColumnIndex < 0) return;
        var item = GetItem(e.RowIndex);
        if (item is null) return;
        var column = _grid.Columns[e.ColumnIndex];
        if (CellValue != null)
        {
            e.Value = CellValue(item, column);
            return;
        }
        string name = string.IsNullOrEmpty(column.DataPropertyName) ? column.Name : column.DataPropertyName;
        if (!_properties.TryGetValue(name, out var property))
            _properties[name] = property = typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (property != null) e.Value = property.GetValue(item);
    }

    public void Dispose() => _grid.CellValueNeeded -= OnCellValueNeeded;
}
