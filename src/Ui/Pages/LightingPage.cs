using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>Lighting: who controls the lamps, then independent settings for the keyboard and the light bar.</summary>
    public sealed class LightingPage : PageBase
    {
        private readonly SegmentedControl controlSegments;
        private readonly ZoneLightingCard keyboardCard;
        private readonly ZoneLightingCard barCard;

        public LightingPage()
        {
            CardPanel controlCard = PageKit.MakeCard(this, 24, 16, 802, 92, "Управление подсветкой");
            controlSegments = PageKit.MakeSegments(controlCard, 20, 46, 420,
                new[] { "Как у клавиатуры (по умолчанию)", "Управляет OmenHub" }, AppSettings.LightingControlEnabled ? 1 : 0);
            PageKit.MakeLabel(controlCard, "Когда включено «Управляет OmenHub», приложение рисует клавиатуру и планку само;\nпри выходе клавиатура возвращается к своим эффектам.",
                460, 44, 8.5f, false, PageKit.RoleSub);
            controlSegments.SelectionChanged += delegate
            {
                AppSettings.LightingControlEnabled = controlSegments.SelectedIndex == 1;
                AppSettings.MarkChanged();
            };

            keyboardCard = new ZoneLightingCard("Клавиатура", "Все клавиши (лампы 0–102)", AppSettings.KeyboardLighting, 124);
            barCard = new ZoneLightingCard("Нижняя планка", "17 ламп под экраном (лампы 103–119)", AppSettings.BarLighting, 380);
            Controls.Add(keyboardCard);
            Controls.Add(barCard);
        }

        public override void LoadFromSettings()
        {
            controlSegments.SelectedIndex = AppSettings.LightingControlEnabled ? 1 : 0;
            keyboardCard.LoadFromSettings();
            barCard.LoadFromSettings();
        }
    }
}
