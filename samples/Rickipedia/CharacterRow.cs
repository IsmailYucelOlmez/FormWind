using System;
using System.Drawing;
using System.Windows.Forms;
using FormWind;
using Rickipedia.Api;

namespace Rickipedia
{
    /// <summary>
    /// Listedeki bir karakter: avatar | ad + köken | durum | tür + konum | cinsiyet | ok.
    /// Kolon yapısı tablo başlığıyla aynıdır (avatar boşluğu + 4 eşit kolon + ok boşluğu).
    /// </summary>
    internal sealed class CharacterRow : TwPanel
    {
        private readonly TwPictureBox _avatar = new TwPictureBox { Size = new Size(88, 88) };

        public CharacterRow(TailwindProvider tw, RickAndMortyClient client, Character character)
        {
            Height = 112;

            Controls.Add(_avatar);
            Controls.Add(SplitCell(tw, character.Name, "Origin", character.Origin));
            Controls.Add(Cell(tw, character.Status));
            Controls.Add(SplitCell(tw, character.Species, "Location", character.Location));
            Controls.Add(Cell(tw, character.Gender));

            var chevron = new TwIcon { IconName = "chevron-right" };
            Controls.Add(chevron);

            tw.SetClasses(this, "row");
            tw.SetClasses(_avatar, "avatar");
            tw.SetClasses(chevron, "w-6 shrink-0 self-center text-xl text-gray-900 group-hover:text-blue-600 dark:text-gray-100 dark:group-hover:text-blue-400");

            WireClicks(this);
            LoadImage(client, character.Image);
        }

        public event EventHandler Activated;

        public bool ImageLoaded { get; private set; }

        private static TwPanel Cell(TailwindProvider tw, string text)
        {
            var cell = new TwPanel();
            var label = new Label { AutoSize = true, Text = text };
            cell.Controls.Add(label);
            tw.SetClasses(cell, "cell");
            tw.SetClasses(label, "cell-text");
            return cell;
        }

        // Üstte ana metin, altta "Origin: 'Earth (C-137)' ↗" satırı.
        private static TwPanel SplitCell(TailwindProvider tw, string text, string metaLabel, NamedResource resource)
        {
            var cell = new TwPanel();
            var label = new Label { AutoSize = true, Text = text };
            var meta = new TwPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var metaText = new Label { AutoSize = true, Text = metaLabel + ": '" + resource.Name + "'" };
            meta.Controls.Add(metaText);

            // Bilinmeyen kökenin bağlantısı yoktur (ekran görüntüsündeki gibi ikon gösterilmez).
            if (!string.IsNullOrEmpty(resource.Url))
            {
                var link = new TwIcon { IconName = "external-link" };
                meta.Controls.Add(link);
                tw.SetClasses(link, "text-xs text-gray-900 dark:text-gray-300");
            }

            cell.Controls.Add(label);
            cell.Controls.Add(meta);
            tw.SetClasses(cell, "cell-split");
            tw.SetClasses(meta, "flex items-center gap-1");
            tw.SetClasses(metaText, "cell-meta min-w-0 truncate");
            tw.SetClasses(label, "cell-text truncate");
            return cell;
        }

        private void WireClicks(Control control)
        {
            control.Click += (_, _) => Activated?.Invoke(this, EventArgs.Empty);
            foreach (Control child in control.Controls) WireClicks(child);
        }

        private async void LoadImage(RickAndMortyClient client, string url)
        {
            try
            {
                var image = await client.GetImageAsync(url);
                if (!IsDisposed) _avatar.Image = image;
            }
            catch (Exception)
            {
                // Avatar yüklenemezse gri yer tutucu kalır.
            }
            finally
            {
                ImageLoaded = true;
            }
        }
    }
}
