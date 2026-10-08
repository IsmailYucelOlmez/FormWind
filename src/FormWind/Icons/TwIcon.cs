using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// Bir ikonu çizen hafif kontrol. Renk <c>text-*</c> (ForeColor), boyut <c>text-*</c> font boyutuyla verilir:
/// <c>text-base</c> = 16 px ikon. AutoSize varsayılan olarak açıktır.
/// </summary>
[ToolboxItem(true)]
[DefaultProperty(nameof(IconName))]
[Description("Windows ikon fontundan bir ikon çizer.")]
public class TwIcon : Control
{
    private string _iconName = "";

    public TwIcon()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor,
            true);
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
        AutoSize = true;
    }

    [Category("FormWind"), DefaultValue(""), TypeConverter(typeof(TwIconNameConverter))]
    [Description("İkon adı (örn. plus, search, user) ya da kod noktası (#E710).")]
    public string IconName
    {
        get => _iconName;
        set
        {
            _iconName = value ?? "";
            if (AutoSize) Parent?.PerformLayout(this, nameof(IconName));
            Invalidate();
        }
    }

    [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), DefaultValue(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public override bool AutoSize
    {
        get => base.AutoSize;
        set => base.AutoSize = value;
    }

    protected override Size DefaultSize => new Size(16, 16);

    /// <summary>Kare ikon kutusunun kenarı (font boyutunun piksel karşılığı).</summary>
    internal int EmPixels => TwIconRenderer.EmPixels(Font.SizeInPoints, DeviceDpi);

    public override Size GetPreferredSize(Size proposedSize)
    {
        var em = EmPixels;
        return new Size(em + Padding.Horizontal, em + Padding.Vertical);
    }

    // AutoSize kontrolün tercih edilen boyutu değişince ebeveyn yeniden yerleşmeli (Label bunu kendisi yapar).
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (AutoSize) Parent?.PerformLayout(this, nameof(Font));
        Invalidate();
    }

    protected override void OnPaddingChanged(EventArgs e)
    {
        base.OnPaddingChanged(e);
        if (AutoSize) Parent?.PerformLayout(this, nameof(Padding));
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!TwTheme.Current.TryGetIcon(_iconName, out var glyph)) return;

        var bounds = new Rectangle(
            Padding.Left,
            Padding.Top,
            Math.Max(0, Width - Padding.Horizontal),
            Math.Max(0, Height - Padding.Vertical));
        TwIconRenderer.Draw(e.Graphics, glyph, TwTheme.Current.IconFontFamily, EmPixels, bounds, ForeColor);
    }
}
