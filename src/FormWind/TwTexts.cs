using System.Globalization;

namespace FormWind;

/// <summary>
/// Metinler: diyalog düğmeleri, doğrulama mesajları, grid menüleri, ekran okuyucu adları. Varsayılan dil
/// İngilizcedir; uygulamanın kullanıcı arayüzü dili Türkçeyse (<see cref="CultureInfo.CurrentUICulture"/>) Türkçe metinler
/// seçilir. Başka bir dil için <see cref="Current"/> değiştirilir:
/// <code>TwTexts.Current = new TwTexts { Ok = "OK", Cancel = "Abbrechen", Required = "Pflichtfeld." };</code>
/// Kontrollerin kendi metin özellikleri (örn. <see cref="TwField.RequiredMessage"/>) atanmışsa bunların önüne geçer.
/// </summary>
public sealed class TwTexts
{
    /// <summary>Kullanılan metinler. İlk değer uygulamanın kullanıcı arayüzü diline göre seçilir.</summary>
    public static TwTexts Current { get; set; } = ForCulture(CultureInfo.CurrentUICulture);

    /// <summary>İngilizce metinler (yeni bir örnek).</summary>
    public static TwTexts English => new TwTexts();

    /// <summary>Türkçe metinler (yeni bir örnek).</summary>
    public static TwTexts Turkish => new TwTexts
    {
        Ok = "Tamam",
        Cancel = "İptal",
        Yes = "Evet",
        No = "Hayır",
        Close = "Kapat",
        Required = "Bu alan zorunludur.",
        InvalidFormat = "Geçersiz biçim.",
        Incomplete = "Eksik giriş.",
        MinLength = "En az {0} karakter girin.",
        InvalidEmail = "Geçerli bir e-posta adresi girin.",
        TcKimlikLength = "T.C. kimlik numarası 11 haneli olmalı.",
        InvalidTcKimlik = "Geçersiz T.C. kimlik numarası.",
        InvalidIban = "Geçersiz IBAN.",
        TurkishIbanLength = "TR IBAN 26 karakter olmalı.",
        InvalidMobile = "Geçerli bir cep telefonu girin.",
        Search = "Ara…",
        Loading = "Yükleniyor…",
        SelectAll = "(Tümünü seç)",
        ClearFilter = "Süzmeyi kaldır",
        Blank = "(Boş)",
        Open = "Aç",
        Select = "Seç",
        Expand = "Genişlet",
        Collapse = "Daralt",
        PreviousMonth = "Önceki ay",
        NextMonth = "Sonraki ay",
        Item = "Öğe {0}",
        Page = "Sayfa {0} / {1}",
    };

    /// <summary>Dile göre metinler: Türkçe için <see cref="Turkish"/>, diğer diller için <see cref="English"/>.</summary>
    public static TwTexts ForCulture(CultureInfo culture) =>
        culture?.TwoLetterISOLanguageName == "tr" ? Turkish : English;

    // Diyalog ve bildirim düğmeleri (TwDialog, TwToast).
    public string Ok { get; set; } = "OK";
    public string Cancel { get; set; } = "Cancel";
    public string Yes { get; set; } = "Yes";
    public string No { get; set; } = "No";
    public string Close { get; set; } = "Close";

    // Doğrulama (TwField, TwValidators). {0} yer tutucuları string.Format ile doldurulur.
    public string Required { get; set; } = "This field is required.";
    public string InvalidFormat { get; set; } = "Invalid format.";
    public string Incomplete { get; set; } = "Incomplete entry.";
    /// <summary>{0}: en az karakter sayısı.</summary>
    public string MinLength { get; set; } = "Enter at least {0} characters.";
    public string InvalidEmail { get; set; } = "Enter a valid email address.";
    public string TcKimlikLength { get; set; } = "The Turkish ID number must have 11 digits.";
    public string InvalidTcKimlik { get; set; } = "Invalid Turkish ID number.";
    public string InvalidIban { get; set; } = "Invalid IBAN.";
    public string TurkishIbanLength { get; set; } = "A TR IBAN must have 26 characters.";
    public string InvalidMobile { get; set; } = "Enter a valid mobile number.";

    // Kontroller ve grid.
    /// <summary>Aranabilir <see cref="TwSelect"/> kutusunun ipucu.</summary>
    public string Search { get; set; } = "Search…";
    /// <summary><see cref="TwGrid.SetLoading"/> katmanının metni.</summary>
    public string Loading { get; set; } = "Loading…";
    /// <summary>Grid süzme menüsü.</summary>
    public string SelectAll { get; set; } = "(Select all)";
    public string ClearFilter { get; set; } = "Clear filter";
    /// <summary>Süzme menüsünde boş değerlerin görünen adı.</summary>
    public string Blank { get; set; } = "(Blank)";

    // Ekran okuyucular (UI Automation adları ve varsayılan eylemler).
    public string Open { get; set; } = "Open";
    public string Select { get; set; } = "Select";
    public string Expand { get; set; } = "Expand";
    public string Collapse { get; set; } = "Collapse";
    public string PreviousMonth { get; set; } = "Previous month";
    public string NextMonth { get; set; } = "Next month";
    /// <summary>Adı olmayan liste satırı; {0}: sıra numarası.</summary>
    public string Item { get; set; } = "Item {0}";
    /// <summary><see cref="TwPager"/>; {0}: sayfa, {1}: sayfa sayısı.</summary>
    public string Page { get; set; } = "Page {0} of {1}";
}
