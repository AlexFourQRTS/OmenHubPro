using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>Card with the four fan modes: BIOS auto, maximum, manual, curve.</summary>
    public sealed class FanModeCard : CardPanel
    {
        private readonly SegmentedControl modeSegments;

        public FanModeCard()
        {
            Left = 24; Top = 16; Width = 802; Height = 100;
            PageKit.MakeLabel(this, "Режим вентиляторов", 20, 14, 11, true, PageKit.RoleText);
            modeSegments = PageKit.MakeSegments(this, 20, 48, 520,
                new[] { "Авто (BIOS)", "Максимум", "Ручной", "По кривой" }, (int)AppSettings.Mode);
            modeSegments.SelectionChanged += delegate
            {
                AppSettings.Mode = (FanMode)modeSegments.SelectedIndex;
                AppSettings.MarkChanged();
            };
        }

        public void LoadFromSettings()
        {
            modeSegments.SelectedIndex = (int)AppSettings.Mode;
        }
    }
}
