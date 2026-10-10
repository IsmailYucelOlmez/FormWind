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

        ApplyColors(control, style, original, applied, TransitionDuration(control, style, context), context.DesignMode);
        ApplyFont(control, style, original, applied, context.Theme);
        ApplySpacing(control, style, original, applied, context.Scale);
        ApplySize(control, style, context.Scale);
        FitPadding(control, applied);
        ApplyTextAlign(control, style);
        ApplyCursor(control, style, original, applied);
        if (!context.DesignMode) ApplyHidden(control, style, applied);
        ApplyFlatButton(control, style, original, applied);
        ApplyChrome(control, style, context, applied);
        if (control is ITwToggle toggle) toggle.SetToggleColors(style.AccentColor, style.FillColor);
        if (!context.DesignMode && TwNativeTheme.Supports(control))
        {
            bool dark = context.DarkScrollbars && (context.State & TwState.Dark) != 0;
            if (dark || TwNativeTheme.IsDark(control)) TwNativeTheme.Apply(control, dark);
        }
        if (control is TwTextBox textBox)
            textBox.PlaceholderColor = context.Parsed.ResolvePart("placeholder", context.State & ~(TwState.Hover | TwState.Active)).ForeColor;
        if (control is TwPictureBox picture && style.ObjectFit is TwImageFit fit) picture.Fit = fit;
        if (control is TwTabStrip tabs) ApplyTabColors(tabs, style, context);
        if (control is TwDatePicker picker) picker.CalendarColors = CalendarColors(style, context);
        if (control is TwCalendar calendar) calendar.Colors = CalendarColors(style, context);
        if (control is TwDataList list) ApplyDataListColors(list, style, context);
        if (control is TwSlider slider) ApplySliderColors(slider, style, context);
        if (control is TwField field) ApplyFieldParts(field, context);
        if (control is TwPager pager)
        {
            // Geçerli sayfa selected:, hover item:hover:.
            var pagerState = context.State & ~(TwState.Hover | TwState.Active | TwState.Focus | TwState.FocusVisible);
            var selected = context.Parsed.ResolvePart("selected", pagerState);
            var hover = context.Parsed.ResolvePart("item", pagerState | TwState.Hover);
            pager.SelectedBackColor = selected.BackColor ?? style.AccentColor ?? ColorTranslator.FromHtml("#2563eb");
            pager.SelectedForeColor = selected.ForeColor ?? Color.White;
            pager.HoverBackColor = hover.BackColor ?? ColorTranslator.FromHtml("#f3f4f6");
            pager.Invalidate();
        }
        if (control is TwProgress progress)
        {
            progress.TrackColor = style.FillColor ?? ColorTranslator.FromHtml("#e5e7eb");
            progress.BarColor = style.AccentColor ?? ColorTranslator.FromHtml("#2563eb");
            progress.Invalidate();
        }
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

    // Takvim: pencere menu:, seçili gün selected:, hover item:hover:, bugün halkası accent-*; metin rengi alanınkinden.
    private static TwCalendarColors CalendarColors(TwStyle style, TwApplyContext context)
    {
        var state = context.State & ~(TwState.Hover | TwState.Active | TwState.Focus);
        var menu = context.Parsed.ResolvePart("menu", state);
        var selected = context.Parsed.ResolvePart("selected", state);
        var hover = context.Parsed.ResolvePart("item", state | TwState.Hover);
        var colors = new TwCalendarColors();
        if ((menu.BackColor ?? style.BackColor) is Color back) colors.Back = back;
        if ((menu.BorderColor ?? style.BorderColor) is Color border) colors.Border = border;
        if ((menu.ForeColor ?? style.ForeColor) is Color fore) colors.Fore = fore;
        if (menu.ForeColor is null && style.ForeColor is Color f) colors.Muted = Color.FromArgb(140, f);
        if ((selected.BackColor ?? style.AccentColor) is Color accent) colors.Accent = accent;
        if (selected.ForeColor is Color accentFore) colors.AccentFore = accentFore;
        if (hover.BackColor is Color hoverBack) colors.Hover = hoverBack;
        return colors;
    }

    // Form alanı: etiket label:, ipucu hint:, hata error: (renk ve font).
    private static void ApplyFieldParts(TwField field, TwApplyContext context)
    {
        var state = context.State & ~(TwState.Hover | TwState.Active | TwState.Focus | TwState.FocusVisible);
        var label = context.Parsed.ResolvePart("label", state);
        var hint = context.Parsed.ResolvePart("hint", state);
        var error = context.Parsed.ResolvePart("error", state);
        float scale = TwDpi.FontScale(field);
        var captionFont = label.HasFont ? TwFonts.Create(label, field.Font, context.Theme, scale) : null;
        var messageStyle = error.HasFont ? error : hint;
        var messageFont = messageStyle.HasFont ? TwFonts.Create(messageStyle, field.Font, context.Theme, scale) : null;
        field.SetPartStyles(label.ForeColor, captionFont, hint.ForeColor, error.ForeColor, messageFont);
    }

    // Kaydırıcı: ray fill-*, dolu kısım accent-*, tutamaç item:bg-* (varsayılan beyaz).
    private static void ApplySliderColors(TwSlider slider, TwStyle style, TwApplyContext context)
    {
        var item = context.Parsed.ResolvePart("item", context.State & ~(TwState.Hover | TwState.Active | TwState.Focus | TwState.FocusVisible));
        slider.TrackColor = style.FillColor ?? ColorTranslator.FromHtml("#e5e7eb");
        slider.AccentColor = style.AccentColor ?? ColorTranslator.FromHtml("#2563eb");
        slider.ThumbColor = item.BackColor ?? Color.White;
        slider.Invalidate();
    }

    // Veri listesi: satırlar item: (hover, rounded), seçili satır selected:, ayırıcı row:border-*, kaydırma çubuğu fill-*.
    private static void ApplyDataListColors(TwDataList list, TwStyle style, TwApplyContext context)
    {
        var state = context.State & ~(TwState.Hover | TwState.Active | TwState.Focus);
        var item = context.Parsed.ResolvePart("item", state);
        var hover = context.Parsed.ResolvePart("item", state | TwState.Hover);
        var selected = context.Parsed.ResolvePart("selected", state);
        var row = context.Parsed.ResolvePart("row", state);
        var colors = new TwDataListColors();
        if (style.BackColor is Color back) colors.Back = back;
        if (style.ForeColor is Color fore)
        {
            colors.Fore = fore;
            // İkincil metin: ön ve arka plan rengin ortası (item:text-* ile değiştirilebilir).
            colors.Muted = Color.FromArgb((fore.R + colors.Back.R) / 2, (fore.G + colors.Back.G) / 2, (fore.B + colors.Back.B) / 2);
        }
        if (item.ForeColor is Color muted) colors.Muted = muted;
        if (hover.BackColor is Color hoverBack) colors.Hover = hoverBack;
        if ((selected.BackColor ?? style.AccentColor) is Color selectedBack) colors.Selected = selectedBack;
        if (selected.ForeColor is Color selectedFore) colors.SelectedFore = selectedFore;
        else if (style.ForeColor is Color f) colors.SelectedFore = f;
        if (row.BorderColor is Color divider) colors.Divider = divider;
        if (style.FillColor is Color thumb) colors.Thumb = thumb;
        // Grup başlıkları header:, iskelet satırlar zemin ile metin renginin %12 karışımı.
        var header = context.Parsed.ResolvePart("header", state);
        if (header.BackColor is Color groupBack) colors.GroupBack = groupBack;
        colors.GroupFore = header.ForeColor ?? colors.Muted;
        colors.Skeleton = Color.FromArgb(
            (colors.Fore.R * 12 + colors.Back.R * 88) / 100, (colors.Fore.G * 12 + colors.Back.G * 88) / 100, (colors.Fore.B * 12 + colors.Back.B * 88) / 100);
        list.Colors = colors;
        if (item.Radius is int radius) list.ItemRadius = radius;
    }

    private static void ApplyTabColors(TwTabStrip tabs, TwStyle style, TwApplyContext context)
    {
        var state = context.State & ~(TwState.Hover | TwState.Active);
        var selected = context.Parsed.ResolvePart("selected", state);
        var hover = context.Parsed.ResolvePart("item", state | TwState.Hover);
        var colors = new TwTabColors();
        if (style.ForeColor is Color fore) colors.Fore = fore;
        if (selected.ForeColor is Color activeFore) colors.ActiveFore = activeFore;
        if (selected.BackColor is Color activeBack) colors.ActiveBack = activeBack;
        if ((selected.BorderColor ?? style.AccentColor) is Color accent) colors.Accent = accent;
        if (selected.FontWeight is int weight) colors.ActiveWeight = weight;
        colors.HoverFore = hover.ForeColor ?? colors.ActiveFore;
        if (hover.BackColor is Color hoverBack) colors.HoverBack = hoverBack;
        if (style.FillColor is Color track) colors.Track = track;
        tabs.Colors = colors;
        tabs.Gap = style.GapX is int gap ? TwSides.Scale(gap, context.Scale) : -1;
    }

    // transition yalnızca çalışma zamanında ve görünen (handle'ı oluşmuş) kontrollerde anlamlıdır.
    private static int TransitionDuration(Control control, TwStyle style, TwApplyContext context) =>
        style.Transition == true && !context.DesignMode && control.IsHandleCreated ? style.DurationMs ?? 150 : 0;

    private static void ApplyColors(Control control, TwStyle style, TwOriginal original, Applied applied, int duration, bool designMode)
    {
        ApplyColor(control, TwColorChannel.Back, style.BackColor, original.BackColor, applied.Back, duration, designMode);
        ApplyColor(control, TwColorChannel.Fore, style.ForeColor, original.ForeColor, applied.Fore, duration, designMode);
    }

    /// <summary>Kanalın rengini uygulama kodu değiştirdiyse (örn. durum rengi) class'lar o kanala bir daha dokunmaz.</summary>
    internal static bool IsColorOverridden(Control control, bool back) =>
        AppliedTable.TryGetValue(control, out var applied) && (back ? applied.Back : applied.Fore).Overridden;

    /// <summary>Class'lar yeniden atanınca renk kanalları yeniden FormWind'e geçer.</summary>
    internal static void ResetColorOverrides(Control control)
    {
        if (!AppliedTable.TryGetValue(control, out var applied)) return;
        applied.Back.Rebase(control, back: true);
        applied.Fore.Rebase(control, back: false);
    }

    // Renk kanalı: FormWind'in kontrolde bıraktığı değer izlenir. Bir sonraki uygulamada değer farklıysa (ya da
    // kalıtıma bırakılmışken açıkça atanmışsa) uygulama kodu değiştirmiştir; kanal uygulamaya geçer ve hover:/dark:
    // varyantları bu rengi ezmez. Ebeveynden kalıtılan renk değişimleri değişiklik sayılmaz.
    private static void ApplyColor(Control control, TwColorChannel channel, Color? target, Color? original, ChannelState state, int duration, bool designMode)
    {
        bool back = channel == TwColorChannel.Back;
        string property = back ? nameof(Control.BackColor) : nameof(Control.ForeColor);
        if (state.Overridden) return;
        // İzleme class'ların atandığı andaki özgün değerle başlar: FormWind kanala henüz dokunmamışken (örn. yalnızca
        // dark:hover: varyantı varken) uygulamanın yaptığı atama da değişiklik sayılır.
        if (!state.Tracked && !designMode)
        {
            state.Tracked = true;
            state.Left = original;
        }
        if (!designMode && state.Tracked && !TwAnimator.IsAnimating(control, channel))
        {
            bool changed = state.Left is Color left
                ? Current(control, back).ToArgb() != left.ToArgb()
                : TwOriginal.IsExplicit(control, property);
            if (changed)
            {
                state.Overridden = true;
                state.Applied = false;
                return;
            }
        }

        if (target is Color color)
        {
            SetColor(control, channel, color, duration, () => Remember(control, back, state, inherited: false));
            state.Applied = true;
        }
        else if (state.Applied)
        {
            if (original is Color restore)
                SetColor(control, channel, restore, duration, () => Remember(control, back, state, inherited: false));
            else if (duration > 0)
                SetColor(control, channel, control.Parent is Control parent ? Current(parent, back) : back ? Control.DefaultBackColor : Control.DefaultForeColor, duration, () =>
                {
                    if (back) control.ResetBackColor();
                    else control.ResetForeColor();
                    Remember(control, back, state, inherited: true);
                });
            else
            {
                if (back) control.ResetBackColor();
                else control.ResetForeColor();
                Remember(control, back, state, inherited: true);
            }
            state.Applied = false;
        }
    }

    // hidden/visible: FormWind'in gizlediği kontrol, class kaldırılınca yeniden görünür olur.
    private static void ApplyHidden(Control control, TwStyle style, Applied applied)
    {
        if (style.Hidden is bool hidden)
        {
            control.Visible = !hidden;
            applied.Hidden = hidden;
        }
        else if (applied.Hidden)
        {
            control.Visible = true;
            applied.Hidden = false;
        }
    }

    private static Color Current(Control control, bool back) => back ? control.BackColor : control.ForeColor;

    // Hedef değil kontrolün gerçekte aldığı değer saklanır: saydamlığı desteklemeyen kontrol atamayı reddedebilir.
    private static void Remember(Control control, bool back, ChannelState state, bool inherited)
    {
        state.Tracked = true;
        // Atama reddedildiyse ya da yapılmadıysa değer kalıtılmıştır; o zaman ebeveynden gelen değişim değişiklik sayılmaz.
        bool explicitValue = !inherited && TwOriginal.IsExplicit(control, back ? nameof(Control.BackColor) : nameof(Control.ForeColor));
        state.Left = explicitValue ? Current(control, back) : null;
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
        // Kalıtılan renk hedefe eşit olsa da atanır: aksi halde ebeveynin rengi değişince class rengi de değişirdi.
        else if (control.ForeColor.ToArgb() != target.ToArgb() || !TwOriginal.IsExplicit(control, nameof(Control.ForeColor))) control.ForeColor = target;
        completed?.Invoke();
    }

    private static void SetBackColor(Control control, Color color)
    {
        if (control.BackColor.ToArgb() == color.ToArgb() && TwOriginal.IsExplicit(control, nameof(Control.BackColor))) return;
        try
        {
            control.BackColor = color;
        }
        catch (ArgumentException)
        {
            // Kontrol saydam arka planı desteklemiyor (örn. TextBox + bg-transparent).
        }
    }

    internal static Font ScaleFont(Font font, float factor) =>
        System.Math.Abs(factor - 1f) < 0.01f ? font : TwFonts.Get(font.FontFamily.Name, font.SizeInPoints * factor, font.Style);

    private static void ApplyFont(Control control, TwStyle style, TwOriginal original, Applied applied, TwTheme theme)
    {
        if (style.HasFont)
        {
            // Açıkça atanmış özgün font, yakalandığı DPI'dan bu ekranın DPI'ına ölçeklenir (PerMonitorV2'de ekran değişince);
            // ebeveynin fontu zaten bu ekrana göredir.
            var baseFont = original.Font is Font own ? ScaleFont(own, TwDpi.DeviceDpi(control) / (float)original.FontDpi)
                : control.Parent?.Font ?? Control.DefaultFont;
            var font = TwFonts.Create(style, baseFont, theme, TwDpi.FontScale(control));
            if (!control.Font.Equals(font)) control.Font = font;
            applied.Font = true;
        }
        else if (applied.Font)
        {
            if (original.Font is Font restore) control.Font = ScaleFont(restore, TwDpi.DeviceDpi(control) / (float)original.FontDpi);
            else control.ResetFont();
            applied.Font = false;
        }
    }

    private static void ApplySpacing(Control control, TwStyle style, TwOriginal original, Applied applied, float scale)
    {
        // DesiredPadding atamadan önce güncellenir: AutoSize kontrolde Padding ataması boyutu hemen değiştirir ve
        // SizeChanged'deki yeniden sığdırma eski değeri geri yazmasın.
        if (!style.Padding.IsEmpty)
        {
            var padding = style.Padding.ApplyTo(original.Padding, scale);
            applied.Padding = true;
            applied.DesiredPadding = padding;
            if (control.Padding != padding) control.Padding = padding;
        }
        else if (applied.Padding)
        {
            applied.Padding = false;
            applied.DesiredPadding = null;
            control.Padding = original.Padding;
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

    // CSS'te padding kutuyu büyütür; AutoSize kapalı bir buton ya da etikette ise içeriği daraltır. Kalan yükseklik
    // bir satıra yetmezse WinForms yazıyı hiç çizmez (örn. 25 px'lik butona py-2), genişlik yetmezse son harfleri alt
    // satıra kaydırıp kırpar. Padding yazı tek satıra sığacak kadar kısılır; kontrol büyüyünce istenen değere döner.
    private static void FitPadding(Control control, Applied applied)
    {
        if (applied.DesiredPadding is not Padding desired || !(control is ButtonBase || control is Label)) return;

        var fitted = FitToText(control, desired);
        if (control.Padding != fitted) control.Padding = fitted;

        if (applied.FitHooked) return;
        applied.FitHooked = true;
        EventHandler refit = (_, _) =>
        {
            if (applied.DesiredPadding is Padding current)
            {
                var value = FitToText(control, current);
                if (control.Padding != value) control.Padding = value;
            }
        };
        control.SizeChanged += refit;
        control.FontChanged += refit;
        control.TextChanged += refit;
    }

    internal static Padding FitToText(Control control, Padding desired)
    {
        if (control.AutoSize || string.IsNullOrEmpty(control.Text)) return desired;

        var text = TextRenderer.MeasureText(control.Text, control.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine);
        // Buton yazıyı kenarlık ve odak çerçevesi kadar içeriden çizer (flat butonda her eksende ~8 px).
        bool button = control is ButtonBase;
        int roomX = control.ClientSize.Width - text.Width - (button ? 8 : 0);
        int roomY = control.ClientSize.Height - Math.Max(text.Height, control.Font.Height) - (button ? 8 : 0);
        var (left, right) = Shrink(desired.Left, desired.Right, roomX);
        var (top, bottom) = Shrink(desired.Top, desired.Bottom, roomY);
        return new Padding(left, top, right, bottom);
    }

    // İki kenarın toplamı room'u aşıyorsa oranlarını koruyarak room'a indirir.
    private static (int First, int Second) Shrink(int first, int second, int room)
    {
        int total = first + second;
        if (total <= room || total == 0) return (first, second);
        room = Math.Max(0, room);
        int shrunk = first * room / total;
        return (shrunk, room - shrunk);
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

    private static void ApplyChrome(Control control, TwStyle style, TwApplyContext context, Applied applied)
    {
        var chrome = TwChrome.FromStyle(style, context.Scale, context.Theme.DefaultBorderColor);
        var outset = TwOutsetSpec.FromStyle(style, context.Scale);

        if (control is ITwChrome owner)
        {
            owner.SetChrome(chrome);
        }
        else if ((style.HasChrome || style.HasGradient || TwDecorator.IsAttached(control)) && SupportsDecoration(control))
        {
            TwDecorator.For(control).Update(chrome);
        }

        // Gölge ve halka ebeveyn tarafından çizilir; native kontrollerde de çalışır (TextBox'ın focus halkası gibi).
        // TextBox kendi kenarlığını çizer ve Paint olayı çalışmaz: native kenarlık kapatılır, class'ların kenarlığı ve
        // köşesi ebeveynde, kontrolün etrafına (zemin rengiyle doldurulmuş bir çerçeve olarak) çizilir.
        if (control is TextBoxBase textBox && control is not ITwChrome)
        {
            if (chrome.HasBorder)
            {
                applied.NativeBorder ??= textBox.BorderStyle;
                if (textBox.BorderStyle != BorderStyle.None) textBox.BorderStyle = BorderStyle.None;
                outset = outset.WithFrame(chrome, FrameGap * context.Scale);
            }
            else if (applied.NativeBorder is BorderStyle restore)
            {
                textBox.BorderStyle = restore;
                applied.NativeBorder = null;
            }
        }

        // DropDownList ComboBox'ın kapalı kutusu tümüyle FormWind tarafından çizilir (kenarlık, köşe, ok).
        if (control is ComboBox combo)
        {
            if (TwComboPainter.Supports(combo) && (chrome.HasBorder || chrome.Radius > 0)) TwComboPainter.Attach(combo, chrome);
            else TwComboPainter.Detach(combo);
        }

        if (style.HasOutset || !outset.IsEmpty || TwOutset.IsAttached(control))
            TwOutset.For(control).Update(outset);
    }

    /// <summary>Native TextBox çerçevesi ile metin arasındaki boşluk (96 DPI pikseli).</summary>
    internal const float FrameGap = 3f;

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
        public readonly ChannelState Back = new ChannelState();
        public readonly ChannelState Fore = new ChannelState();
        public bool Font;
        public bool Padding;
        public bool Margin;
        public bool Cursor;
        public bool FlatButton;
        /// <summary>Çerçeve çizilen TextBox'ın özgün kenarlığı; <c>null</c> = çerçeve yok.</summary>
        public BorderStyle? NativeBorder;
        public bool Hidden;
        public Padding? DesiredPadding;
        public bool FitHooked;
    }

    private sealed class ChannelState
    {
        /// <summary>Renk class'lardan geliyor.</summary>
        public bool Applied;
        /// <summary>FormWind bu kanala en az bir kez dokundu; <see cref="Left"/> geçerli.</summary>
        public bool Tracked;
        /// <summary>FormWind'in bıraktığı açık değer; <c>null</c> = kalıtıma bırakıldı.</summary>
        public Color? Left;
        /// <summary>Uygulama kodu rengi değiştirdi; class'lar bu kanala dokunmaz.</summary>
        public bool Overridden;

        // Kanal yeniden FormWind'e geçer; kontrolün o anki değeri (uygulamanın ya da FormWind'in atadığı) yeni başlangıçtır,
        // yoksa bir sonraki uygulamada değişiklik sayılırdı.
        public void Rebase(Control control, bool back)
        {
            Overridden = false;
            Tracked = true;
            Left = TwOriginal.IsExplicit(control, back ? nameof(Control.BackColor) : nameof(Control.ForeColor))
                ? (back ? control.BackColor : control.ForeColor)
                : null;
        }
    }
}
