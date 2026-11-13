using System;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Bios;
using OmenHub.Hardware.Gpu;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>
    /// GPU power: one of 50 / 80 / 100 / 130 W, held by the governor through the graphics clock ceiling.
    /// </summary>
    public sealed class GpuPowerCard : CardPanel
    {
        private readonly SegmentedControl wattSegments;
        private readonly Label liveLabel;
        private readonly Label biosLabel;


        public GpuPowerCard(int top)
        {
            Left = 24; Top = top; Width = 802; Height = 124;
            PageKit.MakeLabel(this, "GPU power", 20, 12, 11, true, PageKit.RoleText);
            wattSegments = PageKit.MakeSegments(this, 20, 46, 480, PowerChoices.Captions(AppSettings.GpuWattChoices), 0);
            liveLabel = PageKit.MakeLabel(this, "", 20, 86, 9, false, PageKit.RoleSub);
            biosLabel = PageKit.MakeLabel(this, "", 20, 102, 8.5f, false, PageKit.RoleSub);
            LoadFromSettings();
            wattSegments.SelectionChanged += delegate
            {
                AppSettings.GpuWatts = AppSettings.GpuWattChoices[wattSegments.SelectedIndex];
                AppSettings.MarkChanged();
            };
        }

        public void LoadFromSettings()
        {
            int watts = AppSettings.GpuWatts;
            wattSegments.SelectedIndex = Array.IndexOf(AppSettings.GpuWattChoices, AppSettings.Snap(AppSettings.GpuWattChoices, watts));
            biosLabel.Text = "Режим BIOS:  " + BiosSystemCommands.DescribeGpuPowerMode();
        }

        public void ShowLive()
        {
            double watts = GpuPowerGovernor.MeasuredWatts;
            int cap = GpuPowerGovernor.LockedCapMhz;
            liveLabel.Text = "Сейчас: " + (watts < 0 ? "—" : watts.ToString("0.0") + " Вт")
                + "    потолок частоты: " + (cap == 0 ? "нет" : cap + " МГц");
        }
    }
}
