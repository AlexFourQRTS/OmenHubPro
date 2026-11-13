using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Bios;
using OmenHub.Startup;
using OmenHub.Ui.Controls;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Pages
{
    /// <summary>Theme, fan hand-back, autostart and priority information.</summary>
    public sealed class SettingsPage : PageBase
    {
        private readonly SegmentedControl themeSegments;
        private readonly SegmentedControl autostartSegments;
        private readonly Label autostartResultLabel;

        /// <summary>Raised when the user switches the fan mode back to BIOS auto from here.</summary>
        public event System.EventHandler FansHandedBack;


        public SettingsPage(Control themeRoot)
        {
            CardPanel appearance = PageKit.MakeCard(this, 24, 16, 802, 170, "Настройки");
            PageKit.MakeLabel(appearance, "Тема", 20, 58, 9.5f, false, PageKit.RoleSub);
            themeSegments = PageKit.MakeSegments(appearance, 120, 52, 260, new[] { "Светлая", "Тёмная" }, AppSettings.DarkTheme ? 1 : 0);
            themeSegments.SelectionChanged += delegate
            {
                AppSettings.DarkTheme = themeSegments.SelectedIndex == 1;
                Palette.Apply(AppSettings.DarkTheme);
                ThemeApplier.Apply(themeRoot);
                AppSettings.MarkChanged();
            };
            ActionButton handBack = PageKit.MakeButton(appearance, "Вернуть вентиляторы под контроль BIOS", 20, 112, 330, false);
            handBack.Click += delegate
            {
                AppSettings.Mode = FanMode.Auto;
                BiosFanCommands.SetFanMax(false);
                AppSettings.MarkChanged();
                if (FansHandedBack != null) FansHandedBack(this, System.EventArgs.Empty);
            };

            CardPanel startup = PageKit.MakeCard(this, 24, 202, 802, 100, "Запуск");
            PageKit.MakeLabel(startup, "Автозапуск", 20, 58, 9.5f, false, PageKit.RoleSub);
            autostartSegments = PageKit.MakeSegments(startup, 140, 52, 360, new[] { "Выключен", "При входе в Windows" }, AutostartTask.IsEnabled() ? 1 : 0);
            autostartResultLabel = PageKit.MakeLabel(startup, "", 520, 60, 9, false, PageKit.RoleSub);
            autostartSegments.SelectionChanged += delegate { ChangeAutostart(); };
        }


        public override void LoadFromSettings()
        {
            themeSegments.SelectedIndex = AppSettings.DarkTheme ? 1 : 0;
        }

        private void ChangeAutostart()
        {
            string error = autostartSegments.SelectedIndex == 1 ? AutostartTask.Enable() : AutostartTask.Disable();
            bool enabled = AutostartTask.IsEnabled();
            autostartSegments.SelectedIndex = enabled ? 1 : 0;
            autostartResultLabel.Text = error.Length == 0
                ? (enabled ? "Включено: задача планировщика «OmenHub»" : "Выключено")
                : PageKit.Shorten(error, 70);
        }
    }
}
