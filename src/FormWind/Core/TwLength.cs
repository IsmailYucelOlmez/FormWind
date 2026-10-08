using System;
using System.Globalization;

namespace FormWind;

public enum TwLengthUnit
{
    Auto,
    Pixels,
    Fraction,
}

/// <summary>
/// Bir uzunluk: <c>auto</c>, piksel (96 DPI) ya da ebeveyn alanının oranı (<c>w-1/2</c> = 0,5, <c>w-full</c> = 1).
/// </summary>
public readonly struct TwLength : IEquatable<TwLength>
{
    private TwLength(float value, TwLengthUnit unit)
    {
        Value = value;
        Unit = unit;
    }

    public static TwLength Auto => new TwLength(0, TwLengthUnit.Auto);

    public float Value { get; }

    public TwLengthUnit Unit { get; }

    public bool IsAuto => Unit == TwLengthUnit.Auto;

    public bool IsPixels => Unit == TwLengthUnit.Pixels;

    public bool IsFraction => Unit == TwLengthUnit.Fraction;

    public static TwLength Pixels(float pixels) => new TwLength(pixels, TwLengthUnit.Pixels);

    public static TwLength Fraction(float fraction) => new TwLength(fraction, TwLengthUnit.Fraction);

    /// <summary>Piksel değerini DPI ile ölçekler; oran ve auto değişmez.</summary>
    public TwLength Scale(float scale) => IsPixels ? Pixels(Value * scale) : this;

    /// <summary>Kullanılabilir alana göre piksel karşılığı. Auto için <see cref="float.NaN"/>.</summary>
    public float Resolve(float available) => Unit switch
    {
        TwLengthUnit.Pixels => Value,
        TwLengthUnit.Fraction => Value * available,
        _ => float.NaN,
    };

    public bool Equals(TwLength other) => Unit == other.Unit && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is TwLength other && Equals(other);

    public override int GetHashCode() => ((int)Unit * 397) ^ Value.GetHashCode();

    public static bool operator ==(TwLength left, TwLength right) => left.Equals(right);

    public static bool operator !=(TwLength left, TwLength right) => !left.Equals(right);

    public override string ToString() => Unit switch
    {
        TwLengthUnit.Pixels => Value.ToString(CultureInfo.InvariantCulture) + "px",
        TwLengthUnit.Fraction => (Value * 100).ToString(CultureInfo.InvariantCulture) + "%",
        _ => "auto",
    };
}

public enum TwDisplay
{
    Block,
    Flex,
    Grid,
}

public enum TwFlexDirection
{
    Row,
    Column,
}

public enum TwJustify
{
    Start,
    End,
    Center,
    Between,
    Around,
    Evenly,
}

public enum TwAlign
{
    Start,
    End,
    Center,
    Stretch,
}
