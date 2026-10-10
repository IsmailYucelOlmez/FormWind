using System.Windows.Forms;

namespace FormWind;

/// <summary>
/// Sağdan sola (RTL) yardımcıları. Kontrolün kalıtılan <see cref="Control.RightToLeft"/> değeri <c>Yes</c> ise FormWind
/// yerleşimi ve kendi çizdiği parçaları aynalar; formda <c>RightToLeftLayout</c> açıksa Windows pencereleri zaten aynaladığı
/// için FormWind ayrıca aynalamaz.
/// </summary>
internal static class TwRtl
{
    public static bool IsRtl(Control control) => control.RightToLeft == RightToLeft.Yes;

    /// <summary>Pencere, Windows'un ayna yerleşimiyle (WS_EX_LAYOUTRTL) çiziliyor mu.</summary>
    public static bool IsMirroredByWindows(Control control)
    {
        for (var current = control; current != null; current = current.Parent)
        {
            if (current is Form { RightToLeftLayout: true, RightToLeft: RightToLeft.Yes }) return true;
            if (current is TabControl { RightToLeftLayout: true, RightToLeft: RightToLeft.Yes }) return true;
        }
        return false;
    }

    /// <summary>FormWind'in yerleşimi ve çizimi kendisinin aynalaması gerekiyor mu.</summary>
    public static bool NeedsMirroring(Control control) => IsRtl(control) && !IsMirroredByWindows(control);

    /// <summary>Metin bayraklarını sağdan sola okumaya çevirir: sol ve sağ hizalama yer değiştirir.</summary>
    public static TextFormatFlags Mirror(TextFormatFlags flags)
    {
        flags |= TextFormatFlags.RightToLeft;
        if ((flags & TextFormatFlags.HorizontalCenter) != 0) return flags;
        return (flags & TextFormatFlags.Right) != 0 ? flags & ~TextFormatFlags.Right : flags | TextFormatFlags.Right;
    }
}
