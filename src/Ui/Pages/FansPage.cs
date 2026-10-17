using OmenHub.Hardware.Sensors;

namespace OmenHub.Ui.Pages
{
    /// <summary>Fan control page: mode, manual speeds, curves.</summary>
    public sealed class FansPage : PageBase
    {
        private readonly FanModeCard modeCard = new FanModeCard();
        private readonly ManualFansCard manualCard = new ManualFansCard();
        private readonly CurveCard curveCard = new CurveCard();

        public FansPage()
        {
            Controls.Add(modeCard);
            Controls.Add(manualCard);
            Controls.Add(curveCard);
        }

        public override void LoadFromSettings()
        {
            modeCard.LoadFromSettings();
            manualCard.LoadFromSettings();
            curveCard.LoadFromSettings();
        }

        public override void ShowSnapshot(SensorSnapshot snapshot)
        {
            curveCard.ShowSnapshot(snapshot);
        }
    }
}
