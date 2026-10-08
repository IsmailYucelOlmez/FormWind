using System.Drawing;
using System.Windows.Forms;
using Krypton.Toolkit;

namespace FormWind.Krypton;

/// <summary>
/// Varyantları Krypton'un durum paletlerine eşler: temel → StateCommon, hover → StateTracking,
/// active → StatePressed, focus → OverrideFocus, disabled → StateDisabled. Hover ve basılı durumların
/// çizimini Krypton kendisi yönetir; köşeler antialias çizilir.
/// </summary>
public sealed class KryptonApplier : IStyleApplier
{
    /// <summary>Uygulayıcıyı kaydeder. <c>Program.Main</c> içinde, formlar açılmadan önce çağrılmalı.</summary>
    public static void Register() => TwAppliers.Register(new KryptonApplier());

    public bool CanApply(Control control) =>
        control is KryptonButton || control is KryptonPanel || control is KryptonTextBox || control is KryptonLabel;

    public void Apply(Control control, TwStyle style, TwApplyContext context)
    {
        var parsed = context.Parsed;
        var dark = (context.State & TwState.Dark) != 0 ? TwState.Dark : TwState.None;

        var common = parsed.ResolveExact(TwState.None, dark);
        var tracking = parsed.ResolveExact(TwState.Hover, dark | TwState.Hover);
        var pressed = Merge(tracking, parsed.ResolveExact(TwState.Active, dark | TwState.Hover | TwState.Active));
        var focus = parsed.ResolveExact(TwState.Focus, dark | TwState.Focus);
        var disabled = parsed.ResolveExact(TwState.Disabled, dark | TwState.Disabled);

        var scale = context.Scale;
        var theme = context.Theme;

        switch (control)
        {
            case KryptonButton button:
                ApplyTriple(button.StateCommon.Back, button.StateCommon.Border, button.StateCommon.Content, common, scale, theme);
                ApplyTriple(button.StateTracking.Back, button.StateTracking.Border, button.StateTracking.Content, tracking, scale, theme);
                ApplyTriple(button.StatePressed.Back, button.StatePressed.Border, button.StatePressed.Content, pressed, scale, theme);
                ApplyTriple(button.OverrideFocus.Back, button.OverrideFocus.Border, button.OverrideFocus.Content, focus, scale, theme);
                ApplyTriple(button.StateDisabled.Back, button.StateDisabled.Border, button.StateDisabled.Content, disabled, scale, theme);
                break;

            case KryptonPanel panel:
                ApplyBack(panel.StateCommon, common);
                ApplyBack(panel.StateDisabled, disabled);
                break;

            case KryptonTextBox textBox:
                var active = Merge(tracking, focus);
                ApplyInput(textBox.StateCommon.Back, textBox.StateCommon.Border, textBox.StateCommon.Content, common, scale, theme);
                ApplyInput(textBox.StateActive.Back, textBox.StateActive.Border, textBox.StateActive.Content, active, scale, theme);
                ApplyInput(textBox.StateDisabled.Back, textBox.StateDisabled.Border, textBox.StateDisabled.Content, disabled, scale, theme);
                break;

            case KryptonLabel label:
                ApplyContent(label.StateCommon, common, scale, theme);
                ApplyContent(label.StateDisabled, disabled, scale, theme);
                break;
        }

        // Krypton kontrolleri de flex/grid konteynerlerinin öğesi olabilir.
        TwLayout.Apply(control, style, scale);
    }

    private static TwStyle Merge(TwStyle first, TwStyle second)
    {
        var merged = new TwStyle();
        merged.MergeFrom(first);
        merged.MergeFrom(second);
        return merged;
    }

    private static void ApplyTriple(PaletteBack back, PaletteBorder border, PaletteContent content, TwStyle style, float scale, TwTheme theme)
    {
        ApplyBack(back, style);
        ApplyBorder(border, style, scale, theme);
        ApplyContent(content, style, scale, theme);
    }

    // Giriş kontrolleri (KryptonTextBox) ayrı, daha dar palet tipleri kullanır.
    private static void ApplyInput(PaletteInputControlBackStates back, PaletteBorder border, PaletteInputControlContentStates content, TwStyle style, float scale, TwTheme theme)
    {
        back.Color1 = style.BackColor ?? Color.Empty;
        ApplyBorder(border, style, scale, theme);
        content.Color1 = style.ForeColor ?? Color.Empty;
        content.Font = CreateFont(style, theme);
        content.TextH = ToRelativeAlign(style.TextAlign);
        content.Padding = style.Padding.IsEmpty ? new Padding(-1) : style.Padding.ApplyTo(new Padding(-1), scale);
    }

    private static void ApplyBack(PaletteBack back, TwStyle style)
    {
        if (style.BackColor is Color color)
        {
            back.Color1 = color;
            back.ColorStyle = PaletteColorStyle.Solid;
        }
        else
        {
            back.Color1 = Color.Empty;
            back.ColorStyle = PaletteColorStyle.Inherit;
        }
    }

    private static void ApplyBorder(PaletteBorder border, TwStyle style, float scale, TwTheme theme)
    {
        if (style.BorderWidth is int width)
        {
            border.Width = width == 0 ? 0 : TwSides.Scale(width, scale);
            border.DrawBorders = width > 0 ? PaletteDrawBorders.All : PaletteDrawBorders.None;
            border.Color1 = style.BorderColor ?? theme.DefaultBorderColor;
        }
        else
        {
            border.Width = -1;
            border.DrawBorders = PaletteDrawBorders.Inherit;
            border.Color1 = style.BorderColor ?? Color.Empty;
        }

        border.Rounding = style.Radius is int radius ? radius * scale : -1;
    }

    private static void ApplyContent(PaletteContent content, TwStyle style, float scale, TwTheme theme)
    {
        content.ShortText.Color1 = style.ForeColor ?? Color.Empty;
        content.ShortText.Font = CreateFont(style, theme);
        content.ShortText.TextH = ToRelativeAlign(style.TextAlign);
        content.Padding = style.Padding.IsEmpty ? new Padding(-1) : style.Padding.ApplyTo(new Padding(-1), scale);
    }

    // Krypton'da null font = paletten miras. Boyut verilmezse Krypton'un varsayılanı olan 9 pt kullanılır.
    private static Font? CreateFont(TwStyle style, TwTheme theme)
    {
        if (!style.HasFont) return null;
        return TwFonts.Create(style, TwFonts.Get(theme.FontFamilySans, 9f, FontStyle.Regular), theme);
    }

    private static PaletteRelativeAlign ToRelativeAlign(TwTextAlign? align) => align switch
    {
        TwTextAlign.Left => PaletteRelativeAlign.Near,
        TwTextAlign.Center => PaletteRelativeAlign.Center,
        TwTextAlign.Right => PaletteRelativeAlign.Far,
        _ => PaletteRelativeAlign.Inherit,
    };
}
