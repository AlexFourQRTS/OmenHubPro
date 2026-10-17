using System;
using System.Drawing;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;
using OmenHub.Ui.Pages;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Shell
{
    /// <summary>Construction of the window frame: sidebar, header with fan chips, and the pages.</summary>
    public sealed partial class MainForm
    {
        private const int SidebarWidth = 210;
        private const int HeaderHeight = 64;

        private Panel sidebar, header;
        private Label titleLabel, statusLabel;
        private StatChip cpuFanChip, gpuFanChip;
        private PageBase[] pages;
        private NavButton[] navButtons;
        private readonly string[] pageNames = { "Обзор", "Вентиляторы", "Мощность", "Подсветка", "Оверлей", "Настройки", "Лаборатория" };

        private void BuildFrame()
        {
            Text = "OmenHub";
            ClientSize = new Size(1060, 700);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            sidebar = new Panel { Dock = DockStyle.Left, Width = SidebarWidth, Tag = ThemeApplier.RoleSidebar };
            header = new Panel { Dock = DockStyle.Top, Height = HeaderHeight, Tag = ThemeApplier.RoleHeader };
            Controls.Add(sidebar);
            Controls.Add(header);

            PageKit.MakeLabel(sidebar, "OMEN HUB", 24, 22, 15, true, PageKit.RoleText);
            PageKit.MakeLabel(sidebar, "OMEN MAX 16", 26, 54, 8.5f, false, PageKit.RoleSub);
            titleLabel = PageKit.MakeLabel(header, "", 28, 16, 17, true, PageKit.RoleText);
            statusLabel = PageKit.MakeLabel(header, "", 28, 44, 8.5f, false, PageKit.RoleSub);
            BuildFanChips();
        }

        private void BuildFanChips()
        {
            cpuFanChip = new StatChip { Title = "КУЛЕР CPU", Top = 8, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            gpuFanChip = new StatChip { Title = "КУЛЕР GPU", Top = 8, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            header.Controls.Add(cpuFanChip);
            header.Controls.Add(gpuFanChip);
            EventHandler placeChips = delegate
            {
                cpuFanChip.Left = header.Width - 370;
                gpuFanChip.Left = header.Width - 186;
            };
            header.Resize += placeChips;
            Load += placeChips;
        }

        private int LabPageIndex { get { return pages.Length - 1; } }

        /// <summary>The laboratory is the last page and is listed in the menu only when enabled in the settings.</summary>
        private void ApplyLabVisibility()
        {
            navButtons[LabPageIndex].Visible = AppSettings.LabEnabled;
            if (!AppSettings.LabEnabled && currentPage == LabPageIndex) ShowPage(0);
        }

        private void BuildPages()
        {
            var settingsPage = new SettingsPage(this);
            settingsPage.FansHandedBack += delegate { ShowPage(currentPage); };
            settingsPage.LabVisibilityChanged += delegate { ApplyLabVisibility(); };
            pages = new PageBase[] { new OverviewPage(), new FansPage(), new PowerPage(), new LightingPage(), new OverlayPage(), settingsPage, new LightingLabPage() };
            navButtons = new NavButton[pages.Length];
            for (int index = 0; index < pages.Length; index++)
            {
                int pageIndex = index;
                var button = new NavButton { Text = pageNames[index], Left = 0, Top = 100 + index * 46, Width = SidebarWidth };
                button.Click += delegate { ShowPage(pageIndex); };
                sidebar.Controls.Add(button);
                navButtons[index] = button;

                pages[index].Left = SidebarWidth;
                pages[index].Top = HeaderHeight;
                Controls.Add(pages[index]);
            }
        }
    }
}
