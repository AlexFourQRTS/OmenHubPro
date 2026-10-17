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
        private SegmentedControl labSegments, overlaySegments;
        private readonly Label autostartResultLabel;

        /// <summary>Raised when the user switches the fan mode back to BIOS auto from here.</summary>
        public event System.EventHandler FansHandedBack;

        /// <summary>Raised when the lighting laboratory was switched on or off, so the menu can show or hide it.</summary>
        public event System.EventHandler LabVisibilityChanged;

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

            BuildExtrasCard();
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

        private void BuildExtrasCard()
        {
            CardPanel extras = PageKit.MakeCard(this, 24, 478, 802, 140, "Дополнительно");
            PageKit.MakeLabel(extras, "Оверлей в играх", 20, 100, 9.5f, false, PageKit.RoleSub);
            overlaySegments = PageKit.MakeSegments(extras, 220, 94, 260, new[] { "Выключен", "Включён" }, AppSettings.OverlayEnabled ? 1 : 0);
            PageKit.MakeLabel(extras, "Внешний вид оверлея — на вкладке «Оверлей».", 500, 102, 8.5f, false, PageKit.RoleSub);
            overlaySegments.SelectionChanged += delegate { AppSettings.OverlayEnabled = overlaySegments.SelectedIndex == 1; AppSettings.MarkChanged(); };
            PageKit.MakeLabel(extras, "Лаборатория подсветки", 20, 52, 9.5f, false, PageKit.RoleSub);
            labSegments = PageKit.MakeSegments(extras, 220, 46, 260, new[] { "Выключена", "Включена" }, AppSettings.LabEnabled ? 1 : 0);
            PageKit.MakeLabel(extras, "Пронумерованные способы покраски и поиск ламп — для отладки; появляется в меню.", 500, 54, 8.5f, false, PageKit.RoleSub);
            labSegments.SelectionChanged += delegate
            {
                AppSettings.LabEnabled = labSegments.SelectedIndex == 1;
                AppSettings.MarkChanged();
                if (LabVisibilityChanged != null) LabVisibilityChanged(this, System.EventArgs.Empty);
            };
        }

        public override void LoadFromSettings()
        {
            themeSegments.SelectedIndex = AppSettings.DarkTheme ? 0 : 1;
            labSegments.SelectedIndex = AppSettings.LabEnabled ? 1 : 0;
            overlaySegments.SelectedIndex = AppSettings.OverlayEnabled ? 1 : 0;
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
