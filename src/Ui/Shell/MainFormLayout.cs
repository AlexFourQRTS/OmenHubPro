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
        private readonly string[] pageNames = { "Omen", "Подсветка", "Лаборатория" };

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
            titleLabel = PageKit.MakeLabel(header, "", 28, 16, 17, true, PageKit.RoleText);
            statusLabel = PageKit.MakeLabel(header, "", 28, 44, 8.5f, false, PageKit.RoleSub);
            BuildFanChips();
            BuildHeaderButtons();
        }

        private IconButton themeButton, autostartButton;

        private void BuildHeaderButtons()
        {
            themeButton = new IconButton { Icon = IconButton.Glyph.Theme, Top = 12, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            autostartButton = new IconButton { Icon = IconButton.Glyph.Power, Top = 12, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            header.Controls.Add(themeButton);
            header.Controls.Add(autostartButton);
            var hints = new ToolTip();
            hints.SetToolTip(themeButton, "Светлая / тёмная тема");
            hints.SetToolTip(autostartButton, "Автозапуск с Windows");
            EventHandler place = delegate
            {
                themeButton.Left = header.Width - 100;
                autostartButton.Left = header.Width - 52;
            };
            header.Resize += place;
            Load += place;
            themeButton.IsOn = AppSettings.DarkTheme;
            autostartButton.IsOn = OmenHub.Startup.AutostartTask.IsEnabled();
            themeButton.Click += delegate
            {
                AppSettings.DarkTheme = !AppSettings.DarkTheme;
                Palette.Apply(AppSettings.DarkTheme);
                ThemeApplier.Apply(this);
                themeButton.IsOn = AppSettings.DarkTheme;
                AppSettings.MarkChanged();
            };
            autostartButton.Click += delegate
            {
                if (OmenHub.Startup.AutostartTask.IsEnabled()) OmenHub.Startup.AutostartTask.Disable();
                else OmenHub.Startup.AutostartTask.Enable();
                autostartButton.IsOn = OmenHub.Startup.AutostartTask.IsEnabled();
                autostartButton.Invalidate();
            };
        }

        private void BuildFanChips()
        {
            cpuFanChip = new StatChip { Title = "КУЛЕР CPU", Top = 8, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            gpuFanChip = new StatChip { Title = "КУЛЕР GPU", Top = 8, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            header.Controls.Add(cpuFanChip);
            header.Controls.Add(gpuFanChip);
            EventHandler placeChips = delegate
            {
                cpuFanChip.Left = header.Width - 470;
                gpuFanChip.Left = header.Width - 286;
            };
            header.Resize += placeChips;
            Load += placeChips;
        }


        private void BuildPages()
        {
            pages = new PageBase[] { new OmenPage(), new LightingPage() };
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
