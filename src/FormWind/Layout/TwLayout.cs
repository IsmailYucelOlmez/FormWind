using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Forms.Layout;

namespace FormWind;

/// <summary>
/// Yerleşim motorunun kendisi (<see cref="LayoutEngine"/>) yerine geçebilen kontroller. <see cref="TwPanel"/> bunu uygular;
/// kendi konteynerinizde <c>LayoutEngine</c> getter'ından <see cref="TwLayout.Engine"/> döndürün.
/// </summary>
public interface ITwLayoutHost
{
}

/// <summary>Kontrollerin flex/grid yerleşim değerlerini tutar ve uygulayıcılardan güncellenir.</summary>
public static class TwLayout
{
    private static readonly ConditionalWeakTable<Control, TwLayoutData> Table = new ConditionalWeakTable<Control, TwLayoutData>();

    [ThreadStatic]
    private static int _engineDepth;

    /// <summary>Flex/grid konteynerlerinin kullandığı yerleşim motoru.</summary>
    public static LayoutEngine Engine => TwLayoutEngine.Instance;

    internal static bool InEngine => _engineDepth > 0;

    /// <summary>
    /// Stilin yerleşim kısmını kontrole işler. Değer değiştiyse ilgili konteyneri yeniden yerleştirir.
    /// Özel uygulayıcılar (<see cref="IStyleApplier"/>) bunu çağırmalıdır.
    /// </summary>
    public static void Apply(Control control, TwStyle style, float scale)
    {
        if (control is null) throw new ArgumentNullException(nameof(control));
        if (style is null) throw new ArgumentNullException(nameof(style));

        var container = TwContainerProps.From(style, scale);
        var item = TwItemProps.From(style, scale);
        if (container is null && item.IsDefault && !Table.TryGetValue(control, out _)) return;

        var data = Get(control);

        if (!Equals(data.Container, container))
        {
            data.Container = container;
            if (control is ITwLayoutHost) control.PerformLayout();
            else if (container != null)
                Debug.WriteLine($"FormWind: '{control.Name}' flex/grid için bir TwPanel olmalı; yerleşim class'ları yok sayıldı.");
        }

        if (!Equals(data.Item, item))
        {
            data.Item = item;
            control.Parent?.PerformLayout();
        }
    }

    public static bool IsLayoutContainer(Control control) =>
        Table.TryGetValue(control, out var data) && data.Container != null;

    /// <summary>Flex konteynerinin içerikten hesaplanan boyutu (AutoSize için). Grid için hesaplanmaz.</summary>
    public static bool TryMeasure(Control host, out Size size)
    {
        size = Size.Empty;
        if (!Table.TryGetValue(host, out var hostData) || hostData.Container is not { Display: TwDisplay.Flex } container)
            return false;

        bool row = container.Direction == TwFlexDirection.Row;
        float main = 0, cross = 0;
        int count = 0;

        foreach (Control child in host.Controls)
        {
            if (!child.Visible && host.Visible) continue;
            var data = Get(child);
            var item = data.Item;
            var natural = data.Measure(child);
            float width = item.Width.IsPixels ? item.Width.Value : natural.Width;
            float height = item.Height.IsPixels ? item.Height.Value : natural.Height;
            main += row ? width + item.Margin.Horizontal : height + item.Margin.Vertical;
            cross = Math.Max(cross, row ? height + item.Margin.Vertical : width + item.Margin.Horizontal);
            count++;
        }

        if (count > 1) main += (row ? container.GapX : container.GapY) * (count - 1);

        var padding = host.Padding;
        if (host is TwPanel panel)
        {
            var border = panel.BorderInsets();
            padding = new Padding(padding.Left + border.Left, padding.Top + border.Top, padding.Right + border.Right, padding.Bottom + border.Bottom);
        }
        int mainSize = (int)Math.Ceiling(main), crossSize = (int)Math.Ceiling(cross);
        size = row
            ? new Size(mainSize + padding.Horizontal, crossSize + padding.Vertical)
            : new Size(crossSize + padding.Horizontal, mainSize + padding.Vertical);
        return true;
    }

    internal static TwLayoutData Get(Control control) => Table.GetValue(control, c => new TwLayoutData(c));

    internal static TwLayoutData? Find(Control control) => Table.TryGetValue(control, out var data) ? data : null;

    internal static void Enter() => _engineDepth++;

    internal static void Exit() => _engineDepth--;
}

internal sealed class TwLayoutData
{
    public TwLayoutData(Control control)
    {
        NaturalSize = control.Size;
        // Motorun kendi atamaları dışındaki boyut değişiklikleri (Designer, AutoSize, w-32) doğal boyut sayılır.
        control.SizeChanged += (_, _) =>
        {
            if (!TwLayout.InEngine) NaturalSize = control.Size;
        };
    }

    public TwContainerProps? Container { get; set; }

    public TwItemProps Item { get; set; } = TwItemProps.Default;

    /// <summary>Kontrolün yerleşimden bağımsız boyutu; <c>auto</c> uzunlukların karşılığı.</summary>
    public Size NaturalSize { get; private set; }

    /// <summary>
    /// AutoSize kontrollerde tercih edilen boyut, diğerlerinde doğal boyut. Özel bir yerleşim motoru içindeki
    /// AutoSize kontroller kendini boyutlandırmaz (WinForms bunu ebeveynin motoruna bırakır).
    /// </summary>
    public Size Measure(Control control) => control.AutoSize ? control.GetPreferredSize(Size.Empty) : NaturalSize;
}

internal sealed record TwContainerProps(
    TwDisplay Display,
    TwFlexDirection Direction,
    bool Wrap,
    int GapX,
    int GapY,
    TwJustify Justify,
    TwAlign Items,
    int Columns)
{
    public static TwContainerProps? From(TwStyle style, float scale) =>
        style.Display is TwDisplay.Flex or TwDisplay.Grid
            ? new TwContainerProps(
                style.Display.Value,
                style.FlexDirection ?? TwFlexDirection.Row,
                style.FlexWrap ?? false,
                TwSides.Scale(style.GapX ?? 0, scale),
                TwSides.Scale(style.GapY ?? 0, scale),
                style.JustifyContent ?? TwJustify.Start,
                style.AlignItems ?? TwAlign.Stretch,
                Math.Max(1, style.GridColumns ?? 1))
            : null;
}

internal sealed record TwItemProps(
    float Grow,
    float Shrink,
    TwLength Basis,
    TwAlign? Self,
    int ColumnSpan,
    TwLength Width,
    TwLength Height,
    int? MinWidth,
    int? MaxWidth,
    int? MinHeight,
    int? MaxHeight,
    Padding Margin)
{
    // CSS varsayılanları: flex: 0 1 auto, margin 0. WinForms'un varsayılan 3 px Margin'i kullanılmaz.
    public static readonly TwItemProps Default = new TwItemProps(
        0, 1, TwLength.Auto, null, 1, TwLength.Auto, TwLength.Auto, null, null, null, null, Padding.Empty);

    public bool IsDefault => Equals(Default);

    public static TwItemProps From(TwStyle style, float scale) => new TwItemProps(
        style.FlexGrow ?? 0,
        style.FlexShrink ?? 1,
        (style.FlexBasis ?? TwLength.Auto).Scale(scale),
        style.AlignSelf,
        style.ColumnSpan ?? 1,
        (style.Width ?? TwLength.Auto).Scale(scale),
        (style.Height ?? TwLength.Auto).Scale(scale),
        Scale(style.MinWidth, scale),
        Scale(style.MaxWidth, scale),
        Scale(style.MinHeight, scale),
        Scale(style.MaxHeight, scale),
        style.Margin.ApplyTo(Padding.Empty, scale));

    private static int? Scale(int? pixels, float scale) => pixels is int value ? TwSides.Scale(value, scale) : null;
}
