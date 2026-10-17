namespace OmenHub.Ui.Pages
{
    /// <summary>Lighting: the keyboard on top, the bottom light bar below.</summary>
    public sealed class LightingPage : PageBase
    {
        private readonly KeyboardLightingCard keyboardCard = new KeyboardLightingCard();
        private readonly BarCard barCard = new BarCard();

        public LightingPage()
        {
            Controls.Add(keyboardCard);
            Controls.Add(barCard);
        }

        public override void LoadFromSettings()
        {
            keyboardCard.LoadFromSettings();
            barCard.LoadFromSettings();
        }
    }
}
