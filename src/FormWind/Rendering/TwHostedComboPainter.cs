using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// Şeride gömülü <see cref="ToolStripComboBox"/>'ın çerçevesi ve ok düğmesi. Şeridin renderer'ı
/// <see cref="ToolStripProfessionalRenderer"/> değilse WinForms bunları sabit sistem renkleriyle çizer (açık gri düğme,
/// siyah ok, beyaz çerçeve); koyu şeritte göze batardı. WinForms'un çiziminden sonra aynı alanlar kutunun
/// <c>BackColor</c>/<c>ForeColor</c>'ıyla yeniden çizilir. Düzenleme alanına (alt pencere) dokunulmaz.
/// </summary>
internal sealed class TwHostedComboPainter : NativeWindow
{
    private const int WmPaint = 0x000F;
    private const int WhiteFillWidth = 5; // WinForms'un FlatComboAdapter'ındaki düğme önü dolgusu

    private static readonly ConditionalWeakTable<ComboBox, TwHostedComboPainter> Table = new ConditionalWeakTable<ComboBox, TwHostedComboPainter>();

    private readonly ComboBox _combo;
    private bool _attached;

    private TwHostedComboPainter(ComboBox combo)
    {
        _combo = combo;
        _combo.HandleCreated += (_, _) => { if (_attached) AssignHandle(_combo.Handle); };
        _combo.HandleDestroyed += (_, _) => ReleaseHandle();
    }

    public static bool IsAttached(ComboBox combo) => Table.TryGetValue(combo, out var painter) && painter._attached;

    public static void Attach(ComboBox combo)
    {
        var painter = Table.GetValue(combo, c => new TwHostedComboPainter(c));
        if (painter._attached) return;
        painter._attached = true;
        if (combo.IsHandleCreated) painter.AssignHandle(combo.Handle);
        combo.Invalidate();
    }

    public static void Detach(ComboBox combo)
    {
        if (!Table.TryGetValue(combo, out var painter) || !painter._attached) return;
        painter._attached = false;
        painter.ReleaseHandle();
        combo.Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg != WmPaint || _combo.FlatStyle == FlatStyle.Standard || _combo.FlatStyle == FlatStyle.System) return;
        try
        {
            using var g = _combo.CreateGraphics();
            Paint(g, _combo);
        }
        catch (Exception ex) when (!TwErrors.IsCritical(ex))
        {
            TwErrors.Trace("şerit açılır kutusu", ex);
        }
    }

    internal static void Paint(Graphics g, ComboBox combo)
    {
        var client = combo.ClientRectangle;
        if (client.Width <= 0 || client.Height <= 0) return;
        float scale = TwDpi.DeviceDpi(combo) / 96f;
        int arrowWidth = (int)Math.Round(SystemInformation.HorizontalScrollBarArrowWidth * scale / (TwDpi.SystemDpi / 96f));
        var back = combo.BackColor;
        var fore = combo.Enabled ? combo.ForeColor : Color.FromArgb(128, combo.ForeColor);
        var border = Color.FromArgb(
            back.R + (fore.R - back.R) / 4,
            back.G + (fore.G - back.G) / 4,
            back.B + (fore.B - back.B) / 4);

        // FlatComboAdapter yerleşimi: dış çerçeve, içinde düğme (önünde 5 px dolgu).
        var button = new Rectangle(client.Width - arrowWidth - 1, 1, arrowWidth, client.Height - 2);
        if (combo.RightToLeft == RightToLeft.Yes) button.X = 1;
        using (var brush = new SolidBrush(back)) g.FillRectangle(brush, button);
        using (var pen = new Pen(back))
            g.DrawRectangle(pen, 1, 1, client.Width - 3, client.Height - 3);
        using (var pen = new Pen(border))
            g.DrawRectangle(pen, 0, 0, client.Width - 1, client.Height - 1);

        var arrow = Rectangle.FromLTRB(button.X + WhiteFillWidth, button.Y, button.Right, button.Bottom);
        if (combo.RightToLeft == RightToLeft.Yes) arrow = Rectangle.FromLTRB(button.X, button.Y, button.Right - WhiteFillWidth, button.Bottom);
        int half = Math.Max(2, (int)Math.Round(2.5f * scale));
        var center = new Point(arrow.X + arrow.Width / 2, arrow.Y + arrow.Height / 2);
        using var arrowBrush = new SolidBrush(fore);
        g.FillPolygon(arrowBrush, new[]
        {
            new Point(center.X - half, center.Y - half / 2),
            new Point(center.X + half + 1, center.Y - half / 2),
            new Point(center.X, center.Y + half / 2 + 1),
        });
    }
}
