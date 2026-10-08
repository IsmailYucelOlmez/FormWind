using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace FormWind.Design;

/// <summary>
/// Property penceresinde <c>Classes</c> alanı için "…" düğmesi. Bileşen listesi ve bilinmeyen class uyarısı içerir.
/// .NET 8+ projelerinde out-of-process Designer bu editörü yüklemez; orada alan düz metin olarak düzenlenir.
/// </summary>
public sealed class ClassesEditor : UITypeEditor
{
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) => UITypeEditorEditStyle.Modal;

    public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
    {
        if (provider?.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService service)
            return value;

        using var dialog = new ClassesEditorForm(value as string ?? "", TwTheme.Current);
        return service.ShowDialog(dialog) == DialogResult.OK ? dialog.Classes : value;
    }
}

internal sealed class ClassesEditorForm : Form
{
    private readonly TwTheme _theme;
    private readonly TextBox _input;
    private readonly ListBox _components;
    private readonly Label _status;
    private readonly Font _monoFont = new Font("Consolas", 10f);

    public ClassesEditorForm(string classes, TwTheme theme)
    {
        _theme = theme;

        Text = "Classes";
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(600, 320);
        MinimumSize = new Size(420, 240);
        ShowInTaskbar = false;

        _input = new TextBox
        {
            Multiline = true,
            WordWrap = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Font = _monoFont,
            Text = classes,
        };
        _input.TextChanged += (_, _) => UpdateStatus();

        _components = new ListBox { Dock = DockStyle.Right, Width = 180, IntegralHeight = false };
        foreach (var name in theme.Components.Keys.OrderBy(n => n, StringComparer.Ordinal))
            _components.Items.Add(name);
        _components.DoubleClick += (_, _) => Insert(_components.SelectedItem as string);

        _status = new Label { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(6), AutoEllipsis = true };

        var ok = new Button { Text = "Tamam", DialogResult = DialogResult.OK, Width = 90 };
        var cancel = new Button { Text = "İptal", DialogResult = DialogResult.Cancel, Width = 90 };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 40, Padding = new Padding(4) };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        AcceptButton = ok;
        CancelButton = cancel;

        // Dock sırası: önce eklenen en son yerleşir; Fill en başta olmalı.
        Controls.Add(_input);
        Controls.Add(_components);
        Controls.Add(_status);
        Controls.Add(buttons);

        UpdateStatus();
    }

    public string Classes => TwParser.Normalize(_input.Text);

    protected override void Dispose(bool disposing)
    {
        if (disposing) _monoFont.Dispose();
        base.Dispose(disposing);
    }

    private void Insert(string? name)
    {
        if (name is null) return;
        var text = _input.Text.TrimEnd();
        _input.Text = text.Length == 0 ? name : text + " " + name;
        _input.SelectionStart = _input.TextLength;
        _input.Focus();
    }

    private void UpdateStatus()
    {
        var parsed = _theme.Parse(_input.Text);
        if (parsed.Unknown.Count == 0)
        {
            _status.Text = "Tüm class'lar tanındı. Bileşen eklemek için listeye çift tıklayın.";
            _status.ForeColor = SystemColors.GrayText;
        }
        else
        {
            _status.Text = "Bilinmeyen: " + string.Join(", ", parsed.Unknown);
            _status.ForeColor = Color.Firebrick;
        }
    }
}
