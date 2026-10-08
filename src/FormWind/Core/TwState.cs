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

    /// <summary><c>group</c> class'lı bir üst kontrolün üzerindeyken (<c>group-hover:</c>).</summary>
    GroupHover = 1 << 3,

    Hover = 1 << 4,
    Active = 1 << 5,
    Disabled = 1 << 6,
}
