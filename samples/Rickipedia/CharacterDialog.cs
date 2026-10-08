using System;
using System.Drawing;
using System.Windows.Forms;
using FormWind;
using Rickipedia.Api;

namespace Rickipedia
{
    /// <summary>Satıra tıklanınca açılan karakter ayrıntıları.</summary>
    internal sealed class CharacterDialog : Form
    {
        private readonly TailwindProvider _tw;

        public CharacterDialog(RickAndMortyClient client, Character character, bool dark)
        {
            _tw = new TailwindProvider { DarkMode = dark };

            Text = character.Name;
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(560, 352);

            var root = new TwPanel { Dock = DockStyle.Fill };
            var avatar = new TwPictureBox { Size = new Size(220, 220) };
            var info = new TwPanel();
            root.Controls.Add(avatar);
            root.Controls.Add(info);

            var name = new Label { AutoSize = true, Text = character.Name };
            var statusRow = new TwPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var dot = new TwPanel { Size = new Size(10, 10) };
            var status = new Label { AutoSize = true, Text = character.Status + " – " + character.Species };
            statusRow.Controls.Add(dot);
            statusRow.Controls.Add(status);

            info.Controls.Add(name);
            info.Controls.Add(statusRow);
            info.Controls.Add(Field("Gender", character.Gender));
            info.Controls.Add(Field("Origin", character.Origin.Name));
            info.Controls.Add(Field("Last known location", character.Location.Name));
            info.Controls.Add(Field("Episodes", character.Episode.Count.ToString()));

            var close = new TwButton { Text = "Close", Size = new Size(96, 36), DialogResult = DialogResult.OK };
            info.Controls.Add(close);
            AcceptButton = close;
            CancelButton = close;

            Controls.Add(root);

            var dotColor = character.Status == "Alive" ? "bg-green-500" : character.Status == "Dead" ? "bg-red-500" : "bg-gray-400";
            _tw.SetClasses(this, "bg-white dark:bg-gray-900 dark:caption:bg-gray-900 dark:caption:text-gray-100");
            _tw.SetClasses(root, "flex items-start gap-6 p-6 bg-white dark:bg-gray-900");
            _tw.SetClasses(avatar, "rounded-xl object-cover shadow-lg shrink-0 bg-gray-100 dark:bg-gray-800");
            _tw.SetClasses(info, "flex-1 self-stretch flex flex-col gap-2");
            _tw.SetClasses(name, "text-2xl font-bold text-gray-900 dark:text-white");
            _tw.SetClasses(statusRow, "flex items-center gap-2 mb-2");
            _tw.SetClasses(dot, "rounded-full " + dotColor);
            _tw.SetClasses(status, "text-sm font-medium text-gray-700 dark:text-gray-300");
            _tw.SetClasses(close, "btn-primary self-end mt-2");

            LoadImage(client, character.Image, avatar);
        }

        private TwPanel Field(string label, string value)
        {
            var field = new TwPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var caption = new Label { AutoSize = true, Text = label };
            var text = new Label { AutoSize = true, Text = value };
            field.Controls.Add(caption);
            field.Controls.Add(text);
            _tw.SetClasses(field, "flex flex-col");
            _tw.SetClasses(caption, "text-xs text-gray-500 dark:text-gray-400");
            _tw.SetClasses(text, "text-sm text-gray-900 dark:text-gray-100");
            return field;
        }

        private static async void LoadImage(RickAndMortyClient client, string url, TwPictureBox target)
        {
            try
            {
                var image = await client.GetImageAsync(url);
                if (!target.IsDisposed) target.Image = image;
            }
            catch (Exception)
            {
                // yer tutucu kalır
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _tw.Dispose();
            base.Dispose(disposing);
        }
    }
}
