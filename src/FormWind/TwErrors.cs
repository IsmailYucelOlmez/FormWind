using System;
using System.Diagnostics;
using System.Threading;

namespace FormWind;

/// <summary>Stil uygulanırken ve çizilirken çıkan hataların politikası: kritik olmayanlar yutulup raporlanır.</summary>
internal static class TwErrors
{
    /// <summary>Süreç durumunun güvenilmez olduğu, yakalanmaması gereken istisnalar.</summary>
    public static bool IsCritical(Exception exception) =>
        exception is OutOfMemoryException
        || exception is ThreadAbortException
        || exception is AccessViolationException
        || exception is StackOverflowException;

    /// <summary>Sağlayıcısı bilinmeyen yerlerde (çizim) hatayı Debug çıktısına yazar.</summary>
    public static void Trace(string where, Exception exception) =>
        Debug.WriteLine($"FormWind: {where} sırasında hata: {exception}");
}

/// <summary><see cref="TailwindProvider.StyleError"/> olayının verisi.</summary>
public sealed class TwStyleErrorEventArgs : EventArgs
{
    public TwStyleErrorEventArgs(object target, string classes, Exception exception)
    {
        Target = target;
        Classes = classes;
        Exception = exception;
    }

    /// <summary>Stillenen nesne: bir <see cref="System.Windows.Forms.Control"/> ya da <see cref="System.Windows.Forms.DataGridViewColumn"/>.</summary>
    public object Target { get; }

    /// <summary>Uygulanmaya çalışılan class'lar.</summary>
    public string Classes { get; }

    public Exception Exception { get; }
}
