using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FormWind;

/// <summary>Menü ve araç çubuğu renkleri. Boş bırakılan renkler varsayılan açık temadan gelir.</summary>
public sealed class TwMenuColors
{
    public Color Back { get; set; } = Color.White;
    public Color Fore { get; set; } = ColorTranslator.FromHtml("#374151");          // gray-700
    public Color MenuBack { get; set; } = Color.White;
    public Color MenuBorder { get; set; } = ColorTranslator.FromHtml("#e5e7eb");    // gray-200
    public Color ItemHoverBack { get; set; } = ColorTranslator.FromHtml("#f3f4f6"); // gray-100
    public Color ItemHoverFore { get; set; } = ColorTranslator.FromHtml("#111827"); // gray-900
    public Color Separator { get; set; } = ColorTranslator.FromHtml("#e5e7eb");
    public Color Accent { get; set; } = ColorTranslator.FromHtml("#2563eb");        // blue-600
    public Color? BottomBorder { get; set; }
    public float ItemRadius { get; set; } = 4;
}

/// <summary>Tema renkleriyle çizen ToolStrip renderer'ı: düz yüzeyler, yuvarlak hover, ince kenarlıklar.</summary>
public class TwToolStripRenderer : ToolStripRenderer
{
    public TwToolStripRenderer(TwMenuColors colors) => Colors = colors ?? throw new ArgumentNullException(nameof(colors));

    public TwMenuColors Colors { get; }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        var back = e.ToolStrip is ToolStripDropDown ? Colors.MenuBack : Colors.Back;
        using var brush = new SolidBrush(back);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        if (e.ToolStrip is ToolStripDropDown)
        {
            using var pen = new Pen(Colors.MenuBorder);
            var r = e.ToolStrip.ClientRectangle;
            e.Graphics.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
        }
        else if (Colors.BottomBorder is Color bottom)
        {
            using var pen = new Pen(bottom);
            var r = e.ToolStrip.ClientRectangle;
            e.Graphics.DrawLine(pen, r.Left, r.Bottom - 1, r.Right, r.Bottom - 1);
        }
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        // Görüntü kenar boşluğu ayrı renklendirilmez; menü arka planıyla aynı kalır.
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e) => PaintItemBackground(e);

    protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e) => PaintItemBackground(e);

    protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e) => PaintItemBackground(e);

    protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e) => PaintItemBackground(e);

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = !e.Item.Enabled ? Color.FromArgb(128, Colors.Fore)
            : IsHighlighted(e.Item) ? Colors.ItemHoverFore
            : Colors.Fore;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = e.Item is { Enabled: false } ? Color.FromArgb(128, Colors.Fore) : Colors.Fore;
        base.OnRenderArrow(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(Colors.Separator);
        var r = e.Item.ContentRectangle;
        if (e.Vertical) e.Graphics.DrawLine(pen, r.Width / 2, r.Top + 4, r.Width / 2, r.Bottom - 4);
        else e.Graphics.DrawLine(pen, r.Left + 4, r.Height / 2, r.Right - 4, r.Height / 2);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var r = e.ImageRectangle;
        if (r.Width <= 0) return;
        TwToggleDrawing.DrawCheck(e.Graphics, r, Colors.Accent, Math.Max(1.5f, r.Width / 8f));
    }

    private void PaintItemBackground(ToolStripItemRenderEventArgs e)
    {
        var item = e.Item;
        bool checkedButton = item is ToolStripButton { Checked: true };
        if (!IsHighlighted(item) && !checkedButton) return;

        var bounds = new Rectangle(Point.Empty, item.Size);
        if (item.Owner is ToolStripDropDown) bounds.Inflate(-4, -1);
        else bounds.Inflate(-1, -1);

        var state = e.Graphics.Save();
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        TwPainter.FillRounded(e.Graphics, bounds, Colors.ItemRadius, Colors.ItemHoverBack);
        e.Graphics.Restore(state);
    }

    private static bool IsHighlighted(ToolStripItem item) => item.Enabled && (item.Selected || item.Pressed);
}

/// <summary>
/// MenuStrip, ToolStrip, StatusStrip ve ContextMenuStrip'i stiller. Temel class'lar şeridin kendisine,
/// <c>menu:</c> açılan menülere, <c>item:</c> öğelere uygulanır:
/// <c>bg-white text-gray-700 border-b menu:bg-white menu:border-gray-200 item:hover:bg-gray-100 item:rounded-md divide-gray-200</c>.
/// </summary>
public class ToolStripApplier : WinFormsApplier
{
    public override bool CanApply(Control control) => control is ToolStrip;

    public override void Apply(Control control, TwStyle style, TwApplyContext context)
    {
        base.Apply(control, style, context);

        var strip = (ToolStrip)control;
        strip.RenderMode = ToolStripRenderMode.ManagerRenderMode; // Renderer atanınca Custom'a geçer
        strip.Renderer = new TwToolStripRenderer(BuildColors(style, context));
    }

    /// <summary>Şerit/menü renklerini temel class'lar ile <c>menu:</c> ve <c>item:</c> parçalarından kurar.</summary>
    public static TwMenuColors BuildColors(TwStyle style, TwApplyContext context)
    {
        var state = context.State & ~(TwState.Hover | TwState.Active);
        var parsed = context.Parsed;
        var menu = parsed.ResolvePart("menu", state);
        var item = parsed.ResolvePart("item", state);
        var itemHover = parsed.ResolvePart("item", state | TwState.Hover);

        var colors = new TwMenuColors();
        if (style.BackColor is Color back) colors.Back = back;
        if (style.ForeColor is Color fore) colors.Fore = fore;
        colors.MenuBack = menu.BackColor ?? style.BackColor ?? colors.MenuBack;
        if (menu.BorderColor is Color menuBorder) colors.MenuBorder = menuBorder;
        if (item.ForeColor is Color itemFore) colors.Fore = itemFore;
        if (itemHover.BackColor is Color hoverBack) colors.ItemHoverBack = hoverBack;
        colors.ItemHoverFore = itemHover.ForeColor ?? colors.Fore;
        if (style.DivideColor is Color divide) colors.Separator = divide;
        if (style.AccentColor is Color accent) colors.Accent = accent;
        if (item.Radius is int radius) colors.ItemRadius = radius * context.Scale;
        if ((style.BorderSides.Bottom ?? style.BorderWidth ?? 0) > 0)
            colors.BottomBorder = style.BorderColor ?? context.Theme.DefaultBorderColor;
        return colors;
    }
}

/// <summary>
/// Form'un başlık çubuğunu stiller (Windows 10 20H1+: koyu başlık; Windows 11: başlık, metin ve çerçeve rengi).
/// Koyu tema açıkken başlık çubuğu otomatik koyulaşır; renkler <c>caption:bg-*</c>, <c>caption:text-*</c>,
/// <c>caption:border-*</c> ile verilir. Eski Windows sürümlerinde sessizce yok sayılır.
/// </summary>
public class FormApplier : WinFormsApplier
{
    public override bool CanApply(Control control) => control is Form;

    public override void Apply(Control control, TwStyle style, TwApplyContext context)
    {
        base.Apply(control, style, context);
        if (context.DesignMode) return;

        var form = (Form)control;
        var caption = context.Parsed.ResolvePart("caption", context.State & ~(TwState.Hover | TwState.Active));
        TwTitleBar.For(form).Update(
            (context.State & TwState.Dark) != 0,
            caption.BackColor,
            caption.ForeColor,
            caption.BorderColor);
    }
}
