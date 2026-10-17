using System;
using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>Card with the four fan modes: BIOS auto, maximum, manual, curve.</summary>
    public sealed class FanModeCard : CardPanel
    {
        private static readonly string[] ModeDescriptions =
        {
            "BIOS сам управляет вентиляторами.",
            "Оба кулера на максимум.",
            "Фиксированные обороты из ползунков.",
            "Обороты по кривым ниже."
        };

        private readonly SegmentedControl modeSegments;
        private readonly System.Windows.Forms.Label descriptionLabel;

        public FanModeCard()
        {
            Left = 24; Top = 16; Width = 802; Height = 100;
            PageKit.MakeLabel(this, "Режим вентиляторов", 20, 14, 11, true, PageKit.RoleText);
            modeSegments = PageKit.MakeSegments(this, 20, 48, 520,
                new[] { "Авто (BIOS)", "Максимум", "Ручной", "По кривой" }, (int)AppSettings.Mode);
            descriptionLabel = PageKit.MakeLabel(this, "", 556, 56, 8.5f, false, PageKit.RoleSub);
            modeSegments.SelectionChanged += delegate { OnModeChosen(); };
            ShowDescription();
        }

        public void LoadFromSettings()
        {
            modeSegments.SelectedIndex = (int)AppSettings.Mode;
            ShowDescription();
        }

        private void OnModeChosen()
        {
            AppSettings.Mode = (FanMode)modeSegments.SelectedIndex;
            AppSettings.MarkChanged();
            ShowDescription();
        }

        private void ShowDescription()
        {
            descriptionLabel.Text = ModeDescriptions[(int)AppSettings.Mode]
                + "\nЗащита: ≥80 °C — оба кулера на максимум (нельзя отключить).";
        }
    }
}
