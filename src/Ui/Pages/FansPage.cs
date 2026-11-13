using OmenHub.Hardware.Sensors;

namespace OmenHub.Ui.Pages
{
    /// <summary>Fan control page: mode and manual speeds.</summary>
    public sealed class FansPage : PageBase
    {
        private readonly FanModeCard modeCard = new FanModeCard();
        private readonly ManualFansCard manualCard = new ManualFansCard();

        public FansPage()
        {
            Controls.Add(modeCard);
            Controls.Add(manualCard);
        }

        public override void LoadFromSettings()
        {
            modeCard.LoadFromSettings();
            manualCard.LoadFromSettings();
        }

        public override void ShowSnapshot(SensorSnapshot snapshot)
        {
        }
    }
}
