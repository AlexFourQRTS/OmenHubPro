namespace OmenHub.Ui.Pages
{
    /// <summary>Power page: automatic profiles, GPU information and the CPU power limit.</summary>
    public sealed class PowerPage : PageBase
    {
        private readonly PowerProfileCard profileCard = new PowerProfileCard();
        private readonly GpuPowerCard gpuCard = new GpuPowerCard();
        private readonly CpuPowerCard cpuCard = new CpuPowerCard();

        public PowerPage()
        {
            Controls.Add(profileCard);
            Controls.Add(gpuCard);
            Controls.Add(cpuCard);
            profileCard.ProfileChanged += delegate { cpuCard.LoadFromSettings(); };
        }

        public override void LoadFromSettings()
        {
            profileCard.LoadFromSettings();
            cpuCard.LoadFromSettings();
            gpuCard.RefreshBiosMode();
        }
    }
}
