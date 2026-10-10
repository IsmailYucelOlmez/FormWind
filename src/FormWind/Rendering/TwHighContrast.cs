using System;
using Microsoft.Win32;
using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// Windows Yüksek Karşıtlık modunu izler. Mod açıkken <see cref="TailwindProvider"/> class renkleri yerine sistem renklerini
/// kullanır (bkz. <see cref="TailwindProvider.RespectHighContrast"/>); mod çalışırken değişirse stiller yeniden uygulanır.
/// </summary>
internal static class TwHighContrast
{
    private static bool _subscribed;
    private static bool _last;

    /// <summary>Testlerde modu benzetmek için; <c>null</c> = sistem ayarı.</summary>
    internal static bool? Override { get; set; }

    public static bool IsActive => Override ?? SystemInformation.HighContrast;

    /// <summary>Mod açıldı ya da kapandı.</summary>
    public static event EventHandler? Changed
    {
        add
        {
            EnsureSubscribed();
            ChangedCore += value;
        }
        remove => ChangedCore -= value;
    }

    private static event EventHandler? ChangedCore;

    /// <summary>Değişikliği dinleyicilere bildirir (sistem olayı ya da testler).</summary>
    internal static void NotifyChanged()
    {
        _last = IsActive;
        ChangedCore?.Invoke(null, EventArgs.Empty);
    }

    private static void EnsureSubscribed()
    {
        if (_subscribed) return;
        _subscribed = true;
        _last = IsActive;
        // SystemEvents olayı abonenin UI iş parçacığında tetikler.
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category != UserPreferenceCategory.Accessibility && e.Category != UserPreferenceCategory.Color) return;
            if (IsActive != _last) NotifyChanged();
        };
    }
}
