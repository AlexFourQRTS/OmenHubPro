using System.Drawing;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>Card with two sliders: fixed CPU fan speed and GPU fan speed.</summary>
    public sealed class ManualFansCard : CardPanel
    {
        private const int RpmStep = 100;
        private readonly ValueSlider cpuSlider;
        private readonly ValueSlider gpuSlider;
        private readonly Label cpuValueLabel;
        private readonly Label gpuValueLabel;

        public ManualFansCard()
        {
            Left = 24; Top = 132; Width = 802; Height = 150;
            PageKit.MakeLabel(this, "Ручные обороты", 20, 14, 11, true, PageKit.RoleText);
            PageKit.MakeLabel(this, "Кулер CPU", 20, 54, 9.5f, false, PageKit.RoleSub);
            PageKit.MakeLabel(this, "Кулер GPU", 20, 100, 9.5f, false, PageKit.RoleSub);
            cpuSlider = PageKit.MakeSlider(this, 110, 48, 560, 0, AppSettings.MaximumFanRpm, RpmStep, AppSettings.ManualCpuFanRpm);
            gpuSlider = PageKit.MakeSlider(this, 110, 94, 560, 0, AppSettings.MaximumFanRpm, RpmStep, AppSettings.ManualGpuFanRpm);
            cpuValueLabel = PageKit.MakeValueLabel(this, 690, 50, 100, 10.5f, ContentAlignment.MiddleRight);
            gpuValueLabel = PageKit.MakeValueLabel(this, 690, 96, 100, 10.5f, ContentAlignment.MiddleRight);
            cpuSlider.ValueChanged += delegate { AppSettings.ManualCpuFanRpm = cpuSlider.Value; ShowValues(); AppSettings.MarkChanged(); };
            gpuSlider.ValueChanged += delegate { AppSettings.ManualGpuFanRpm = gpuSlider.Value; ShowValues(); AppSettings.MarkChanged(); };
            ShowValues();
        }

        public void LoadFromSettings()
        {
            cpuSlider.Value = AppSettings.ManualCpuFanRpm;
            gpuSlider.Value = AppSettings.ManualGpuFanRpm;
            ShowValues();
        }

        private void ShowValues()
        {
            cpuValueLabel.Text = cpuSlider.Value + " об/мин";
            gpuValueLabel.Text = gpuSlider.Value + " об/мин";
        }
    }
}
