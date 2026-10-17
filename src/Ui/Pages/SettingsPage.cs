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
            CardPanel appearance = PageKit.MakeCard(this, 24, 16, 802, 220, "Настройки");
            PageKit.MakeLabel(appearance, "Тема", 20, 58, 9.5f, false, PageKit.RoleSub);
            themeSegments = PageKit.MakeSegments(appearance, 120, 52, 260, new[] { "Тёмная", "Светлая" }, AppSettings.DarkTheme ? 0 : 1);
            themeSegments.SelectionChanged += delegate
            {
                AppSettings.DarkTheme = themeSegments.SelectedIndex == 0;
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
            PageKit.MakeLabel(appearance, "При выходе из программы вентиляторы автоматически возвращаются в режим BIOS.\nНастройки: %APPDATA%\\OmenHub\\settings.ini",
                20, 170, 9, false, PageKit.RoleSub);

            CardPanel startup = PageKit.MakeCard(this, 24, 252, 802, 210, "Запуск и приоритет");
            PageKit.MakeLabel(startup, "Автозапуск", 20, 58, 9.5f, false, PageKit.RoleSub);
            autostartSegments = PageKit.MakeSegments(startup, 140, 52, 360, new[] { "Выключен", "При входе в Windows" }, AutostartTask.IsEnabled() ? 1 : 0);
            autostartResultLabel = PageKit.MakeLabel(startup, "", 520, 60, 9, false, PageKit.RoleSub);
            autostartSegments.SelectionChanged += delegate { ChangeAutostart(); };
            PageKit.MakeLabel(startup, "Запускается от имени администратора без запроса UAC (задача с наивысшими правами), работает и от батареи.\nПри автозапуске программа стартует сразу в трее.",
                20, 106, 9, false, PageKit.RoleSub);
            PageKit.MakeLabel(startup, "Приоритет: процесс — «Высокий», поток управления кулерами — наивысший, «режим эффективности» Windows отключён.\nReal-time намеренно не используется: он может подвесить систему.",
                20, 150, 9, false, PageKit.RoleSub);
        }

        public override void LoadFromSettings()
        {
            themeSegments.SelectedIndex = AppSettings.DarkTheme ? 0 : 1;
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
