using System.Drawing;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Lighting;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>
    /// Keyboard map with two modes. "Paint": left click colours a key, right click restores the general colour.
    /// "Identify": click a lamp to make it blink white, then say whether it is a key or part of the bottom bar.
    /// </summary>
    public sealed class KeyMapCard : CardPanel
    {
        private readonly KeyMapView map;
        private readonly ColorPad brush;
        private readonly SegmentedControl modeSegments;
        private readonly Label selectionLabel;
        private bool layoutLoaded;

        private bool IdentifyMode { get { return modeSegments.SelectedIndex == 1; } }

        public KeyMapCard(int top)
        {
            Left = 24; Top = top; Width = 802; Height = 372;
            PageKit.MakeLabel(this, "Отдельные клавиши", 20, 14, 11, true, PageKit.RoleText);
            modeSegments = PageKit.MakeSegments(this, 470, 8, 310, new[] { "Красить", "Определить лампу" }, 0);
            map = new KeyMapView { Left = 16, Top = 50, Width = 770, Height = 236 };
            Controls.Add(map);
            map.ColorOfLamp = ColorOfLamp;
            map.IsBarLamp = delegate (int lamp) { return AppSettings.BarLamps.ContainsKey(lamp); };
            map.LampClicked += OnLampClicked;

            PageKit.MakeLabel(this, "Кисть", 20, 304, 9.5f, false, PageKit.RoleSub);
            brush = new ColorPad { Left = 80, Top = 298, Width = 300, Height = 62 };
            brush.SetColor(255, 0, 80);
            Controls.Add(brush);
            selectionLabel = PageKit.MakeLabel(this, "", 400, 296, 9, false, PageKit.RoleSub);
            ActionButton keyButton = PageKit.MakeButton(this, "Это клавиша", 400, 322, 150, false);
            ActionButton barButton = PageKit.MakeButton(this, "Это планка", 562, 322, 150, false);
            ActionButton resetButton = PageKit.MakeButton(this, "Сбросить все клавиши", 580, 296, 190, false);
            keyButton.Click += delegate { Classify(false); };
            barButton.Click += delegate { Classify(true); };
            resetButton.Click += delegate { AppSettings.KeyColors.Clear(); AppSettings.MarkChanged(); map.Invalidate(); };
            modeSegments.SelectionChanged += delegate { ResetSelection(); };
            ShowSelection();
        }

        public void LoadFromSettings()
        {
            if (!layoutLoaded)
            {
                float[] xMillimetres, yMillimetres;
                LampLayout.Get(out xMillimetres, out yMillimetres);
                map.SetPositions(xMillimetres, yMillimetres);
                layoutLoaded = true;
            }
            map.Invalidate();
        }

        private void OnLampClicked(int lamp, MouseButtons button)
        {
            if (IdentifyMode)
            {
                AppSettings.IdentifiedLamp = lamp;
                map.SelectedLamp = lamp;
                ShowSelection();
                return;
            }
            if (button == MouseButtons.Right)
            {
                byte[] removed;
                AppSettings.KeyColors.TryRemove(lamp, out removed);
            }
            else AppSettings.KeyColors[lamp] = new[] { brush.SelectedRgb[0], brush.SelectedRgb[1], brush.SelectedRgb[2] };
            AppSettings.MarkChanged();
        }

        private void Classify(bool isBar)
        {
            int lamp = AppSettings.IdentifiedLamp;
            if (lamp < 0) return;
            if (isBar) AppSettings.BarLamps[lamp] = true;
            else { bool removed; AppSettings.BarLamps.TryRemove(lamp, out removed); }
            AppSettings.MarkChanged();
            ShowSelection();
            map.Invalidate();
        }

        private void ResetSelection()
        {
            AppSettings.IdentifiedLamp = -1;
            map.SelectedLamp = -1;
            ShowSelection();
        }

        private void ShowSelection()
        {
            int lamp = AppSettings.IdentifiedLamp;
            selectionLabel.Text = !IdentifyMode ? "Режим «Определить лампу»: клик по лампе — она мигает белым."
                : lamp < 0 ? "Кликни лампу: она замигает белым. Потом выбери, что это."
                : "Лампа " + lamp + " мигает. Сейчас считается: " + (AppSettings.BarLamps.ContainsKey(lamp) ? "планка" : "клавиша");
        }

        private static Color ColorOfLamp(int lamp)
        {
            byte[] color;
            if (AppSettings.BarLamps.ContainsKey(lamp))
                color = AppSettings.BarZoneColors[0];
            else if (!AppSettings.KeyColors.TryGetValue(lamp, out color))
                color = LightingEffects.ColorAt(AppSettings.KeyboardLighting, lamp, 120, 0);
            return Color.FromArgb(color[0], color[1], color[2]);
        }
    }
}
