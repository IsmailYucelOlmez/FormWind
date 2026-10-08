using System;
using System.Windows.Forms;

namespace FormWind.Showcase
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            LoadOrders();
        }

        public bool DarkMode
        {
            get => chkDark.Checked;
            set => chkDark.Checked = value;
        }

        public void FocusFirstInput() => txtName.Focus();

        private void chkDark_CheckedChanged(object sender, EventArgs e)
        {
            tailwindProvider1.DarkMode = chkDark.Checked;
        }

        private void LoadOrders()
        {
            gridOrders.Columns.Add("no", "SİPARİŞ NO");
            gridOrders.Columns.Add("customer", "MÜŞTERİ");
            gridOrders.Columns.Add("date", "TARİH");
            gridOrders.Columns.Add("amount", "TUTAR");
            gridOrders.Columns.Add("status", "DURUM");

            // Kolon bazlı hizalama class'larla değil, kolonun kendi stiliyle yapılır.
            gridOrders.Columns["amount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            gridOrders.Rows.Add("#10234", "Ayşe Yılmaz", "08.10.2026", "1.250,00 ₺", "Hazırlanıyor");
            gridOrders.Rows.Add("#10233", "Mehmet Demir", "08.10.2026", "480,50 ₺", "Kargoda");
            gridOrders.Rows.Add("#10232", "Zeynep Kaya", "07.10.2026", "2.940,00 ₺", "Teslim edildi");
            gridOrders.Rows.Add("#10231", "Can Öztürk", "07.10.2026", "175,00 ₺", "İptal");
            gridOrders.Rows.Add("#10230", "Elif Şahin", "06.10.2026", "860,25 ₺", "Teslim edildi");
        }
    }
}
