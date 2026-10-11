using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// Tarih seçici: <see cref="TwTextBox"/> tarzı alan ve sağda takvim ikonu; tıklanınca (ya da Alt+↓ / F4) açılır pencerede
/// <see cref="TwCalendar"/> gösterir. Tarih elle de yazılabilir; odak kaybında ya da Enter'da <see cref="Culture"/>'a göre çözülür.
/// Alan <c>input</c> gibi class'larla; takvim <c>menu:bg-* menu:border-*</c> (pencere), <c>selected:bg-* selected:text-*</c>
/// (seçili gün), <c>item:hover:bg-*</c> (hover), <c>accent-*</c> (bugün halkası) ile stillenir.
/// </summary>
[ToolboxItem(true)]
[DefaultEvent(nameof(ValueChanged))]
[DefaultProperty(nameof(Value))]
[Description("Stylable date picker.")]
public class TwDatePicker : TwTextBox
{
    private DateTime? _value;
    private string _format = "d";
    private CultureInfo _culture = CultureInfo.CurrentCulture;
    private TwCalendarColors _calendarColors = new TwCalendarColors();
    private ToolStripDropDown? _dropDown;
    private TwCalendar? _calendar;

    public TwDatePicker()
    {
        InnerTextBox.KeyDown += OnInnerKeyDown;
        InnerTextBox.Leave += (_, _) => Commit();
        PlaceholderText = "gg.aa.yyyy";
    }

    public event EventHandler? ValueChanged;

    [Category("Data"), DefaultValue(null)]
    public DateTime? Value
    {
        get => _value;
        set
        {
            var date = value?.Date;
            if (date != null && (date < MinDate.Date || date > MaxDate.Date)) return;
            bool changed = date != _value;
            _value = date;
            UpdateText();
            if (changed) ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Gösterim biçimi (<see cref="DateTime.ToString(string, IFormatProvider)"/>); varsayılan kısa tarih.</summary>
    [Category("Appearance"), DefaultValue("d")]
    public string Format
    {
        get => _format;
        set
        {
            _format = string.IsNullOrEmpty(value) ? "d" : value;
            UpdateText();
        }
    }

    [Category("Behavior")]
    public DateTime MinDate { get; set; } = DateTime.MinValue;

    [Category("Behavior")]
    public DateTime MaxDate { get; set; } = DateTime.MaxValue;

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CultureInfo Culture
    {
        get => _culture;
        set
        {
            _culture = value ?? CultureInfo.CurrentCulture;
            UpdateText();
        }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal TwCalendarColors CalendarColors
    {
        get => _calendarColors;
        set => _calendarColors = value ?? new TwCalendarColors();
    }

    [Browsable(false)]
    public bool DroppedDown => _dropDown?.Visible == true;

    /// <summary>Açık takvim (kapalıyken <c>null</c>).</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TwCalendar? Calendar => DroppedDown ? _calendar : null;

    private int IconWidth => (int)Math.Round(30 * TwDpi.DeviceDpi(this) / 96f);

    public override Rectangle DisplayRectangle
    {
        get
        {
            var rect = base.DisplayRectangle;
            return new Rectangle(rect.X, rect.Y, Math.Max(0, rect.Width - IconWidth), rect.Height);
        }
    }

    public void ShowCalendar()
    {
        Commit();
        _calendar ??= new TwCalendar();
        _calendar.Font = Font;
        _calendar.Culture = _culture;
        _calendar.Colors = _calendarColors;
        _calendar.MinDate = MinDate;
        _calendar.MaxDate = MaxDate;
        _calendar.Size = _calendar.GetPreferredSize(Size.Empty);
        _calendar.SelectedDate = _value;
        if (_value is null) _calendar.DisplayMonth = DateTime.Today;

        if (_dropDown is null)
        {
            _dropDown = new ToolStripDropDown { Padding = Padding.Empty, AutoClose = true, DropShadowEnabled = true };
            _dropDown.Items.Add(new ToolStripControlHost(_calendar) { Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = false, Size = _calendar.Size });
            _calendar.DateSelected += (_, _) =>
            {
                Value = _calendar.SelectedDate;
                _dropDown.Close();
                InnerTextBox.Focus();
            };
            _calendar.Cancelled += (_, _) => _dropDown.Close();
        }
        else
        {
            _dropDown.Items[0].Size = _calendar.Size;
        }

        _dropDown.Show(this, new Point(0, Height + (int)Math.Round(4 * TwDpi.DeviceDpi(this) / 96f)));
        _calendar.Focus();
    }

    /// <summary>Metni tarihe çevirir; geçersiz ya da boşsa önceki değer geri yazılır (boş metin değeri temizler).</summary>
    public void Commit()
    {
        var text = InnerTextBox.Text.Trim();
        if (text.Length == 0)
        {
            Value = null;
            return;
        }
        if (DateTime.TryParseExact(text, _format, _culture, DateTimeStyles.None, out var exact)) Value = exact;
        else if (DateTime.TryParse(text, _culture, DateTimeStyles.None, out var parsed)) Value = parsed;
        else UpdateText();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var icon = IconBounds();
        {
            var color = Enabled ? ForeColor : Color.FromArgb(128, ForeColor);
            TwIconRenderer.DrawIcon(e.Graphics, "calendar", TwIconRenderer.EmPixels(Font.SizeInPoints, TwDpi.DeviceDpi(this)), icon, color);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && IconBounds().Contains(e.Location))
        {
            ShowCalendar();
            return;
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Cursor = IconBounds().Contains(e.Location) ? Cursors.Hand : Cursors.IBeam;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dropDown?.Dispose();
            _calendar?.Dispose();
        }
        base.Dispose(disposing);
    }

    private Rectangle IconBounds()
    {
        var border = BorderInsets();
        return new Rectangle(Width - border.Right - IconWidth, border.Top, IconWidth, Math.Max(0, Height - border.Vertical));
    }

    private void OnInnerKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F4 || (e.Alt && e.KeyCode == Keys.Down))
        {
            ShowCalendar();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Enter)
        {
            Commit();
            e.SuppressKeyPress = true;
        }
    }

    private void UpdateText() => InnerTextBox.Text = _value?.ToString(_format, _culture) ?? "";
}
