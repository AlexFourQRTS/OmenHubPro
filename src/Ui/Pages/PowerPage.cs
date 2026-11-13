namespace OmenHub.Ui.Pages
{
    /// <summary>Power page: GPU power and CPU power, fixed wattage choices.</summary>
    public sealed class PowerPage : PageBase
    {
        private readonly GpuPowerCard gpuCard = new GpuPowerCard(16);
        private readonly CpuPowerCard cpuCard = new CpuPowerCard(156);

        public PowerPage()
        {
            Controls.Add(gpuCard);
            Controls.Add(cpuCard);
        }

        public override void ShowSnapshot(OmenHub.Hardware.Sensors.SensorSnapshot snapshot)
        {
            gpuCard.ShowLive();
            cpuCard.ShowStatus();
        }

        public override void LoadFromSettings()
        {
            cpuCard.LoadFromSettings();
            gpuCard.LoadFromSettings();
        }
    }
}
