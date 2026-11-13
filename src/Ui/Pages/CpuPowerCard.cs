using System;
using System.Drawing;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Power;
using OmenHub.Ui.Controls;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Pages
{
    /// <summary>
    /// CPU power limit (20-75 W, step 5), laid out like the GPU card and applied by the engine within a moment.
    /// Touching it while "Pro auto" is on switches the profile to manual and keeps the profile's values.
    /// When the processor cannot be reached (driver missing or blocked) a short red note says so.
    /// </summary>
    public sealed class CpuPowerCard : CardPanel
    {
        private readonly SegmentedControl limitSegments;
        private readonly ValueSlider wattsSlider;
        private readonly Label wattsLabel;
        private readonly Label statusLabel;

        /// <summary>Raised when the card left the Pro auto profile, so the other power cards can refresh.</summary>
        public event EventHandler ProfileLeft;

        public CpuPowerCard(int top)
        {
            Left = 24; Top = top; Width = 802; Height = 112;
            PageKit.MakeLabel(this, "CPU power", 20, 12, 11, true, PageKit.RoleText);
            limitSegments = PageKit.MakeSegments(this, 20, 42, 220, new[] { "Без лимита", "Лимит" }, AppSettings.CpuLimitEnabled ? 1 : 0);
            wattsSlider = PageKit.MakeSlider(this, 260, 46, 420, AppSettings.MinimumCpuWatts, AppSettings.MaximumCpuWatts,
                AppSettings.CpuWattsStep, AppSettings.CpuWatts);
            wattsLabel = PageKit.MakeValueLabel(this, 690, 46, 92, 10.5f, ContentAlignment.MiddleRight);
            statusLabel = PageKit.MakeLabel(this, "", 20, 88, 9, true, PageKit.RoleText);

            limitSegments.SelectionChanged += delegate
            {
                LeaveProfile();
                AppSettings.CpuLimitEnabled = limitSegments.SelectedIndex == 1;
                AppSettings.MarkChanged();
            };
            wattsSlider.ValueChanged += delegate
            {
                LeaveProfile();
                AppSettings.CpuWatts = wattsSlider.Value;
                AppSettings.CpuLimitEnabled = true;
                limitSegments.SelectedIndex = 1;
                wattsLabel.Text = wattsSlider.Value + " Вт";
                AppSettings.MarkChanged();
            };
            LoadFromSettings();
        }

        public void LoadFromSettings()
        {
            bool profileOn = AppSettings.ProAutoEnabled;
            limitSegments.SelectedIndex = (profileOn || AppSettings.CpuLimitEnabled) ? 1 : 0;
            wattsSlider.Value = profileOn ? AppSettings.ProAutoWatts : AppSettings.CpuWatts;
            wattsLabel.Text = wattsSlider.Value + " Вт";
        }

        /// <summary>Called once per second: shows a red note while the processor limit cannot be applied.</summary>
        public void ShowStatus()
        {
            statusLabel.Text = RyzenAdjTool.DriverFailed ? "Нет доступа к процессору (драйвер недоступен)" : "";
            statusLabel.ForeColor = Palette.Bad;
        }

        private void LeaveProfile()
        {
            if (!AppSettings.ProAutoEnabled) return;
            AppSettings.LeaveProAuto();
            if (ProfileLeft != null) ProfileLeft(this, EventArgs.Empty);
        }
    }
}
