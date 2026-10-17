using OmenHub.Core.Settings;

namespace OmenHub.Ui.Pages
{
    /// <summary>Keyboard lighting: one general colour/effect for all keys, plus individual keys on top.</summary>
    public sealed class KeyboardPage : PageBase
    {
        private readonly ZoneLightingCard generalCard;
        private readonly KeyMapCard keyMapCard;

        public KeyboardPage()
        {
            generalCard = new ZoneLightingCard("Клавиатура", "Общий цвет и эффект для всех клавиш", AppSettings.KeyboardLighting, 16);
            keyMapCard = new KeyMapCard(260);
            Controls.Add(generalCard);
            Controls.Add(keyMapCard);
        }

        public override void LoadFromSettings()
        {
            generalCard.LoadFromSettings();
            keyMapCard.LoadFromSettings();
        }
    }
}
