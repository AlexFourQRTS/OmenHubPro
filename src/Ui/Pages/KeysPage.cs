using System.Drawing;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Lighting;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>
    /// Per-key colours on top of the keyboard's general colour: left click paints a key with the brush colour,
    /// right click removes the override. Works while "OmenHub controls the lighting" is on.
    /// </summary>
    public sealed class KeysPage : PageBase
    {
        private readonly KeyMapView map;
        private readonly ColorPad brush;
        private readonly Label hintLabel;
        private bool layoutLoaded;

        public KeysPage()
        {
            CardPanel card = PageKit.MakeCard(this, 24, 16, 802, 604, "Отдельные клавиши");
            PageKit.MakeLabel(card, "Левый клик — покрасить клавишу кистью, правый — вернуть общий цвет. Число на клавише — номер её лампы.",
                20, 40, 8.5f, false, PageKit.RoleSub);
            map = new KeyMapView { Left = 16, Top = 70, Width = 770, Height = 290 };
            card.Controls.Add(map);
            map.ColorOfLamp = ColorOfLamp;
            map.LampClicked += OnLampClicked;

            PageKit.MakeLabel(card, "Кисть", 20, 380, 9.5f, false, PageKit.RoleSub);
            brush = new ColorPad { Left = 20, Top = 404, Width = 380, Height = 120 };
            brush.SetColor(255, 0, 80);
            card.Controls.Add(brush);

            ActionButton resetButton = PageKit.MakeButton(card, "Сбросить все клавиши", 430, 404, 220, false);
            resetButton.Click += delegate { AppSettings.KeyColors.Clear(); AppSettings.MarkChanged(); map.Invalidate(); };
            hintLabel = PageKit.MakeLabel(card, "", 430, 450, 9, false, PageKit.RoleSub);
        }

        public override void LoadFromSettings()
        {
            if (!layoutLoaded)
            {
                float[] xs, ys;
                LampLayout.Get(out xs, out ys);
                map.SetPositions(xs, ys);
                layoutLoaded = true;
            }
            hintLabel.Text = AppSettings.LightingControlEnabled
                ? "OmenHub управляет подсветкой — изменения видны сразу."
                : "Включи «Управляет OmenHub» на вкладке «Подсветка», иначе клавиатура не покажет цвета.";
            map.Invalidate();
        }

        private void OnLampClicked(int lamp, MouseButtons button)
        {
            if (button == MouseButtons.Right)
            {
                byte[] removed;
                AppSettings.KeyColors.TryRemove(lamp, out removed);
            }
            else AppSettings.KeyColors[lamp] = new[] { brush.SelectedRgb[0], brush.SelectedRgb[1], brush.SelectedRgb[2] };
            AppSettings.MarkChanged();
        }

        private static Color ColorOfLamp(int lamp)
        {
            byte[] color;
            if (!AppSettings.KeyColors.TryGetValue(lamp, out color))
            {
                ZoneLighting keyboard = AppSettings.KeyboardLighting;
                color = LightingEffects.ColorAt(keyboard, lamp, LightingEngine.KeyboardLastLamp + 1, 0);
            }
            return Color.FromArgb(color[0], color[1], color[2]);
        }
    }
}
