using System.Drawing;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Power;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>CPU power limit (20-75 W, step 5) applied through RyzenAdj.</summary>
    public sealed class CpuPowerCard : CardPanel
    {
        private readonly ValueSlider wattsSlider;
        private readonly Label wattsLabel;

        public CpuPowerCard()
        {
            Left = 24; Top = 304; Width = 802; Height = 112;
            PageKit.MakeLabel(this, "CPU power", 20, 12, 11, true, PageKit.RoleText);
            wattsSlider = PageKit.MakeSlider(this, 20, 54, 520, AppSettings.MinimumCpuWatts, AppSettings.MaximumCpuWatts,
                AppSettings.CpuWattsStep, AppSettings.CpuWatts);
            wattsLabel = PageKit.MakeValueLabel(this, 550, 54, 92, 10.5f, ContentAlignment.MiddleRight);
            ActionButton applyButton = PageKit.MakeButton(this, "Применить", 660, 50, 122, true);
            wattsSlider.ValueChanged += delegate { OnSliderMoved(); };
            applyButton.Click += delegate { RyzenAdjTool.SetPowerLimit(wattsSlider.Value); };
            LoadFromSettings();
        }

        public void LoadFromSettings()
        {
            wattsSlider.Value = AppSettings.ProAutoEnabled ? AppSettings.ProAutoWatts : AppSettings.CpuWatts;
            wattsLabel.Text = wattsSlider.Value + " Вт";
        }

        private void OnSliderMoved()
        {
            if (AppSettings.ProAutoEnabled) { LoadFromSettings(); return; }   // locked while the profile is active
            AppSettings.CpuWatts = wattsSlider.Value;
            wattsLabel.Text = wattsSlider.Value + " Вт";
            AppSettings.MarkChanged();
        }
    }
}
