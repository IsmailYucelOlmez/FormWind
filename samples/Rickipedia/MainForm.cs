using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rickipedia.Api;

namespace Rickipedia
{
    public partial class MainForm : Form
    {
        private readonly RickAndMortyClient _client = new RickAndMortyClient();
        private readonly List<CharacterRow> _rows = new List<CharacterRow>();
        private List<Character> _results = new List<Character>();
        private CancellationTokenSource _loading;
        private bool _loaded;

        public MainForm()
        {
            InitializeComponent();

            var logo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
            if (File.Exists(logo)) picLogo.Image = Image.FromFile(logo);

            // Liste kaydırma çubuğu gösterdiğinde başlık kolonları da aynı genişlikte kalsın.
            pnlList.ClientSizeChanged += (_, _) =>
                pnlTableHeader.Padding = new Padding(0, 0, pnlList.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0, 0);

            UpdatePadding();
        }

        public bool DarkMode
        {
            get => tw.DarkMode;
            set
            {
                tw.DarkMode = value;
                icoTheme.IconName = value ? "sun" : "moon";
            }
        }

        /// <summary>Ekran görüntüsü modu: durum filtresinin menüsünü açar.</summary>
        internal ContextMenuStrip OpenStatusMenu()
        {
            selStatus.ShowDropDown();
            return selStatus.DropDownMenu;
        }

        /// <summary>Ekran görüntüsü modu: ilk karakterin ayrıntı penceresini (modal olmadan) oluşturur.</summary>
        internal Form CreateFirstDetails() =>
            _results.Count == 0 ? null : new CharacterDialog(_client, _results[0], tw.DarkMode);

        /// <summary>Liste yüklendi ve görünen tüm avatarlar indirildi (ekran görüntüsü modu için).</summary>
        public bool IsReadyForSnapshot => _loaded && _rows.All(r => r.ImageLoaded);

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await LoadAsync();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdatePadding();
        }

        // İçerik, ekran görüntüsündeki oranlarla ortalanır: menü genişliğin ~%58'i, liste ~%75'i.
        private void UpdatePadding()
        {
            if (pnlHeader == null) return;
            int width = ClientSize.Width;
            int header = Math.Max(24, (int)(width * 0.21));
            int content = Math.Max(24, (int)(width * 0.125));
            pnlHeader.Padding = new Padding(header, 0, header, 0);
            pnlContent.Padding = new Padding(content, 28, content, 0);
        }

        private async Task LoadAsync()
        {
            _loading?.Cancel();
            var cancellation = _loading = new CancellationTokenSource();
            _loaded = false;
            ShowMessage("Loading characters...");

            try
            {
                var page = await _client.GetCharactersAsync(
                    txtSearch.Text,
                    Filter(selStatus),
                    Filter(selSpecies),
                    Filter(selGender),
                    cancellation.Token);
                if (cancellation.IsCancellationRequested) return;

                _results = page.Results;
                ShowRows();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // Yeni bir arama başladı.
            }
            catch (Exception ex)
            {
                _results = new List<Character>();
                ShowRows();
                ShowMessage("Characters could not be loaded: " + ex.Message);
            }
            finally
            {
                if (!cancellation.IsCancellationRequested) _loaded = true;
            }
        }

        // İlk seçenek ("All ...") filtre uygulamaz.
        private static string Filter(FormWind.TwSelect select) => select.SelectedIndex > 0 ? select.SelectedItem : null;

        private int RowLimit => selRows.SelectedIndex switch { 0 => 5, 1 => 10, _ => 20 };

        private void ShowRows()
        {
            pnlList.SuspendLayout();
            try
            {
                foreach (var row in _rows)
                {
                    pnlList.Controls.Remove(row);
                    row.Dispose();
                }
                _rows.Clear();

                foreach (var character in _results.Take(RowLimit))
                {
                    var row = new CharacterRow(tw, _client, character);
                    row.Activated += (_, _) => ShowDetails(character);
                    _rows.Add(row);
                    pnlList.Controls.Add(row);
                }

                lblMessage.Visible = _rows.Count == 0;
                if (_rows.Count == 0) lblMessage.Text = "No characters match these filters.";
            }
            finally
            {
                pnlList.ResumeLayout(true);
            }
            pnlList.AutoScrollPosition = Point.Empty;
        }

        private void ShowMessage(string text)
        {
            lblMessage.Text = text;
            lblMessage.Visible = true;
            lblMessage.BringToFront();
        }

        private void ShowDetails(Character character)
        {
            using (var dialog = new CharacterDialog(_client, character, tw.DarkMode))
                dialog.ShowDialog(this);
        }

        private async void btnSearch_Click(object sender, EventArgs e) => await LoadAsync();

        private async void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await LoadAsync();
        }

        private async void lnkClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            await LoadAsync();
        }

        private async void filter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (IsHandleCreated) await LoadAsync();
        }

        private void selRows_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (IsHandleCreated) ShowRows();
        }

        private void icoTheme_Click(object sender, EventArgs e) => DarkMode = !DarkMode;
    }
}
