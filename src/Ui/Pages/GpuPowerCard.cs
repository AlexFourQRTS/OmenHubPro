using System;
using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>GPU power: one of 50 / 80 / 100 / 130 W, held by the governor through the graphics clock ceiling.</summary>
    public sealed class GpuPowerCard : CardPanel
    {
        private readonly SegmentedControl wattSegments;

        public GpuPowerCard(int top)
        {
            Left = 24; Top = top; Width = 802; Height = 92;
            PageKit.MakeLabel(this, "GPU power", 20, 12, 11, true, PageKit.RoleText);
            wattSegments = PageKit.MakeSegments(this, 20, 42, 480, PowerChoices.Captions(AppSettings.GpuWattChoices), 0);
            LoadFromSettings();
            wattSegments.SelectionChanged += delegate
            {
                AppSettings.GpuWatts = AppSettings.GpuWattChoices[wattSegments.SelectedIndex];
                AppSettings.MarkChanged();
            };
        }

        public void LoadFromSettings()
        {
            wattSegments.SelectedIndex = Array.IndexOf(AppSettings.GpuWattChoices, AppSettings.Snap(AppSettings.GpuWattChoices, AppSettings.GpuWatts));
        }
    }
}
