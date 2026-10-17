using System;
using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>
    /// One automatic power profile. "From mains" can be switched between manual limits and Pro auto (CPU 40 / GPU 110 W).
    /// "From battery" always runs the economy profile (CPU 20 / GPU 30 W): it is shown as a single, fixed choice.
    /// </summary>
    public sealed class PowerProfileCard : CardPanel
    {
        private readonly bool forBattery;
        private readonly SegmentedControl segments;

        /// <summary>Raised when the mains profile was switched, so the CPU and GPU cards can refresh their sliders.</summary>
        public event EventHandler ProfileChanged;

        public PowerProfileCard(bool forBattery, int top)
        {
            this.forBattery = forBattery;
            Left = 24; Top = top; Width = 802; Height = 92;
            PageKit.MakeLabel(this, forBattery ? "Профиль мощности: от батареи" : "Профиль мощности: от сети", 20, 12, 11, true, PageKit.RoleText);
            string[] items = forBattery
                ? new[] { "Экономия: CPU " + AppSettings.BatteryWatts + " / GPU " + AppSettings.BatteryGpuWatts + " Вт" }
                : new[] { "Вручную", "Про авто: CPU " + AppSettings.ProAutoWatts + " / GPU " + AppSettings.ProAutoGpuWatts + " Вт" };
            segments = PageKit.MakeSegments(this, 20, 42, forBattery ? 340 : 460, items, 0);
            if (!forBattery) segments.SelectionChanged += delegate { OnMainsChosen(); };
            LoadFromSettings();
        }

        public void LoadFromSettings()
        {
            if (forBattery) segments.SelectedIndex = 0;   // the only choice
            else segments.SelectedIndex = AppSettings.ProAutoEnabled ? 1 : 0;
        }

        private void OnMainsChosen()
        {
            AppSettings.ProAutoEnabled = segments.SelectedIndex == 1;
            if (AppSettings.ProAutoEnabled) AppSettings.CpuWatts = AppSettings.ProAutoWatts;
            AppSettings.MarkChanged();
            if (ProfileChanged != null) ProfileChanged(this, EventArgs.Empty);
        }
    }
}
