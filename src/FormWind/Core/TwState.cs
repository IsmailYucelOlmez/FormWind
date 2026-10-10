using System;

namespace FormWind;

/// <summary>
/// Stil hesabında kullanılan durum bayrakları. Değerler aynı zamanda varyantların
/// uygulanma önceliğini belirler: yüksek bit, sonra uygulanır (ve öncekini ezer).
/// </summary>
[Flags]
public enum TwState
{
    None = 0,
    Dark = 1 << 0,
    Checked = 1 << 1,
    Focus = 1 << 2,

    /// <summary>Odak klavyeyle geldiyse (<c>focus-visible:</c>); fareyle tıklanınca uygulanmaz.</summary>
    FocusVisible = 1 << 3,

    /// <summary><c>group</c> class'lı bir üst kontrolün üzerindeyken (<c>group-hover:</c>).</summary>
    GroupHover = 1 << 4,

    Hover = 1 << 5,
    Active = 1 << 6,
    Disabled = 1 << 7,

    /// <summary>Kontrolün doğrulama hatası var (<c>invalid:</c>; bkz. <see cref="TailwindProvider.SetError"/>).</summary>
    Invalid = 1 << 8,

    /// <summary>Formun genişliği en az 640 px (<c>sm:</c>; bkz. <see cref="TwBreakpoints"/>).</summary>
    Sm = 1 << 9,

    /// <summary>En az 768 px (<c>md:</c>).</summary>
    Md = 1 << 10,

    /// <summary>En az 1024 px (<c>lg:</c>).</summary>
    Lg = 1 << 11,

    /// <summary>En az 1280 px (<c>xl:</c>).</summary>
    Xl = 1 << 12,

    /// <summary>En az 1536 px (<c>2xl:</c>).</summary>
    Xxl = 1 << 13,

    /// <summary>Kontrolün (kalıtılan) <c>RightToLeft</c> değeri <c>No</c> (<c>ltr:</c>).</summary>
    Ltr = 1 << 14,

    /// <summary>Kontrolün (kalıtılan) <c>RightToLeft</c> değeri <c>Yes</c> (<c>rtl:</c>).</summary>
    Rtl = 1 << 15,

    /// <summary>
    /// Windows Yüksek Karşıtlık modu açık (<c>forced-colors:</c>). Bu durumda class renkleri sistem renklerine çevrilir;
    /// yalnızca <c>forced-colors:</c> varyantlarının renkleri uygulanır.
    /// </summary>
    HighContrast = 1 << 16,

    /// <summary>Tüm genişlik eşikleri.</summary>
    Breakpoints = Sm | Md | Lg | Xl | Xxl,
}
