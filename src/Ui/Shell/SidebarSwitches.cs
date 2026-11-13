using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Startup;
using OmenHub.Ui.Controls;
using OmenHub.Ui.Pages;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Shell
{
    /// <summary>Two switches at the bottom of the sidebar: theme (light / dark) and autostart with Windows.</summary>
    public sealed class SidebarSwitches
    {
        private const int ButtonWidth = 170;
        private const int ButtonLeft = 20;
        private readonly ActionButton themeButton;
        private readonly ActionButton autostartButton;

        public SidebarSwitches(Panel sidebar, Control themeRoot)
        {
            themeButton = PageKit.MakeButton(sidebar, "", ButtonLeft, sidebar.Parent.ClientSize.Height - 100, ButtonWidth, false);
            autostartButton = PageKit.MakeButton(sidebar, "", ButtonLeft, sidebar.Parent.ClientSize.Height - 56, ButtonWidth, false);
            themeButton.Anchor = autostartButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            ShowTexts();

            themeButton.Click += delegate
            {
                AppSettings.DarkTheme = !AppSettings.DarkTheme;
                Palette.Apply(AppSettings.DarkTheme);
                ThemeApplier.Apply(themeRoot);
                AppSettings.MarkChanged();
                ShowTexts();
            };
            autostartButton.Click += delegate
            {
                if (AutostartTask.IsEnabled()) AutostartTask.Disable(); else AutostartTask.Enable();
                ShowTexts();
            };
        }

        private void ShowTexts()
        {
            themeButton.Text = "Тема: " + (AppSettings.DarkTheme ? "тёмная" : "светлая");
            autostartButton.Text = "Автозапуск: " + (AutostartTask.IsEnabled() ? "вкл" : "выкл");
            themeButton.Invalidate();
            autostartButton.Invalidate();
        }
    }
}
