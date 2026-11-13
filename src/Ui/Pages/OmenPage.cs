using OmenHub.Hardware.Sensors;

namespace OmenHub.Ui.Pages
{
    /// <summary>The main page: fan mode, own fan speeds, GPU power and CPU power.</summary>
    public sealed class OmenPage : PageBase
    {
        private readonly FanModeCard modeCard = new FanModeCard();
        private readonly ManualFansCard manualCard = new ManualFansCard();
        private readonly GpuPowerCard gpuCard = new GpuPowerCard(298);
        private readonly CpuPowerCard cpuCard = new CpuPowerCard(438);

        public OmenPage()
        {
            Controls.Add(modeCard);
            Controls.Add(manualCard);
            Controls.Add(gpuCard);
            Controls.Add(cpuCard);
        }

        public override void LoadFromSettings()
        {
            modeCard.LoadFromSettings();
            manualCard.LoadFromSettings();
            gpuCard.LoadFromSettings();
            cpuCard.LoadFromSettings();
        }

        public override void ShowSnapshot(SensorSnapshot snapshot)
        {
            gpuCard.ShowLive();
            cpuCard.ShowStatus();
        }
    }
}
