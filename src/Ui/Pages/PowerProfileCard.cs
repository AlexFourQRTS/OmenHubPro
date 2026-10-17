using System;
using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>
    /// One automatic power profile: "Профиль мощности: от сети" (Pro auto, CPU 40 W) or "от батареи" (economy,
    /// CPU 20 W and GPU 30 W). The two cards share this class and differ only in what they switch.
    /// </summary>
    public sealed class PowerProfileCard : CardPanel
    {
        private readonly bool forBattery;
        private readonly SegmentedControl segments;

        /// <summary>Raised when a profile was switched, so the CPU card can refresh its slider.</summary>
        public event EventHandler ProfileChanged;

        public PowerProfileCard(bool forBattery, int top)
        {
            this.forBattery = forBattery;
            Left = 24; Top = top; Width = 802; Height = 92;
            PageKit.MakeLabel(this, forBattery ? "Профиль мощности: от батареи" : "Профиль мощности: от сети", 20, 12, 11, true, PageKit.RoleText);
            string[] items = forBattery
                ? new[] { "Без изменений", "Экономия: CPU " + AppSettings.BatteryWatts + " / GPU " + AppSettings.BatteryGpuWatts + " Вт" }
                : new[] { "Вручную", "Про авто: CPU " + AppSettings.ProAutoWatts + " / GPU " + AppSettings.ProAutoGpuWatts + " Вт" };
            segments = PageKit.MakeSegments(this, 20, 42, 460, items, 0);
            segments.SelectionChanged += delegate { OnChosen(); };
            LoadFromSettings();
        }

        public void LoadFromSettings()
        {
            segments.SelectedIndex = (forBattery ? AppSettings.BatteryEcoEnabled : AppSettings.ProAutoEnabled) ? 1 : 0;
        }

        private void OnChosen()
        {
            bool enabled = segments.SelectedIndex == 1;
            if (forBattery) AppSettings.BatteryEcoEnabled = enabled;
            else
            {
                AppSettings.ProAutoEnabled = enabled;
                if (enabled) AppSettings.CpuWatts = AppSettings.ProAutoWatts;
            }
            AppSettings.MarkChanged();
            if (ProfileChanged != null) ProfileChanged(this, EventArgs.Empty);
        }
    }
}
