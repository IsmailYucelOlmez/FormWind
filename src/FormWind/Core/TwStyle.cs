using System.Drawing;
using System.Windows.Forms;

namespace FormWind;

public enum TwTextAlign
{
    Left,
    Center,
    Right,
}

/// <summary>
/// Bir varyantın ya da birleştirilmiş durumun stil değerleri. <c>null</c> değer "bu class'lar
/// bu property'ye dokunmuyor" anlamına gelir. Piksel değerleri 96 DPI'a göredir.
/// </summary>
public sealed class TwStyle
{
    public Color? BackColor { get; set; }
    public Color? ForeColor { get; set; }
    public Color? BorderColor { get; set; }
    public int? BorderWidth { get; set; }

    /// <summary>Kenar bazlı kalınlıklar (<c>border-b</c>, <c>border-x-2</c>); tanımsız kenar <see cref="BorderWidth"/>'i kullanır.</summary>
    public TwSides BorderSides { get; } = new TwSides();

    public TwShadowSize? Shadow { get; set; }
    public Color? ShadowColor { get; set; }
    public int? RingWidth { get; set; }
    public Color? RingColor { get; set; }
    public int? RingOffset { get; set; }
    public Color? RingOffsetColor { get; set; }

    public TwGradientDirection? GradientDirection { get; set; }
    public Color? GradientFrom { get; set; }
    public Color? GradientVia { get; set; }
    public Color? GradientTo { get; set; }

    /// <summary><c>fill-*</c>: işaret kutusu, anahtar rayı gibi iç yüzeylerin dolgu rengi.</summary>
    public Color? FillColor { get; set; }

    /// <summary><c>accent-*</c>: seçili CheckBox/Radio/Switch rengi.</summary>
    public Color? AccentColor { get; set; }

    /// <summary><c>object-cover</c> / <c>object-contain</c> / <c>object-fill</c>: <see cref="TwPictureBox"/> resim yerleşimi.</summary>
    public TwImageFit? ObjectFit { get; set; }

    /// <summary><c>group</c>: alt kontroller <c>group-hover:</c> ile bu kontrolün hover durumuna tepki verir.</summary>
    public bool? IsGroup { get; set; }

    /// <summary><c>transition</c>: arka plan ve metin rengi değişimleri <see cref="DurationMs"/> boyunca geçişle uygulanır.</summary>
    public bool? Transition { get; set; }

    /// <summary><c>duration-*</c> (ms). Tailwind varsayılanı 150.</summary>
    public int? DurationMs { get; set; }
    public int? Radius { get; set; }
    public float? FontSizePx { get; set; }
    public string? FontFamily { get; set; }
    /// <summary>CSS font kalınlığı: 100 (thin) … 400 (normal) … 700 (bold) … 900 (black).</summary>
    public int? FontWeight { get; set; }
    public bool? Italic { get; set; }
    public bool? Underline { get; set; }
    public TwSides Padding { get; } = new TwSides();
    public TwSides Margin { get; } = new TwSides();
    public TwLength? Width { get; set; }
    public TwLength? Height { get; set; }
    public int? MinWidth { get; set; }
    public int? MaxWidth { get; set; }
    public int? MinHeight { get; set; }
    public int? MaxHeight { get; set; }
    public TwTextAlign? TextAlign { get; set; }

    /// <summary><c>whitespace-nowrap</c> = true. Şimdilik DataGridView hücrelerinde (WrapMode) kullanılır.</summary>
    public bool? NoWrap { get; set; }

    /// <summary><c>truncate</c>: sığmayan metin "…" ile kısaltılır (Label.AutoEllipsis).</summary>
    public bool? Truncate { get; set; }

    /// <summary>Windows 11 pencere zemini (<c>backdrop-mica</c>, <c>backdrop-acrylic</c>, <c>backdrop-tabbed</c>, <c>backdrop-none</c>); yalnızca Form.</summary>
    public TwBackdrop? Backdrop { get; set; }

    public Cursor? Cursor { get; set; }
    public bool? Hidden { get; set; }

    // Yerleşim konteyneri (flex / grid). Yalnızca ITwLayoutHost kontrollerinde (TwPanel) etkilidir.
    public TwDisplay? Display { get; set; }
    public TwFlexDirection? FlexDirection { get; set; }
    public bool? FlexWrap { get; set; }
    public int? GapX { get; set; }
    public int? GapY { get; set; }
    public TwJustify? JustifyContent { get; set; }
    public TwAlign? AlignItems { get; set; }
    public int? GridColumns { get; set; }

    /// <summary>Grid'in açık satır sayısı (<c>grid-rows-3</c>): kabın yüksekliği eşit satırlara bölünür.</summary>
    public int? GridRows { get; set; }

    // Yerleşim öğesi: ebeveyn flex / grid konteyneriyse etkilidir.
    public float? FlexGrow { get; set; }
    public float? FlexShrink { get; set; }
    public TwLength? FlexBasis { get; set; }
    public TwAlign? AlignSelf { get; set; }

    /// <summary>Grid'de kaplanan kolon sayısı. <see cref="int.MaxValue"/> = <c>col-span-full</c>.</summary>
    public int? ColumnSpan { get; set; }

    /// <summary>Grid'de kaplanan satır sayısı. <see cref="int.MaxValue"/> = <c>row-span-full</c> (açık satırların tümü).</summary>
    public int? RowSpan { get; set; }

    // Ayraç çizgileri (divide-x, divide-y, divide-{renk}). Şimdilik DataGridView ızgara çizgilerinde kullanılır.
    public bool? DivideX { get; set; }
    public bool? DivideY { get; set; }
    public Color? DivideColor { get; set; }

    public bool HasFont =>
        FontSizePx.HasValue || FontFamily != null || FontWeight.HasValue || Italic.HasValue || Underline.HasValue;

    public bool HasChrome =>
        (BorderWidth ?? 0) > 0 || (Radius ?? 0) > 0 || HasSideBorder;

    public bool HasSideBorder =>
        (BorderSides.Left ?? 0) > 0 || (BorderSides.Top ?? 0) > 0
        || (BorderSides.Right ?? 0) > 0 || (BorderSides.Bottom ?? 0) > 0;

    /// <summary>Kontrolün dışına, ebeveyn tarafından çizilen gölge ya da halka var mı.</summary>
    public bool HasOutset => (Shadow ?? TwShadowSize.None) != TwShadowSize.None || (RingWidth ?? 0) > 0;

    public bool HasGradient => GradientDirection.HasValue && (GradientFrom.HasValue || GradientTo.HasValue);

    /// <summary>
    /// Yüksek Karşıtlık modu: renkleri sistem renklerine çevirir. Kontrolün kendi zemini ve yazısı özgün (sistem) değerlerine
    /// bırakılır; seçili ve hover parçaları vurgu renklerini, kenarlık ve ayırıcılar yazı rengini alır. Gölge ve gradient çizilmez.
    /// </summary>
    internal void ApplyForcedColors(string? part, TwState state)
    {
        bool highlighted = part == "selected" || ((part == "item" || part == "row") && (state & TwState.Hover) != 0);
        if (part is null)
        {
            BackColor = null;
            ForeColor = null;
        }
        else if (highlighted)
        {
            // Zemin verilmişse vurgu zemini + vurgu yazısı; yalnızca yazı rengi verilmişse (örn. alt çizgili sekme) vurgu rengi yazıya.
            bool hasBack = BackColor.HasValue;
            ForeColor = hasBack ? SystemColors.HighlightText : ForeColor.HasValue ? SystemColors.Highlight : null;
            BackColor = hasBack ? SystemColors.Highlight : null;
        }
        else if (part == "label" || part == "hint" || part == "error" || part == "placeholder" || part == "item")
        {
            BackColor = null;
            ForeColor = part == "placeholder" ? SystemColors.GrayText : SystemColors.WindowText;
        }
        else
        {
            BackColor = SystemColors.Window;
            ForeColor = SystemColors.WindowText;
        }
        // Kenarlık ve ayırıcılar yalnızca class'larda varsa çizilir; rengi yazı rengidir.
        if (BorderColor.HasValue || BorderWidth.HasValue || HasSideBorder) BorderColor = SystemColors.WindowText;
        if (DivideColor.HasValue || DivideX == true || DivideY == true) DivideColor = SystemColors.WindowText;
        Shadow = null;
        ShadowColor = null;
        RingColor = SystemColors.Highlight;
        RingOffsetColor = SystemColors.Window;
        GradientDirection = null;
        GradientFrom = null;
        GradientVia = null;
        GradientTo = null;
        FillColor = SystemColors.ControlDark;
        AccentColor = SystemColors.Highlight;
        Transition = null;
    }

    /// <summary><paramref name="other"/>'daki tanımlı değerleri bu stilin üzerine yazar.</summary>
    public void MergeFrom(TwStyle other)
    {
        BackColor = other.BackColor ?? BackColor;
        ForeColor = other.ForeColor ?? ForeColor;
        BorderColor = other.BorderColor ?? BorderColor;
        BorderWidth = other.BorderWidth ?? BorderWidth;
        BorderSides.MergeFrom(other.BorderSides);
        Shadow = other.Shadow ?? Shadow;
        ShadowColor = other.ShadowColor ?? ShadowColor;
        RingWidth = other.RingWidth ?? RingWidth;
        RingColor = other.RingColor ?? RingColor;
        RingOffset = other.RingOffset ?? RingOffset;
        RingOffsetColor = other.RingOffsetColor ?? RingOffsetColor;
        GradientDirection = other.GradientDirection ?? GradientDirection;
        GradientFrom = other.GradientFrom ?? GradientFrom;
        GradientVia = other.GradientVia ?? GradientVia;
        GradientTo = other.GradientTo ?? GradientTo;
        FillColor = other.FillColor ?? FillColor;
        AccentColor = other.AccentColor ?? AccentColor;
        ObjectFit = other.ObjectFit ?? ObjectFit;
        IsGroup = other.IsGroup ?? IsGroup;
        Transition = other.Transition ?? Transition;
        DurationMs = other.DurationMs ?? DurationMs;
        Radius = other.Radius ?? Radius;
        FontSizePx = other.FontSizePx ?? FontSizePx;
        FontFamily = other.FontFamily ?? FontFamily;
        FontWeight = other.FontWeight ?? FontWeight;
        Italic = other.Italic ?? Italic;
        Underline = other.Underline ?? Underline;
        Padding.MergeFrom(other.Padding);
        Margin.MergeFrom(other.Margin);
        Width = other.Width ?? Width;
        Height = other.Height ?? Height;
        MinWidth = other.MinWidth ?? MinWidth;
        MaxWidth = other.MaxWidth ?? MaxWidth;
        MinHeight = other.MinHeight ?? MinHeight;
        MaxHeight = other.MaxHeight ?? MaxHeight;
        TextAlign = other.TextAlign ?? TextAlign;
        NoWrap = other.NoWrap ?? NoWrap;
        Truncate = other.Truncate ?? Truncate;
        Backdrop = other.Backdrop ?? Backdrop;
        Cursor = other.Cursor ?? Cursor;
        Hidden = other.Hidden ?? Hidden;

        Display = other.Display ?? Display;
        FlexDirection = other.FlexDirection ?? FlexDirection;
        FlexWrap = other.FlexWrap ?? FlexWrap;
        GapX = other.GapX ?? GapX;
        GapY = other.GapY ?? GapY;
        JustifyContent = other.JustifyContent ?? JustifyContent;
        AlignItems = other.AlignItems ?? AlignItems;
        GridColumns = other.GridColumns ?? GridColumns;
        GridRows = other.GridRows ?? GridRows;

        FlexGrow = other.FlexGrow ?? FlexGrow;
        FlexShrink = other.FlexShrink ?? FlexShrink;
        FlexBasis = other.FlexBasis ?? FlexBasis;
        AlignSelf = other.AlignSelf ?? AlignSelf;
        ColumnSpan = other.ColumnSpan ?? ColumnSpan;
        RowSpan = other.RowSpan ?? RowSpan;

        DivideX = other.DivideX ?? DivideX;
        DivideY = other.DivideY ?? DivideY;
        DivideColor = other.DivideColor ?? DivideColor;
    }
}

/// <summary>Windows 11 sistem zemini (DWMWA_SYSTEMBACKDROP_TYPE).</summary>
public enum TwBackdrop
{
    /// <summary>Sistem varsayılanı.</summary>
    Auto = 0,
    None = 1,
    Mica = 2,
    Acrylic = 3,
    /// <summary>Sekmeli pencereler için Mica Alt.</summary>
    Tabbed = 4,
}
