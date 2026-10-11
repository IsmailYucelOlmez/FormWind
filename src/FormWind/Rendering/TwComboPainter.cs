using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// <c>DropDownList</c> modundaki standart <see cref="ComboBox"/>'ın kapalı kutusunu class'larla çizer: zemin, kenarlık,
/// köşe, seçili metin ve ok. Kutunun düzenleme alanı olmadığından tamamı tek bir WM_PAINT'te çizilebilir.
/// Açılır liste native kalır (renkleri <c>BackColor</c>/<c>ForeColor</c>'dan gelir). Düzenlenebilir modlarda
/// (<c>DropDown</c>, <c>Simple</c>) devreye girmez; o kutular için <c>TwSelect</c> kullanılmalı.
/// </summary>
internal sealed class TwComboPainter : NativeWindow
{
    private const int WmPaint = 0x000F;
    private const int WmPrintClient = 0x0318;

    private static readonly ConditionalWeakTable<ComboBox, TwComboPainter> Table = new ConditionalWeakTable<ComboBox, TwComboPainter>();

    private readonly ComboBox _combo;
    private TwChrome _chrome = TwChrome.None;
    private bool _attached;

    private TwComboPainter(ComboBox combo)
    {
        _combo = combo;
        _combo.HandleCreated += (_, _) => { if (_attached) AssignHandle(_combo.Handle); };
        _combo.HandleDestroyed += (_, _) => ReleaseHandle();
    }

    public static bool IsAttached(ComboBox combo) => Table.TryGetValue(combo, out var painter) && painter._attached;

    public static bool Supports(ComboBox combo) => combo.DropDownStyle == ComboBoxStyle.DropDownList;

    public static void Attach(ComboBox combo, TwChrome chrome)
    {
        var painter = Table.GetValue(combo, c => new TwComboPainter(c));
        painter._chrome = chrome;
        if (!painter._attached)
        {
            painter._attached = true;
            if (combo.IsHandleCreated) painter.AssignHandle(combo.Handle);
        }
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
        if (!Supports(_combo) || (m.Msg != WmPaint && m.Msg != WmPrintClient))
        {
            base.WndProc(ref m);
            return;
        }

        // WM_PRINTCLIENT (DrawToBitmap) ve hdc'li WM_PAINT verilen hdc'ye çizer; düz WM_PAINT BeginPaint ile.
        if (m.WParam != IntPtr.Zero)
        {
            using (var g = Graphics.FromHdc(m.WParam)) Paint(g);
            m.Result = IntPtr.Zero;
            return;
        }

        var ps = new PaintStruct();
        var hdc = BeginPaint(m.HWnd, ref ps);
        try
        {
            using var g = Graphics.FromHdc(hdc);
            Paint(g);
        }
        finally
        {
            EndPaint(m.HWnd, ref ps);
        }
        m.Result = IntPtr.Zero;
    }

    internal void Paint(Graphics g)
    {
        try
        {
            PaintCore(g);
        }
        catch (Exception ex) when (!TwErrors.IsCritical(ex))
        {
            TwErrors.Trace("ComboBox painting", ex);
        }
    }

    private void PaintCore(Graphics g)
    {
        var combo = _combo;
        var bounds = new Rectangle(Point.Empty, combo.Size);
        float scale = TwDpi.DeviceDpi(combo) / 96f;

        TwPainter.PaintParentBackground(combo, g);
        TwPainter.FillSurface(g, bounds, _chrome.Radius, combo.BackColor, _chrome.Gradient);
        TwPainter.StrokeChrome(g, bounds, _chrome);

        var fore = combo.Enabled ? combo.ForeColor : Color.FromArgb(128, combo.ForeColor);
        int chevron = (int)Math.Round(28 * scale);
        int left = (int)Math.Ceiling(_chrome.BorderLeft) + (combo.Padding.Left > 0 ? combo.Padding.Left : (int)Math.Round(8 * scale));
        var text = combo.SelectedIndex >= 0 ? combo.GetItemText(combo.SelectedItem) : combo.Text;
        var textBounds = new Rectangle(left, 0, Math.Max(0, bounds.Width - left - chevron), bounds.Height);
        TextRenderer.DrawText(g, text, combo.Font, textBounds, fore,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        int em = TwIconRenderer.EmPixels(combo.Font.SizeInPoints * 0.85f, TwDpi.DeviceDpi(combo));
        var chevronBounds = new Rectangle(bounds.Width - chevron - (int)Math.Ceiling(_chrome.BorderRight), 0, chevron, bounds.Height);
        TwIconRenderer.DrawIcon(g, "chevron-down", em, chevronBounds, fore);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PaintStruct
    {
        public IntPtr Hdc;
        public bool Erase;
        public int Left, Top, Right, Bottom;
        public bool Restore;
        public bool IncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] Reserved;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr BeginPaint(IntPtr hwnd, ref PaintStruct paint);

    [DllImport("user32.dll")]
    private static extern bool EndPaint(IntPtr hwnd, ref PaintStruct paint);
}
