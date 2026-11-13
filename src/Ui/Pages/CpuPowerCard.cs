using System;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Power;
using OmenHub.Ui.Controls;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Pages
{
    /// <summary>
    /// CPU power: one of 20 / 40 / 50 / 60 W, applied by the engine within a moment.
    /// Choosing a value while "Pro auto" is on switches the profile to manual and keeps the profile's GPU value.
    /// When the processor cannot be reached (driver missing or blocked) a short red note says so.
    /// </summary>
    public sealed class CpuPowerCard : CardPanel
    {
        private readonly SegmentedControl wattSegments;
        private readonly Label statusLabel;

        /// <summary>Raised when the card left the Pro auto profile, so the other power cards can refresh.</summary>
        public event EventHandler ProfileLeft;

        public CpuPowerCard(int top)
        {
            Left = 24; Top = top; Width = 802; Height = 112;
            PageKit.MakeLabel(this, "CPU power", 20, 12, 11, true, PageKit.RoleText);
            wattSegments = PageKit.MakeSegments(this, 20, 46, 480, PowerChoices.Captions(AppSettings.CpuWattChoices), 0);
            statusLabel = PageKit.MakeLabel(this, "", 20, 88, 9, true, PageKit.RoleText);
            LoadFromSettings();
            wattSegments.SelectionChanged += delegate
            {
                bool leftProfile = AppSettings.ProAutoEnabled;
                if (leftProfile) AppSettings.LeaveProAuto();
                AppSettings.CpuWatts = AppSettings.CpuWattChoices[wattSegments.SelectedIndex];
                AppSettings.MarkChanged();
                if (leftProfile && ProfileLeft != null) ProfileLeft(this, EventArgs.Empty);
            };
        }

        public void LoadFromSettings()
        {
            int watts = AppSettings.ProAutoEnabled ? AppSettings.ProAutoWatts : AppSettings.CpuWatts;
            wattSegments.SelectedIndex = Array.IndexOf(AppSettings.CpuWattChoices, AppSettings.Snap(AppSettings.CpuWattChoices, watts));
        }

        /// <summary>Called once per second: shows a red note while the processor limit cannot be applied.</summary>
        public void ShowStatus()
        {
            statusLabel.Text = RyzenAdjTool.DriverFailed ? "Нет доступа к процессору (драйвер недоступен)" : "";
            statusLabel.ForeColor = Palette.Bad;
        }
    }
}
