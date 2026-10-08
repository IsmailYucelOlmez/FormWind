namespace Rickipedia
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tw = new FormWind.TailwindProvider(this.components);
            this.pnlHeader = new FormWind.TwPanel();
            this.pnlBrand = new FormWind.TwPanel();
            this.picLogo = new FormWind.TwPictureBox();
            this.lblBrand = new System.Windows.Forms.Label();
            this.pnlNav = new FormWind.TwPanel();
            this.lnkCharacters = new System.Windows.Forms.Label();
            this.lnkLocations = new System.Windows.Forms.Label();
            this.lnkEpisodes = new System.Windows.Forms.Label();
            this.icoTheme = new FormWind.TwIcon();
            this.pnlContent = new FormWind.TwPanel();
            this.pnlToolbar = new FormWind.TwPanel();
            this.txtSearch = new FormWind.TwTextBox();
            this.btnSearch = new FormWind.TwButton();
            this.lnkClear = new System.Windows.Forms.Label();
            this.pnlToolbarSpacer = new FormWind.TwPanel();
            this.selRows = new FormWind.TwSelect();
            this.selStatus = new FormWind.TwSelect();
            this.selSpecies = new FormWind.TwSelect();
            this.selGender = new FormWind.TwSelect();
            this.pnlTableHeader = new FormWind.TwPanel();
            this.pnlHeaderAvatar = new FormWind.TwPanel();
            this.pnlThName = new FormWind.TwPanel();
            this.lblThName = new System.Windows.Forms.Label();
            this.pnlThStatus = new FormWind.TwPanel();
            this.lblThStatus = new System.Windows.Forms.Label();
            this.pnlThSpecies = new FormWind.TwPanel();
            this.lblThSpecies = new System.Windows.Forms.Label();
            this.pnlThGender = new FormWind.TwPanel();
            this.lblThGender = new System.Windows.Forms.Label();
            this.pnlHeaderChevron = new FormWind.TwPanel();
            this.pnlList = new FormWind.TwPanel();
            this.lblMessage = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.tw)).BeginInit();
            this.pnlHeader.SuspendLayout();
            this.pnlBrand.SuspendLayout();
            this.pnlNav.SuspendLayout();
            this.pnlContent.SuspendLayout();
            this.pnlToolbar.SuspendLayout();
            this.pnlTableHeader.SuspendLayout();
            this.pnlList.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlHeader: logo solda, menü sağda; yatay boşluk pencere genişliğine göre koddan verilir.
            //
            this.pnlHeader.Controls.Add(this.pnlBrand);
            this.pnlHeader.Controls.Add(this.pnlNav);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(1280, 56);
            this.pnlHeader.TabIndex = 1;
            this.tw.SetClasses(this.pnlHeader, "flex items-center justify-between bg-white border-b border-gray-800 dark:bg-gray-900 dark:border-gray-700");
            //
            // pnlBrand
            //
            this.pnlBrand.AutoSize = true;
            this.pnlBrand.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlBrand.Controls.Add(this.picLogo);
            this.pnlBrand.Controls.Add(this.lblBrand);
            this.pnlBrand.Name = "pnlBrand";
            this.pnlBrand.TabIndex = 0;
            this.tw.SetClasses(this.pnlBrand, "flex items-center gap-1");
            //
            // picLogo
            //
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size(36, 36);
            this.picLogo.TabIndex = 0;
            this.tw.SetClasses(this.picLogo, "object-contain");
            //
            // lblBrand
            //
            this.lblBrand.AutoSize = true;
            this.lblBrand.Name = "lblBrand";
            this.lblBrand.TabIndex = 1;
            this.lblBrand.Text = "Rickipedia";
            this.tw.SetClasses(this.lblBrand, "text-base font-bold text-gray-900 dark:text-white");
            //
            // pnlNav
            //
            this.pnlNav.AutoSize = true;
            this.pnlNav.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlNav.Controls.Add(this.lnkCharacters);
            this.pnlNav.Controls.Add(this.lnkLocations);
            this.pnlNav.Controls.Add(this.lnkEpisodes);
            this.pnlNav.Controls.Add(this.icoTheme);
            this.pnlNav.Name = "pnlNav";
            this.pnlNav.TabIndex = 1;
            this.tw.SetClasses(this.pnlNav, "flex items-center gap-3");
            //
            // lnkCharacters
            //
            this.lnkCharacters.AutoSize = true;
            this.lnkCharacters.Name = "lnkCharacters";
            this.lnkCharacters.TabIndex = 0;
            this.lnkCharacters.Text = "Characters";
            this.tw.SetClasses(this.lnkCharacters, "nav-link-active");
            //
            // lnkLocations
            //
            this.lnkLocations.AutoSize = true;
            this.lnkLocations.Name = "lnkLocations";
            this.lnkLocations.TabIndex = 1;
            this.lnkLocations.Text = "Locations";
            this.tw.SetClasses(this.lnkLocations, "nav-link");
            //
            // lnkEpisodes
            //
            this.lnkEpisodes.AutoSize = true;
            this.lnkEpisodes.Name = "lnkEpisodes";
            this.lnkEpisodes.TabIndex = 2;
            this.lnkEpisodes.Text = "Episodes";
            this.tw.SetClasses(this.lnkEpisodes, "nav-link");
            //
            // icoTheme
            //
            this.icoTheme.IconName = "moon";
            this.icoTheme.Name = "icoTheme";
            this.icoTheme.TabIndex = 3;
            this.icoTheme.Click += new System.EventHandler(this.icoTheme_Click);
            this.tw.SetClasses(this.icoTheme, "text-sm text-gray-900 cursor-pointer hover:text-blue-600 dark:text-white dark:hover:text-blue-400");
            //
            // pnlContent
            //
            this.pnlContent.Controls.Add(this.pnlToolbar);
            this.pnlContent.Controls.Add(this.pnlTableHeader);
            this.pnlContent.Controls.Add(this.pnlList);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.TabIndex = 0;
            this.tw.SetClasses(this.pnlContent, "flex flex-col gap-2 bg-white dark:bg-gray-900");
            //
            // pnlToolbar
            //
            this.pnlToolbar.Controls.Add(this.txtSearch);
            this.pnlToolbar.Controls.Add(this.btnSearch);
            this.pnlToolbar.Controls.Add(this.lnkClear);
            this.pnlToolbar.Controls.Add(this.pnlToolbarSpacer);
            this.pnlToolbar.Controls.Add(this.selRows);
            this.pnlToolbar.Controls.Add(this.selStatus);
            this.pnlToolbar.Controls.Add(this.selSpecies);
            this.pnlToolbar.Controls.Add(this.selGender);
            this.pnlToolbar.Name = "pnlToolbar";
            this.pnlToolbar.Size = new System.Drawing.Size(960, 44);
            this.pnlToolbar.TabIndex = 0;
            this.tw.SetClasses(this.pnlToolbar, "flex items-center gap-3 mb-2");
            //
            // txtSearch
            //
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.PlaceholderText = "Search by Name...";
            this.txtSearch.Size = new System.Drawing.Size(320, 30);
            this.txtSearch.TabIndex = 0;
            this.txtSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSearch_KeyDown);
            this.tw.SetClasses(this.txtSearch, "search");
            //
            // btnSearch
            //
            this.btnSearch.IconName = "search";
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(32, 32);
            this.btnSearch.TabIndex = 1;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            this.tw.SetClasses(this.btnSearch, "w-8 h-8 rounded-md text-base text-gray-900 bg-white hover:bg-gray-100 transition cursor-pointer dark:bg-gray-900 dark:text-white dark:hover:bg-gray-800");
            //
            // lnkClear
            //
            this.lnkClear.AutoSize = true;
            this.lnkClear.Name = "lnkClear";
            this.lnkClear.TabIndex = 2;
            this.lnkClear.Text = "Clear";
            this.lnkClear.Click += new System.EventHandler(this.lnkClear_Click);
            this.tw.SetClasses(this.lnkClear, "text-xs text-gray-700 cursor-pointer hover:underline dark:text-gray-300");
            //
            // pnlToolbarSpacer
            //
            this.pnlToolbarSpacer.Name = "pnlToolbarSpacer";
            this.pnlToolbarSpacer.Size = new System.Drawing.Size(10, 10);
            this.pnlToolbarSpacer.TabIndex = 3;
            this.tw.SetClasses(this.pnlToolbarSpacer, "flex-1");
            //
            // selRows
            //
            this.selRows.AutoSize = true;
            this.selRows.Items.AddRange(new string[] { "5 Row", "10 Row", "20 Row" });
            this.selRows.Name = "selRows";
            this.selRows.SelectedIndex = 2;
            this.selRows.TabIndex = 4;
            this.selRows.SelectedIndexChanged += new System.EventHandler(this.selRows_SelectedIndexChanged);
            this.tw.SetClasses(this.selRows, "pill");
            //
            // selStatus
            //
            this.selStatus.AutoSize = true;
            this.selStatus.Items.AddRange(new string[] { "All Status", "Alive", "Dead", "unknown" });
            this.selStatus.Name = "selStatus";
            this.selStatus.SelectedIndex = 1;
            this.selStatus.TabIndex = 5;
            this.selStatus.SelectedIndexChanged += new System.EventHandler(this.filter_SelectedIndexChanged);
            this.tw.SetClasses(this.selStatus, "pill");
            //
            // selSpecies
            //
            this.selSpecies.AutoSize = true;
            this.selSpecies.Items.AddRange(new string[] { "All Species", "Human", "Alien", "Humanoid", "Robot", "Animal", "Mythological Creature", "Cronenberg", "Disease", "Poopybutthole", "unknown" });
            this.selSpecies.Name = "selSpecies";
            this.selSpecies.SelectedIndex = 1;
            this.selSpecies.TabIndex = 6;
            this.selSpecies.SelectedIndexChanged += new System.EventHandler(this.filter_SelectedIndexChanged);
            this.tw.SetClasses(this.selSpecies, "pill");
            //
            // selGender
            //
            this.selGender.AutoSize = true;
            this.selGender.Items.AddRange(new string[] { "All Genders", "Male", "Female", "Genderless", "unknown" });
            this.selGender.Name = "selGender";
            this.selGender.SelectedIndex = 1;
            this.selGender.TabIndex = 7;
            this.selGender.SelectedIndexChanged += new System.EventHandler(this.filter_SelectedIndexChanged);
            this.tw.SetClasses(this.selGender, "pill");
            //
            // pnlTableHeader: satırlarla aynı kolon yapısı (avatar boşluğu + 4 eşit kolon + ok boşluğu).
            //
            this.pnlTableHeader.Controls.Add(this.pnlHeaderAvatar);
            this.pnlTableHeader.Controls.Add(this.pnlThName);
            this.pnlTableHeader.Controls.Add(this.pnlThStatus);
            this.pnlTableHeader.Controls.Add(this.pnlThSpecies);
            this.pnlTableHeader.Controls.Add(this.pnlThGender);
            this.pnlTableHeader.Controls.Add(this.pnlHeaderChevron);
            this.pnlTableHeader.Name = "pnlTableHeader";
            this.pnlTableHeader.Size = new System.Drawing.Size(960, 30);
            this.pnlTableHeader.TabIndex = 1;
            this.tw.SetClasses(this.pnlTableHeader, "flex items-center border-b border-gray-800 dark:border-gray-600");
            //
            // pnlHeaderAvatar
            //
            this.pnlHeaderAvatar.Name = "pnlHeaderAvatar";
            this.pnlHeaderAvatar.Size = new System.Drawing.Size(88, 10);
            this.pnlHeaderAvatar.TabIndex = 0;
            this.tw.SetClasses(this.pnlHeaderAvatar, "w-22 shrink-0");
            //
            // pnlThName
            //
            this.pnlThName.Controls.Add(this.lblThName);
            this.pnlThName.Name = "pnlThName";
            this.pnlThName.TabIndex = 1;
            this.tw.SetClasses(this.pnlThName, "flex-1 flex justify-center");
            //
            // lblThName
            //
            this.lblThName.AutoSize = true;
            this.lblThName.Name = "lblThName";
            this.lblThName.Text = "• Name";
            this.tw.SetClasses(this.lblThName, "th");
            //
            // pnlThStatus
            //
            this.pnlThStatus.Controls.Add(this.lblThStatus);
            this.pnlThStatus.Name = "pnlThStatus";
            this.pnlThStatus.TabIndex = 2;
            this.tw.SetClasses(this.pnlThStatus, "flex-1 flex justify-center");
            //
            // lblThStatus
            //
            this.lblThStatus.AutoSize = true;
            this.lblThStatus.Name = "lblThStatus";
            this.lblThStatus.Text = "• Status";
            this.tw.SetClasses(this.lblThStatus, "th");
            //
            // pnlThSpecies
            //
            this.pnlThSpecies.Controls.Add(this.lblThSpecies);
            this.pnlThSpecies.Name = "pnlThSpecies";
            this.pnlThSpecies.TabIndex = 3;
            this.tw.SetClasses(this.pnlThSpecies, "flex-1 flex justify-center");
            //
            // lblThSpecies
            //
            this.lblThSpecies.AutoSize = true;
            this.lblThSpecies.Name = "lblThSpecies";
            this.lblThSpecies.Text = "• Species";
            this.tw.SetClasses(this.lblThSpecies, "th");
            //
            // pnlThGender
            //
            this.pnlThGender.Controls.Add(this.lblThGender);
            this.pnlThGender.Name = "pnlThGender";
            this.pnlThGender.TabIndex = 4;
            this.tw.SetClasses(this.pnlThGender, "flex-1 flex justify-center");
            //
            // lblThGender
            //
            this.lblThGender.AutoSize = true;
            this.lblThGender.Name = "lblThGender";
            this.lblThGender.Text = "Gender";
            this.tw.SetClasses(this.lblThGender, "th");
            //
            // pnlHeaderChevron
            //
            this.pnlHeaderChevron.Name = "pnlHeaderChevron";
            this.pnlHeaderChevron.Size = new System.Drawing.Size(24, 10);
            this.pnlHeaderChevron.TabIndex = 5;
            this.tw.SetClasses(this.pnlHeaderChevron, "w-6 shrink-0");
            //
            // pnlList: karakter satırları koddan eklenir.
            //
            this.pnlList.AutoScroll = true;
            this.pnlList.Controls.Add(this.lblMessage);
            this.pnlList.Name = "pnlList";
            this.pnlList.TabIndex = 2;
            this.tw.SetClasses(this.pnlList, "flex-1 flex flex-col bg-white dark:bg-gray-900");
            //
            // lblMessage: yükleniyor / sonuç yok / hata
            //
            this.lblMessage.AutoSize = true;
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Text = "Loading characters...";
            this.tw.SetClasses(this.lblMessage, "self-center mt-10 text-sm text-gray-500 dark:text-gray-400");
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 820);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(900, 500);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Rickipedia";
            this.tw.SetClasses(this, "bg-white dark:bg-gray-900 caption:bg-white caption:text-gray-900 dark:caption:bg-gray-900 dark:caption:text-gray-100");
            ((System.ComponentModel.ISupportInitialize)(this.tw)).EndInit();
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlBrand.ResumeLayout(false);
            this.pnlBrand.PerformLayout();
            this.pnlNav.ResumeLayout(false);
            this.pnlNav.PerformLayout();
            this.pnlToolbar.ResumeLayout(false);
            this.pnlToolbar.PerformLayout();
            this.pnlTableHeader.ResumeLayout(false);
            this.pnlTableHeader.PerformLayout();
            this.pnlList.ResumeLayout(false);
            this.pnlList.PerformLayout();
            this.pnlContent.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private FormWind.TailwindProvider tw;
        private FormWind.TwPanel pnlHeader;
        private FormWind.TwPanel pnlBrand;
        private FormWind.TwPictureBox picLogo;
        private System.Windows.Forms.Label lblBrand;
        private FormWind.TwPanel pnlNav;
        private System.Windows.Forms.Label lnkCharacters;
        private System.Windows.Forms.Label lnkLocations;
        private System.Windows.Forms.Label lnkEpisodes;
        private FormWind.TwIcon icoTheme;
        private FormWind.TwPanel pnlContent;
        private FormWind.TwPanel pnlToolbar;
        private FormWind.TwTextBox txtSearch;
        private FormWind.TwButton btnSearch;
        private System.Windows.Forms.Label lnkClear;
        private FormWind.TwPanel pnlToolbarSpacer;
        private FormWind.TwSelect selRows;
        private FormWind.TwSelect selStatus;
        private FormWind.TwSelect selSpecies;
        private FormWind.TwSelect selGender;
        private FormWind.TwPanel pnlTableHeader;
        private FormWind.TwPanel pnlHeaderAvatar;
        private FormWind.TwPanel pnlThName;
        private System.Windows.Forms.Label lblThName;
        private FormWind.TwPanel pnlThStatus;
        private System.Windows.Forms.Label lblThStatus;
        private FormWind.TwPanel pnlThSpecies;
        private System.Windows.Forms.Label lblThSpecies;
        private FormWind.TwPanel pnlThGender;
        private System.Windows.Forms.Label lblThGender;
        private FormWind.TwPanel pnlHeaderChevron;
        private FormWind.TwPanel pnlList;
        private System.Windows.Forms.Label lblMessage;
    }
}
