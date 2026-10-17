using System.Drawing;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>
    /// Appearance and content of the in-game overlay. Switching it on or off altogether is in Settings.
    /// Rows: CPU, GPU, memory. Values: temperature, power, fan speed.
    /// </summary>
    public sealed class OverlayPage : PageBase
    {
        private readonly SegmentedControl modeSegments;
        private readonly SegmentedControl cornerSegments;
        private readonly ValueSlider sizeSlider;
        private readonly ValueSlider fillSlider, textSlider;
        private readonly Label sizeLabel;
        private readonly Label fillLabel, textLabel;
        private readonly ColorPad colorPad;
        private readonly SegmentedControl cpuSwitch, gpuSwitch, ramSwitch, temperatureSwitch, powerSwitch, fanSwitch;

        public OverlayPage()
        {
            CardPanel card = PageKit.MakeCard(this, 24, 16, 802, 600, "Оверлей в играх");

            PageKit.MakeLabel(card, "Показывать", 20, 78, 9.5f, false, PageKit.RoleSub);
            modeSegments = PageKit.MakeSegments(card, 160, 72, 330, new[] { "Только в играх", "Всегда" }, (int)AppSettings.Overlay - 1);
            PageKit.MakeLabel(card, "Место", 20, 126, 9.5f, false, PageKit.RoleSub);
            cornerSegments = PageKit.MakeSegments(card, 160, 120, 620, new[] { "Слева сверху", "Справа сверху", "Слева снизу", "Справа снизу" }, (int)AppSettings.OverlayPosition);

            PageKit.MakeLabel(card, "Размер", 20, 174, 9.5f, false, PageKit.RoleSub);
            sizeSlider = PageKit.MakeSlider(card, 160, 168, 420, 60, 200, 10, AppSettings.OverlayScalePercent);
            sizeLabel = PageKit.MakeValueLabel(card, 590, 170, 90, 10.5f, ContentAlignment.MiddleRight);
            PageKit.MakeLabel(card, "Прозрачность заливки", 20, 218, 9.5f, false, PageKit.RoleSub);
            fillSlider = PageKit.MakeSlider(card, 200, 212, 380, 0, 100, 5, AppSettings.OverlayFillOpacityPercent);
            fillLabel = PageKit.MakeValueLabel(card, 590, 214, 90, 10.5f, ContentAlignment.MiddleRight);
            PageKit.MakeLabel(card, "Прозрачность текста", 20, 262, 9.5f, false, PageKit.RoleSub);
            textSlider = PageKit.MakeSlider(card, 200, 256, 380, 20, 100, 5, AppSettings.OverlayTextOpacityPercent);
            textLabel = PageKit.MakeValueLabel(card, 590, 258, 90, 10.5f, ContentAlignment.MiddleRight);

            PageKit.MakeLabel(card, "Цвет подписей", 20, 312, 9.5f, false, PageKit.RoleSub);
            colorPad = new ColorPad { Left = 160, Top = 306, Width = 330, Height = 56 };
            card.Controls.Add(colorPad);

            PageKit.MakeLabel(card, "Строки", 20, 392, 9.5f, true, PageKit.RoleText);
            cpuSwitch = MakeSwitch(card, 160, 378, "Процессор");
            gpuSwitch = MakeSwitch(card, 330, 378, "Видеокарта");
            ramSwitch = MakeSwitch(card, 500, 378, "ОЗУ");
            PageKit.MakeLabel(card, "Значения", 20, 468, 9.5f, true, PageKit.RoleText);
            temperatureSwitch = MakeSwitch(card, 160, 454, "Температура");
            powerSwitch = MakeSwitch(card, 330, 454, "Мощность");
            fanSwitch = MakeSwitch(card, 500, 454, "Обороты кулера");
            WireEvents();
            LoadFromSettings();
        }

        private static SegmentedControl MakeSwitch(CardPanel card, int left, int top, string name)
        {
            PageKit.MakeLabel(card, name, left, top, 8.5f, false, PageKit.RoleSub);
            return PageKit.MakeSegments(card, left, top + 20, 150, new[] { "Вкл", "Выкл" }, 0);
        }

        private void WireEvents()
        {
            modeSegments.SelectionChanged += delegate { AppSettings.Overlay = (OverlayMode)(modeSegments.SelectedIndex + 1); AppSettings.MarkChanged(); };
            cornerSegments.SelectionChanged += delegate { AppSettings.OverlayPosition = (OverlayCorner)cornerSegments.SelectedIndex; AppSettings.MarkChanged(); };
            sizeSlider.ValueChanged += delegate { AppSettings.OverlayScalePercent = sizeSlider.Value; ShowNumbers(); AppSettings.MarkChanged(); };
            fillSlider.ValueChanged += delegate { AppSettings.OverlayFillOpacityPercent = fillSlider.Value; ShowNumbers(); AppSettings.MarkChanged(); };
            textSlider.ValueChanged += delegate { AppSettings.OverlayTextOpacityPercent = textSlider.Value; ShowNumbers(); AppSettings.MarkChanged(); };
            colorPad.ColorChanged += delegate { AppSettings.OverlayColor = (byte[])colorPad.SelectedRgb.Clone(); AppSettings.MarkChanged(); };
            cpuSwitch.SelectionChanged += delegate { AppSettings.OverlayShowCpu = cpuSwitch.SelectedIndex == 0; AppSettings.MarkChanged(); };
            gpuSwitch.SelectionChanged += delegate { AppSettings.OverlayShowGpu = gpuSwitch.SelectedIndex == 0; AppSettings.MarkChanged(); };
            ramSwitch.SelectionChanged += delegate { AppSettings.OverlayShowRam = ramSwitch.SelectedIndex == 0; AppSettings.MarkChanged(); };
            temperatureSwitch.SelectionChanged += delegate { AppSettings.OverlayShowTemperature = temperatureSwitch.SelectedIndex == 0; AppSettings.MarkChanged(); };
            powerSwitch.SelectionChanged += delegate { AppSettings.OverlayShowPower = powerSwitch.SelectedIndex == 0; AppSettings.MarkChanged(); };
            fanSwitch.SelectionChanged += delegate { AppSettings.OverlayShowFan = fanSwitch.SelectedIndex == 0; AppSettings.MarkChanged(); };
        }

        public override void LoadFromSettings()
        {
            modeSegments.SelectedIndex = (int)AppSettings.Overlay - 1;
            cornerSegments.SelectedIndex = (int)AppSettings.OverlayPosition;
            sizeSlider.Value = AppSettings.OverlayScalePercent;
            fillSlider.Value = AppSettings.OverlayFillOpacityPercent;
            textSlider.Value = AppSettings.OverlayTextOpacityPercent;
            colorPad.SetColor(AppSettings.OverlayColor[0], AppSettings.OverlayColor[1], AppSettings.OverlayColor[2]);
            cpuSwitch.SelectedIndex = AppSettings.OverlayShowCpu ? 0 : 1;
            gpuSwitch.SelectedIndex = AppSettings.OverlayShowGpu ? 0 : 1;
            ramSwitch.SelectedIndex = AppSettings.OverlayShowRam ? 0 : 1;
            temperatureSwitch.SelectedIndex = AppSettings.OverlayShowTemperature ? 0 : 1;
            powerSwitch.SelectedIndex = AppSettings.OverlayShowPower ? 0 : 1;
            fanSwitch.SelectedIndex = AppSettings.OverlayShowFan ? 0 : 1;
            ShowNumbers();
        }

        private void ShowNumbers()
        {
            sizeLabel.Text = sizeSlider.Value + " %";
            fillLabel.Text = fillSlider.Value + " %";
            textLabel.Text = textSlider.Value + " %";
        }
    }
}
