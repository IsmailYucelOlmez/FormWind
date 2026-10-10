using System;
using System.Collections.Generic;

namespace FormWind;

/// <summary>Bir varyant (örn. <c>dark:hover</c> ya da <c>dark:header</c>) ve ona ait stil.</summary>
public sealed class TwVariant
{
    internal TwVariant(TwState conditions, string? part, string name)
    {
        Conditions = conditions;
        Part = part;
        Name = name;
    }

    /// <summary>Varyantın uygulanması için gereken durumlar. <see cref="TwState.None"/> = temel class'lar.</summary>
    public TwState Conditions { get; }

    /// <summary>
    /// Kontrolün bir parçası (örn. DataGridView'da <c>header</c>, <c>cell</c>). <c>null</c> = kontrolün kendisi.
    /// Parça stillerini yalnızca o parçayı tanıyan uygulayıcı kullanır.
    /// </summary>
    public string? Part { get; }

    public string Name { get; }

    public TwStyle Style { get; } = new TwStyle();
}

/// <summary>Bir class dizisinin parse sonucu. Değişmezdir ve tema tarafından önbelleğe alınır.</summary>
public sealed class TwParsed
{
    public static readonly TwParsed Empty = new TwParsed("", Array.Empty<TwVariant>(), Array.Empty<string>());

    internal TwParsed(string classes, IReadOnlyList<TwVariant> variants, IReadOnlyList<string> unknown)
    {
        Classes = classes;
        Variants = variants;
        Unknown = unknown;
    }

    public string Classes { get; }

    /// <summary>Uygulama sırasına dizilmiş varyantlar.</summary>
    public IReadOnlyList<TwVariant> Variants { get; }

    /// <summary>Tanınmayan class'lar (varyant önekleriyle birlikte).</summary>
    public IReadOnlyList<string> Unknown { get; }

    /// <summary>Kontrolün kendisi için: koşulları <paramref name="state"/> içinde kalan varyantları sırayla birleştirir.</summary>
    public TwStyle Resolve(TwState state) => ResolvePart(null, state);

    /// <summary>Bir parça için (<c>null</c> = kontrolün kendisi) koşulları <paramref name="state"/> içinde kalan varyantları birleştirir.</summary>
    public TwStyle ResolvePart(string? part, TwState state)
    {
        var style = new TwStyle();
        foreach (var variant in Variants)
        {
            if (variant.Part == part && (variant.Conditions & state) == variant.Conditions)
                style.MergeFrom(variant.Style);
        }
        return style;
    }

    /// <summary>Kontrolün kendisi için koşulları arasında <paramref name="condition"/> bulunan bir varyant var mı.</summary>
    /// <summary>Herhangi bir varyant (parçalılar dahil) verilen durumlardan birini kullanıyor mu.</summary>
    public bool UsesAny(TwState condition)
    {
        foreach (var variant in Variants)
            if ((variant.Conditions & condition) != 0) return true;
        return false;
    }

    public bool HasCondition(TwState condition)
    {
        foreach (var variant in Variants)
        {
            if (variant.Part is null && (variant.Conditions & condition) != 0) return true;
        }
        return false;
    }

    /// <summary>Parçanın, koşulları arasında <paramref name="condition"/> bulunan bir varyantı var mı (örn. <c>row:hover:</c>).</summary>
    public bool HasPartCondition(string part, TwState condition)
    {
        foreach (var variant in Variants)
        {
            if (variant.Part == part && (variant.Conditions & condition) == condition) return true;
        }
        return false;
    }

    /// <summary>
    /// Koşulları <paramref name="required"/>'ın tamamını içeren ve <paramref name="allowed"/> dışına
    /// çıkmayan varyantları birleştirir (yalnızca kontrolün kendisi). Durumları ayrı tutan kütüphaneler (örn. Krypton) için.
    /// </summary>
    public TwStyle ResolveExact(TwState required, TwState allowed)
    {
        var style = new TwStyle();
        foreach (var variant in Variants)
        {
            if (variant.Part is null
                && (variant.Conditions & required) == required
                && (variant.Conditions & ~allowed) == 0)
                style.MergeFrom(variant.Style);
        }
        return style;
    }
}
