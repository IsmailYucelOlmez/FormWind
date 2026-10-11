using System;
using System.ComponentModel;
using System.Linq;

namespace FormWind.Design;

/// <summary>
/// Drop-down for the <c>Classes</c> field that suggests component names (btn-primary, card, input …); free text can be typed too.
/// TypeConverters also work in the .NET 8+ out-of-process Designer, where <see cref="ClassesEditor"/> does not open.
/// A selected name replaces the field's whole value.
/// </summary>
public sealed class ClassesConverter : StringConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) =>
        new StandardValuesCollection(TwTheme.Current.Components.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
}
