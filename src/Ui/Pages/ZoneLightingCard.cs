using System.Drawing;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>Colour, effect and brightness of one lighting zone (keyboard or light bar).</summary>
    public sealed class ZoneLightingCard : CardPanel
    {
        private readonly ZoneLighting zone;
        private readonly SegmentedControl effectSegments;
        private readonly ColorPad colorPad;
        private readonly ValueSlider brightnessSlider;
        private readonly Label brightnessLabel;

        public ZoneLightingCard(string title, string subtitle, ZoneLighting zone, int top)
        {
            this.zone = zone;
            Left = 24; Top = top; Width = 802; Height = 232;
            PageKit.MakeLabel(this, title, 20, 14, 11, true, PageKit.RoleText);
            PageKit.MakeLabel(this, subtitle, 20, 38, 8.5f, false, PageKit.RoleSub);

            effectSegments = PageKit.MakeSegments(this, 20, 66, 380, new[] { "Выкл", "Цвет", "Радуга", "Дыхание" }, (int)zone.Effect);
            colorPad = new ColorPad { Left = 20, Top = 116, Width = 380, Height = 112 };
            Controls.Add(colorPad);

            PageKit.MakeLabel(this, "Яркость", 430, 74, 9.5f, false, PageKit.RoleSub);
            brightnessSlider = PageKit.MakeSlider(this, 430, 100, 340, 0, 100, 5, zone.BrightnessPercent);
            brightnessLabel = PageKit.MakeValueLabel(this, 670, 70, 100, 10.5f, ContentAlignment.MiddleRight);
            PageKit.MakeLabel(this, "Цвет — потяни по палитре слева: оттенок вправо, к белому вниз.\nДля «Радуга» и «Выкл» цвет не нужен.",
                430, 156, 8.5f, false, PageKit.RoleSub);

            effectSegments.SelectionChanged += delegate { zone.Effect = (LightingEffect)effectSegments.SelectedIndex; Changed(); };
            colorPad.ColorChanged += delegate { StoreColor(); };
            brightnessSlider.ValueChanged += delegate { zone.BrightnessPercent = brightnessSlider.Value; ShowBrightness(); Changed(); };
            LoadFromSettings();
        }

        public void LoadFromSettings()
        {
            effectSegments.SelectedIndex = (int)zone.Effect;
            colorPad.SetColor(zone.Red, zone.Green, zone.Blue);
            brightnessSlider.Value = zone.BrightnessPercent;
            ShowBrightness();
        }

        private void StoreColor()
        {
            zone.Red = colorPad.SelectedRgb[0];
            zone.Green = colorPad.SelectedRgb[1];
            zone.Blue = colorPad.SelectedRgb[2];
            if (zone.Effect == LightingEffect.Off || zone.Effect == LightingEffect.Rainbow)
            {
                zone.Effect = LightingEffect.Static;
                effectSegments.SelectedIndex = (int)LightingEffect.Static;
            }
            Changed();
        }

        private void ShowBrightness()
        {
            brightnessLabel.Text = brightnessSlider.Value + " %";
        }

        private static void Changed()
        {
            AppSettings.MarkChanged();
        }
    }
}
