using System.Drawing;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>Bottom light bar: switch it off exclusively (BIOS) or colour its four zones, left to right.</summary>
    public sealed class BarCard : CardPanel
    {
        private const int ColumnSpacing = 196;
        private readonly BarZoneColumn[] columns = new BarZoneColumn[AppSettings.BarZoneCount];
        private readonly SegmentedControl enabledSegments;
        private readonly ValueSlider brightnessSlider;
        private readonly Label brightnessLabel;

        public BarCard()
        {
            Left = 24; Top = 388; Width = 802; Height = 232;
            PageKit.MakeLabel(this, "Нижняя панель", 20, 12, 11, true, PageKit.RoleText);
            enabledSegments = PageKit.MakeSegments(this, 470, 6, 310, new[] { "Включена", "Выключена" }, AppSettings.BarEnabled ? 0 : 1);
            for (int zone = 0; zone < columns.Length; zone++)
                columns[zone] = new BarZoneColumn(this, zone, 20 + zone * ColumnSpacing);

            PageKit.MakeLabel(this, "Яркость", 20, 194, 9.5f, false, PageKit.RoleSub);
            brightnessSlider = PageKit.MakeSlider(this, 100, 188, 330, 0, 100, 5, AppSettings.BarBrightnessPercent);
            brightnessLabel = PageKit.MakeValueLabel(this, 436, 190, 70, 10, ContentAlignment.MiddleRight);
            ActionButton copyButton = PageKit.MakeButton(this, "Все зоны как зона 1", 530, 186, 220, false);

            enabledSegments.SelectionChanged += delegate { AppSettings.BarEnabled = enabledSegments.SelectedIndex == 0; AppSettings.MarkChanged(); };
            brightnessSlider.ValueChanged += delegate
            {
                AppSettings.BarBrightnessPercent = brightnessSlider.Value;
                brightnessLabel.Text = brightnessSlider.Value + " %";
                AppSettings.MarkChanged();
            };
            copyButton.Click += delegate
            {
                for (int zone = 1; zone < AppSettings.BarZoneCount; zone++)
                    AppSettings.BarZoneColors[zone] = (byte[])AppSettings.BarZoneColors[0].Clone();
                AppSettings.MarkChanged();
                LoadFromSettings();
            };
        }

        public void LoadFromSettings()
        {
            foreach (BarZoneColumn column in columns) column.LoadFromSettings();
            enabledSegments.SelectedIndex = AppSettings.BarEnabled ? 0 : 1;
            brightnessSlider.Value = AppSettings.BarBrightnessPercent;
            brightnessLabel.Text = brightnessSlider.Value + " %";
        }
    }
}
