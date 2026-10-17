namespace OmenHub.Ui.Pages
{
    /// <summary>Power page: the two automatic profiles (mains / battery), then GPU power and CPU power.</summary>
    public sealed class PowerPage : PageBase
    {
        private readonly PowerProfileCard mainsCard = new PowerProfileCard(false, 16);
        private readonly PowerProfileCard batteryCard = new PowerProfileCard(true, 120);
        private readonly GpuPowerCard gpuCard = new GpuPowerCard(224);
        private readonly CpuPowerCard cpuCard = new CpuPowerCard(360);

        public PowerPage()
        {
            Controls.Add(mainsCard);
            Controls.Add(batteryCard);
            Controls.Add(gpuCard);
            Controls.Add(cpuCard);
            mainsCard.ProfileChanged += delegate { cpuCard.LoadFromSettings(); gpuCard.LoadFromSettings(); };
        }

        public override void ShowSnapshot(OmenHub.Hardware.Sensors.SensorSnapshot snapshot)
        {
            gpuCard.ShowLive();
        }

        public override void LoadFromSettings()
        {
            mainsCard.LoadFromSettings();
            batteryCard.LoadFromSettings();
            cpuCard.LoadFromSettings();
            gpuCard.LoadFromSettings();
        }
    }
}
