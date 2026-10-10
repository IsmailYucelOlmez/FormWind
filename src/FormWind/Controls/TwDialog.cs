using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FormWind;

public enum TwDialogKind
{
    Info,
    Success,
    Warning,
    Error,
    Question,
}

public enum TwDialogButtons
{
    Ok,
    OkCancel,
    YesNo,
    YesNoCancel,
}

/// <summary><see cref="TwDialog"/> seçenekleri.</summary>
public sealed class TwDialogOptions
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public TwDialogKind Kind { get; set; } = TwDialogKind.Info;
    public TwDialogButtons Buttons { get; set; } = TwDialogButtons.Ok;

    /// <summary>Enter'ın bastığı düğme (varsayılan: ilk olumlu düğme).</summary>
    public DialogResult? DefaultResult { get; set; }

    /// <summary>Olumlu düğme kırmızı çizilir (silme onayı gibi).</summary>
    public bool Danger { get; set; }

    /// <summary>Koyu tema; verilmezse sahip formun başlık çubuğundan (FormWind class'ı varsa) alınır.</summary>
    public bool? Dark { get; set; }
}

/// <summary>
/// MessageBox yerine stillenebilir diyalog: ikon, başlık, mesaj ve sağa dayalı düğmeler.
/// <code>
/// if (TwDialog.Show(this, "Sipariş silinsin mi?", "Silme onayı", TwDialogKind.Warning, TwDialogButtons.YesNo, danger: true) == DialogResult.Yes) …
/// </code>
/// Düğme metinleri <see cref="Texts"/> ile değiştirilebilir. Esc iptal/hayır, Enter varsayılan düğmedir.
/// </summary>
public static class TwDialog
{
    /// <summary>Düğme metinleri (yerelleştirme için).</summary>
    public static Dictionary<DialogResult, string> Texts { get; } = new Dictionary<DialogResult, string>
    {
        [DialogResult.OK] = "Tamam",
        [DialogResult.Cancel] = "İptal",
        [DialogResult.Yes] = "Evet",
        [DialogResult.No] = "Hayır",
    };

    public static DialogResult Show(IWin32Window? owner, string message, string? title = null,
        TwDialogKind kind = TwDialogKind.Info, TwDialogButtons buttons = TwDialogButtons.Ok, bool danger = false) =>
        Show(owner, new TwDialogOptions { Message = message, Title = title ?? "", Kind = kind, Buttons = buttons, Danger = danger });

    public static DialogResult Show(IWin32Window? owner, TwDialogOptions options)
    {
        using var dialog = Create(options, owner as Control);
        return owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
    }

    /// <summary>Diyaloğu göstermeden oluşturur (özelleştirme ve testler için).</summary>
    public static TwDialogForm Create(TwDialogOptions options, Control? owner = null)
    {
        bool dark = options.Dark ?? (owner?.FindForm() is Form form && TwTitleBar.For(form).Dark);
        return new TwDialogForm(options, dark);
    }
}

/// <summary><see cref="TwDialog"/>'un formu.</summary>
public sealed class TwDialogForm : Form
{
    private readonly TailwindProvider _tw = new TailwindProvider();
    private readonly List<TwButton> _buttons = new List<TwButton>();

    internal TwDialogForm(TwDialogOptions options, bool dark)
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9f);
        Text = options.Title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        KeyPreview = true;
        AccessibleRole = AccessibleRole.Dialog;
        Options = options;

        var root = new TwPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        Controls.Add(root);
        var body = new TwPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        root.Controls.Add(body);

        var (icon, tone) = options.Kind switch
        {
            TwDialogKind.Success => ("check", "green"),
            TwDialogKind.Warning => ("warning", "amber"),
            TwDialogKind.Error => ("error", "red"),
            TwDialogKind.Question => ("help", "blue"),
            _ => ("info", "blue"),
        };
        var badge = new TwPanel { Size = new Size(40, 40) };
        body.Controls.Add(badge);
        // Şeffaf zemin: yuvarlak rozetin köşelerini ikonun kare zemini örtmesin.
        IconBadge = new TwIcon { IconName = icon, Dock = DockStyle.Fill, AccessibleName = options.Kind.ToString(), BackColor = Color.Transparent };
        badge.Controls.Add(IconBadge);

        var texts = new TwPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        body.Controls.Add(texts);
        TitleLabel = new Label { AutoSize = true, Text = options.Title, Visible = options.Title.Length > 0 };
        MessageLabel = new Label { AutoSize = true, Text = options.Message, MaximumSize = new Size(360, 0) };
        texts.Controls.Add(TitleLabel);
        texts.Controls.Add(MessageLabel);

        var row = new TwPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        root.Controls.Add(row);
        var results = options.Buttons switch
        {
            TwDialogButtons.OkCancel => new[] { DialogResult.Cancel, DialogResult.OK },
            TwDialogButtons.YesNo => new[] { DialogResult.No, DialogResult.Yes },
            TwDialogButtons.YesNoCancel => new[] { DialogResult.Cancel, DialogResult.No, DialogResult.Yes },
            _ => new[] { DialogResult.OK },
        };
        var primary = results[results.Length - 1];
        foreach (var result in results)
        {
            var button = new TwButton { Text = TwDialog.Texts[result], DialogResult = result, AutoSize = true, MinimumSize = new Size(88, 0) };
            row.Controls.Add(button);
            _buttons.Add(button);
            _tw.SetClasses(button, result == primary ? (options.Danger ? "btn-danger" : "btn-primary") : "btn-ghost");
        }

        var defaultResult = options.DefaultResult ?? primary;
        AcceptButton = _buttons.Find(b => b.DialogResult == defaultResult) ?? _buttons[_buttons.Count - 1];
        CancelButton = _buttons.Find(b => b.DialogResult is DialogResult.Cancel or DialogResult.No) ?? (results.Length == 1 ? _buttons[0] : null);

        _tw.SetClasses(this, "font-sans bg-white dark:bg-gray-800 dark:caption:bg-gray-800 dark:caption:text-gray-100");
        _tw.SetClasses(root, "flex flex-col gap-6 p-6 bg-white dark:bg-gray-800");
        _tw.SetClasses(body, "flex items-start gap-4");
        _tw.SetClasses(badge, $"rounded-full bg-{tone}-100 dark:bg-{tone}-900");
        _tw.SetClasses(IconBadge, $"text-xl text-{tone}-600 dark:text-{tone}-300");
        _tw.SetClasses(texts, "flex flex-col gap-1 pt-1");
        _tw.SetClasses(TitleLabel, "text-base font-semibold text-gray-900 dark:text-white");
        _tw.SetClasses(MessageLabel, "text-sm text-gray-600 dark:text-gray-300");
        _tw.SetClasses(row, "flex justify-end gap-2");
        _tw.DarkMode = dark;
        ResumeLayout(true);
    }

    public TwDialogOptions Options { get; }

    public IReadOnlyList<TwButton> Buttons => _buttons;

    public Label TitleLabel { get; }

    public Label MessageLabel { get; }

    public TwIcon IconBadge { get; }

    /// <summary>Diyaloğun class sağlayıcısı (görünümü değiştirmek için).</summary>
    public TailwindProvider Provider => _tw;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        (AcceptButton as Control)?.Focus();
        // Ekran okuyucu diyaloğu açılınca mesajı okusun.
        AccessibilityNotifyClients(AccessibleEvents.SystemDialogStart, -1);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tw.Dispose();
        base.Dispose(disposing);
    }
}
