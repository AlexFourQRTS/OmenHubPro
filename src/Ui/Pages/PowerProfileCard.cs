using System;
using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>Card with the automatic CPU profiles: "Pro auto" on AC and the economy limit on battery.</summary>
    public sealed class PowerProfileCard : CardPanel
    {
        private readonly SegmentedControl proAutoSegments;
        private readonly SegmentedControl batterySegments;

        /// <summary>Raised when a profile was switched, so the CPU card can refresh its slider.</summary>
        public event EventHandler ProfileChanged;

        public PowerProfileCard()
        {
            Left = 24; Top = 16; Width = 802; Height = 140;
            PageKit.MakeLabel(this, "Профиль мощности", 20, 14, 11, true, PageKit.RoleText);
            proAutoSegments = PageKit.MakeSegments(this, 20, 50, 340,
                new[] { "Вручную", "Про авто (CPU " + AppSettings.ProAutoWatts + " Вт)" }, AppSettings.ProAutoEnabled ? 1 : 0);

            PageKit.MakeLabel(this, "От батареи", 20, 100, 9.5f, false, PageKit.RoleSub);
            batterySegments = PageKit.MakeSegments(this, 140, 94, 340,
                new[] { "Без изменений", "CPU " + AppSettings.BatteryWatts + " / GPU " + AppSettings.BatteryGpuWatts + " Вт (экономия)" }, AppSettings.BatteryEcoEnabled ? 1 : 0);

            proAutoSegments.SelectionChanged += delegate { OnProAutoChosen(); };
            batterySegments.SelectionChanged += delegate { OnBatteryChosen(); };
        }

        public void LoadFromSettings()
        {
            proAutoSegments.SelectedIndex = AppSettings.ProAutoEnabled ? 1 : 0;
            batterySegments.SelectedIndex = AppSettings.BatteryEcoEnabled ? 1 : 0;
        }

        private void OnProAutoChosen()
        {
            AppSettings.ProAutoEnabled = proAutoSegments.SelectedIndex == 1;
            if (AppSettings.ProAutoEnabled) AppSettings.CpuWatts = AppSettings.ProAutoWatts;
            AppSettings.MarkChanged();
            if (ProfileChanged != null) ProfileChanged(this, EventArgs.Empty);
        }

        private void OnBatteryChosen()
        {
            AppSettings.BatteryEcoEnabled = batterySegments.SelectedIndex == 1;
            AppSettings.MarkChanged();
        }
    }
}
