using System;
using System.Drawing;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Bios;
using OmenHub.Hardware.Gpu;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>
    /// GPU power target in watts (30-130, step 5), held by the governor through the graphics clock ceiling.
    /// Touching it while "Pro auto" is on switches the profile to manual and keeps the profile's values.
    /// </summary>
    public sealed class GpuPowerCard : CardPanel
    {
        private readonly SegmentedControl limitSegments;
        private readonly ValueSlider wattsSlider;
        private readonly Label wattsLabel;
        private readonly Label liveLabel;
        private readonly Label biosLabel;

        /// <summary>Raised when the card left the Pro auto profile, so the other power cards can refresh.</summary>
        public event EventHandler ProfileLeft;

        public GpuPowerCard(int top)
        {
            Left = 24; Top = top; Width = 802; Height = 124;
            PageKit.MakeLabel(this, "GPU power", 20, 12, 11, true, PageKit.RoleText);
            limitSegments = PageKit.MakeSegments(this, 20, 42, 220, new[] { "Без лимита", "Лимит" }, AppSettings.GpuLimitEnabled ? 1 : 0);
            wattsSlider = PageKit.MakeSlider(this, 260, 46, 420, AppSettings.MinimumGpuWatts, AppSettings.MaximumGpuWatts,
                AppSettings.GpuWattsStep, AppSettings.GpuWatts);
            wattsLabel = PageKit.MakeValueLabel(this, 690, 46, 92, 10.5f, ContentAlignment.MiddleRight);
            liveLabel = PageKit.MakeLabel(this, "", 20, 86, 9, false, PageKit.RoleSub);
            biosLabel = PageKit.MakeLabel(this, "", 20, 102, 8.5f, false, PageKit.RoleSub);

            limitSegments.SelectionChanged += delegate
            {
                LeaveProfile();
                AppSettings.GpuLimitEnabled = limitSegments.SelectedIndex == 1;
                AppSettings.MarkChanged();
            };
            wattsSlider.ValueChanged += delegate
            {
                LeaveProfile();
                AppSettings.GpuWatts = wattsSlider.Value;
                AppSettings.GpuLimitEnabled = true;
                limitSegments.SelectedIndex = 1;
                wattsLabel.Text = wattsSlider.Value + " Вт";
                AppSettings.MarkChanged();
            };
            LoadFromSettings();
        }

        public void LoadFromSettings()
        {
            bool profileOn = AppSettings.ProAutoEnabled;
            limitSegments.SelectedIndex = (profileOn || AppSettings.GpuLimitEnabled) ? 1 : 0;
            wattsSlider.Value = profileOn ? AppSettings.ProAutoGpuWatts : AppSettings.GpuWatts;
            wattsLabel.Text = wattsSlider.Value + " Вт";
            biosLabel.Text = "Режим BIOS:  " + BiosSystemCommands.DescribeGpuPowerMode();
        }

        public void ShowLive()
        {
            double watts = GpuPowerGovernor.MeasuredWatts;
            int cap = GpuPowerGovernor.LockedCapMhz;
            liveLabel.Text = "Сейчас: " + (watts < 0 ? "—" : watts.ToString("0.0") + " Вт")
                + "    потолок частоты: " + (cap == 0 ? "нет" : cap + " МГц");
        }

        private void LeaveProfile()
        {
            if (!AppSettings.ProAutoEnabled) return;
            AppSettings.LeaveProAuto();
            if (ProfileLeft != null) ProfileLeft(this, EventArgs.Empty);
        }
    }
}
