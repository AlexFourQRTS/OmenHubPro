using System.Drawing;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>Bottom light bar: four zones coloured independently, plus overall brightness.</summary>
    public sealed class BarPage : PageBase
    {
        private const int ColumnSpacing = 196;
        private readonly BarZoneColumn[] columns = new BarZoneColumn[AppSettings.BarZoneCount];
        private readonly ValueSlider brightnessSlider;
        private readonly Label brightnessLabel;
        private readonly SegmentedControl enabledSegments;

        public BarPage()
        {
            CardPanel card = PageKit.MakeCard(this, 24, 16, 802, 340, "Нижняя панель");
            enabledSegments = PageKit.MakeSegments(card, 470, 8, 310, new[] { "Включена", "Выключена" }, AppSettings.BarEnabled ? 0 : 1);
            enabledSegments.SelectionChanged += delegate
            {
                AppSettings.BarEnabled = enabledSegments.SelectedIndex == 0;
                AppSettings.MarkChanged();
            };
            PageKit.MakeLabel(card, "Четыре зоны слева направо. Цвет запоминается и остаётся даже после закрытия программы.",
                20, 38, 8.5f, false, PageKit.RoleSub);
            for (int zone = 0; zone < columns.Length; zone++)
                columns[zone] = new BarZoneColumn(card, zone, 20 + zone * ColumnSpacing);

            PageKit.MakeLabel(card, "Яркость панели", 20, 268, 9.5f, false, PageKit.RoleSub);
            brightnessSlider = PageKit.MakeSlider(card, 150, 262, 440, 0, 100, 5, AppSettings.BarBrightnessPercent);
            brightnessLabel = PageKit.MakeValueLabel(card, 600, 264, 90, 10.5f, ContentAlignment.MiddleRight);
            brightnessSlider.ValueChanged += delegate
            {
                AppSettings.BarBrightnessPercent = brightnessSlider.Value;
                brightnessLabel.Text = brightnessSlider.Value + " %";
                AppSettings.MarkChanged();
            };
            ActionButton copyButton = PageKit.MakeButton(card, "Все зоны как зона 1", 20, 298, 220, false);
            copyButton.Click += delegate
            {
                for (int zone = 1; zone < AppSettings.BarZoneCount; zone++)
                    AppSettings.BarZoneColors[zone] = (byte[])AppSettings.BarZoneColors[0].Clone();
                AppSettings.MarkChanged();
                LoadFromSettings();
            };
        }

        public override void LoadFromSettings()
        {
            foreach (BarZoneColumn column in columns) column.LoadFromSettings();
            enabledSegments.SelectedIndex = AppSettings.BarEnabled ? 0 : 1;
            brightnessSlider.Value = AppSettings.BarBrightnessPercent;
            brightnessLabel.Text = brightnessSlider.Value + " %";
        }
    }
}
