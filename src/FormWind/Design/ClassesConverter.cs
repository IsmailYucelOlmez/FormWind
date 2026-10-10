using System;
using System.ComponentModel;
using System.Linq;

namespace FormWind.Design;

/// <summary>
/// <c>Classes</c> alanında bileşen adlarını (btn-primary, card, input …) öneren açılır liste; serbest metin de yazılabilir.
/// TypeConverter'lar .NET 8+'ın süreç dışı Designer'ında da çalışır, orada <see cref="ClassesEditor"/> açılmaz.
/// Seçilen ad alanın tüm değerinin yerine geçer.
/// </summary>
public sealed class ClassesConverter : StringConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) =>
        new StandardValuesCollection(TwTheme.Current.Components.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
}
