using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// Standart WinForms kontrollerine stil uygular: renk, yazı, boşluk, boyut, hizalama, imleç;
/// yuvarlak köşe ve kenarlık için <see cref="ITwChrome"/> ya da Region + Paint kullanır.
/// </summary>
/// <remarks>
/// Bir property yalnızca stil onu tanımlıyorsa ya da daha önce bu uygulayıcı tarafından değiştirildiyse
/// ellenir. Böylece uygulama kodunun class'larla ilgisi olmayan atamaları korunur.
/// </remarks>
public class WinFormsApplier : IStyleApplier
{
    private static readonly ConditionalWeakTable<Control, Applied> AppliedTable = new ConditionalWeakTable<Control, Applied>();

    public virtual bool CanApply(Control control) => true;

    public virtual void Apply(Control control, TwStyle style, TwApplyContext context)
    {
        if (control is null) throw new ArgumentNullException(nameof(control));
        if (style is null) throw new ArgumentNullException(nameof(style));
        if (context is null) throw new ArgumentNullException(nameof(context));

        var applied = AppliedTable.GetValue(control, _ => new Applied());
        var original = context.Original;

        ApplyColors(control, style, original, applied, TransitionDuration(control, style, context));
        ApplyFont(control, style, original, applied, context.Theme);
        ApplySpacing(control, style, original, applied, context.Scale);
        ApplySize(control, style, context.Scale);
        ApplyTextAlign(control, style);
        ApplyCursor(control, style, original, applied);
        if (!context.DesignMode && style.Hidden is bool hidden)
            control.Visible = !hidden;
        ApplyFlatButton(control, style, original, applied);
        ApplyChrome(control, style, context);
        if (control is ITwToggle toggle) toggle.SetToggleColors(style.AccentColor, style.FillColor);
        if (control is TwTextBox textBox)
            textBox.PlaceholderColor = context.Parsed.ResolvePart("placeholder", context.State & ~(TwState.Hover | TwState.Active)).ForeColor;
        if (control is TwPictureBox picture && style.ObjectFit is TwImageFit fit) picture.Fit = fit;
        if (control is Label label && style.Truncate is bool truncate) label.AutoEllipsis = truncate;
        if (control is TwSelect select)
        {
            var colors = ToolStripApplier.BuildColors(style, context);
            colors.MenuBack = context.Parsed.ResolvePart("menu", context.State & ~(TwState.Hover | TwState.Active)).BackColor
                              ?? style.BackColor ?? colors.MenuBack;
            colors.BottomBorder = null; // kutunun kenarlığı menüye taşınmaz
            select.MenuColors = colors;
        }
        TwLayout.Apply(control, style, context.Scale);
    }

    // transition yalnızca çalışma zamanında ve görünen (handle'ı oluşmuş) kontrollerde anlamlıdır.
    private static int TransitionDuration(Control control, TwStyle style, TwApplyContext context) =>
        style.Transition == true && !context.DesignMode && control.IsHandleCreated ? style.DurationMs ?? 150 : 0;

    private static void ApplyColors(Control control, TwStyle style, TwOriginal original, Applied applied, int duration)
    {
        if (style.BackColor is Color back)
        {
            SetColor(control, TwColorChannel.Back, back, duration, null);
            applied.BackColor = true;
        }
        else if (applied.BackColor)
        {
            if (original.BackColor is Color restore) SetColor(control, TwColorChannel.Back, restore, duration, null);
            else if (duration > 0) SetColor(control, TwColorChannel.Back, control.Parent?.BackColor ?? Control.DefaultBackColor, duration, control.ResetBackColor);
            else control.ResetBackColor();
            applied.BackColor = false;
        }

        if (style.ForeColor is Color fore)
        {
            SetColor(control, TwColorChannel.Fore, fore, duration, null);
            applied.ForeColor = true;
        }
        else if (applied.ForeColor)
        {
            if (original.ForeColor is Color restore) SetColor(control, TwColorChannel.Fore, restore, duration, null);
            else if (duration > 0) SetColor(control, TwColorChannel.Fore, control.Parent?.ForeColor ?? Control.DefaultForeColor, duration, control.ResetForeColor);
            else control.ResetForeColor();
            applied.ForeColor = false;
        }
    }

    // Geçiş bitince miras değere dönmek için completed ile Reset çağrılır.
    private static void SetColor(Control control, TwColorChannel channel, Color target, int duration, Action? completed)
    {
        if (duration > 0)
        {
            TwAnimator.Animate(control, channel, target, duration, completed);
            return;
        }

        TwAnimator.Cancel(control, channel);
        if (channel == TwColorChannel.Back) SetBackColor(control, target);
        else if (control.ForeColor.ToArgb() != target.ToArgb()) control.ForeColor = target;
        completed?.Invoke();
    }

    private static void SetBackColor(Control control, Color color)
    {
        if (control.BackColor.ToArgb() == color.ToArgb()) return;
        try
        {
            control.BackColor = color;
        }
        catch (ArgumentException)
        {
            // Kontrol saydam arka planı desteklemiyor (örn. TextBox + bg-transparent).
        }
    }

    private static void ApplyFont(Control control, TwStyle style, TwOriginal original, Applied applied, TwTheme theme)
    {
        if (style.HasFont)
        {
            var baseFont = original.Font ?? control.Parent?.Font ?? Control.DefaultFont;
            var font = TwFonts.Create(style, baseFont, theme);
            if (!control.Font.Equals(font)) control.Font = font;
            applied.Font = true;
        }
        else if (applied.Font)
        {
            if (original.Font is Font restore) control.Font = restore;
            else control.ResetFont();
            applied.Font = false;
        }
    }

    private static void ApplySpacing(Control control, TwStyle style, TwOriginal original, Applied applied, float scale)
    {
        if (!style.Padding.IsEmpty)
        {
            var padding = style.Padding.ApplyTo(original.Padding, scale);
            if (control.Padding != padding) control.Padding = padding;
            applied.Padding = true;
        }
        else if (applied.Padding)
        {
            control.Padding = original.Padding;
            applied.Padding = false;
        }

        if (!style.Margin.IsEmpty)
        {
            var margin = style.Margin.ApplyTo(original.Margin, scale);
            if (control.Margin != margin) control.Margin = margin;
            applied.Margin = true;
        }
        else if (applied.Margin)
        {
            control.Margin = original.Margin;
            applied.Margin = false;
        }
    }

    // Boyutlar geri alınmaz: layout (Anchor/Dock) kontrolü sonradan yeniden boyutlandırmış olabilir.
    // Oransal boyutlar (w-1/2, w-full) yalnızca flex/grid konteyneri içinde, yerleşim motoru tarafından uygulanır.
    private static void ApplySize(Control control, TwStyle style, float scale)
    {
        if (style.Width is { IsPixels: true } width)
        {
            var value = (int)Math.Round(width.Value * scale);
            if (control.Width != value) control.Width = value;
        }
        if (style.Height is { IsPixels: true } height)
        {
            var value = (int)Math.Round(height.Value * scale);
            if (control.Height != value) control.Height = value;
        }
    }

    private static void ApplyTextAlign(Control control, TwStyle style)
    {
        if (style.TextAlign is not TwTextAlign align) return;

        switch (control)
        {
            case ButtonBase button: button.TextAlign = ToContentAlignment(align, button.TextAlign); break;
            case Label label: label.TextAlign = ToContentAlignment(align, label.TextAlign); break;
            case TextBox textBox: textBox.TextAlign = ToHorizontalAlignment(align); break;
            case TwTextBox twTextBox: twTextBox.InnerTextBox.TextAlign = ToHorizontalAlignment(align); break;
        }
    }

    private static void ApplyCursor(Control control, TwStyle style, TwOriginal original, Applied applied)
    {
        if (style.Cursor is Cursor cursor)
        {
            if (control.Cursor != cursor) control.Cursor = cursor;
            applied.Cursor = true;
        }
        else if (applied.Cursor)
        {
            if (original.Cursor is Cursor restore) control.Cursor = restore;
            else control.ResetCursor();
            applied.Cursor = false;
        }
    }

    // Flat buton hover'da kendi rengini üretir; hesapladığımız renge sabitlenir.
    private static void ApplyFlatButton(Control control, TwStyle style, TwOriginal original, Applied applied)
    {
        if (control is not Button button || control is ITwChrome) return;

        if (style.BackColor.HasValue || style.HasChrome)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = button.BackColor;
            button.FlatAppearance.MouseDownBackColor = button.BackColor;
            applied.FlatButton = true;
        }
        else if (applied.FlatButton)
        {
            button.FlatStyle = original.FlatStyle ?? FlatStyle.Standard;
            button.FlatAppearance.BorderSize = original.FlatBorderSize ?? 1;
            button.FlatAppearance.MouseOverBackColor = Color.Empty;
            button.FlatAppearance.MouseDownBackColor = Color.Empty;
            applied.FlatButton = false;
        }
    }

    private static void ApplyChrome(Control control, TwStyle style, TwApplyContext context)
    {
        var chrome = TwChrome.FromStyle(style, context.Scale, context.Theme.DefaultBorderColor);

        if (control is ITwChrome owner)
        {
            owner.SetChrome(chrome);
        }
        else if ((style.HasChrome || style.HasGradient || TwDecorator.IsAttached(control)) && SupportsDecoration(control))
        {
            TwDecorator.For(control).Update(chrome);
        }

        // Gölge ve halka ebeveyn tarafından çizilir; native kontrollerde de çalışır (TextBox'ın focus halkası gibi).
        if (style.HasOutset || TwOutset.IsAttached(control))
            TwOutset.For(control).Update(TwOutsetSpec.FromStyle(style, context.Scale));
    }

    // Native çizilen kontrollerde Paint olayı çalışmaz; Form'da Region başlık çubuğunu keser.
    private static bool SupportsDecoration(Control control) =>
        !(control is Form
          || control is TextBoxBase
          || control is ComboBox
          || control is ListBox
          || control is DateTimePicker
          || control is UpDownBase
          || control is ListView
          || control is TreeView);

    private static ContentAlignment ToContentAlignment(TwTextAlign align, ContentAlignment current)
    {
        int row = ((int)current & 0x7) != 0 ? 1 : ((int)current & 0x70) != 0 ? 16 : 256;
        int column = align == TwTextAlign.Left ? 1 : align == TwTextAlign.Center ? 2 : 4;
        return (ContentAlignment)(row * column);
    }

    private static HorizontalAlignment ToHorizontalAlignment(TwTextAlign align) =>
        align == TwTextAlign.Left ? HorizontalAlignment.Left
        : align == TwTextAlign.Center ? HorizontalAlignment.Center
        : HorizontalAlignment.Right;

    private sealed class Applied
    {
        public bool BackColor;
        public bool ForeColor;
        public bool Font;
        public bool Padding;
        public bool Margin;
        public bool Cursor;
        public bool FlatButton;
    }
}
