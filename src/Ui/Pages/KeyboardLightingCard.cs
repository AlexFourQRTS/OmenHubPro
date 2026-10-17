using System.Drawing;
using System.Windows.Forms;
using OmenHub.Core.Presets;
using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;
using OmenHub.Ui.Shell;

namespace OmenHub.Ui.Pages
{
    /// <summary>Keyboard lighting: effect, two colours, brightness, speed and saved presets.</summary>
    public sealed class KeyboardLightingCard : CardPanel
    {
        private static readonly string[] EffectNames =
        {
            "Выкл", "Цвет", "Градиент", "Дыхание", "Радуга", "Цикл цвета", "Волна", "Мигание", "Мерцание", "Сканер", "Огонь"
        };

        private readonly ChoiceGrid effectGrid;
        private readonly ColorPad primaryPad;
        private readonly ColorPad secondaryPad;
        private readonly ValueSlider brightnessSlider;
        private readonly ValueSlider speedSlider;
        private readonly Label brightnessLabel;
        private readonly Label speedLabel;
        private readonly ChoiceGrid presetGrid;

        private static ZoneLighting Lighting { get { return AppSettings.KeyboardLighting; } }

        public KeyboardLightingCard()
        {
            Left = 24; Top = 16; Width = 802; Height = 356;
            PageKit.MakeLabel(this, "Клавиатура", 20, 12, 11, true, PageKit.RoleText);
            effectGrid = new ChoiceGrid { Left = 20, Top = 44, Width = 762, Columns = 6, Items = EffectNames };
            Controls.Add(effectGrid);

            PageKit.MakeLabel(this, "Основной цвет", 20, 126, 9, false, PageKit.RoleSub);
            PageKit.MakeLabel(this, "Второй цвет", 290, 126, 9, false, PageKit.RoleSub);
            primaryPad = MakePad(20);
            secondaryPad = MakePad(290);

            PageKit.MakeLabel(this, "Яркость", 560, 126, 9, false, PageKit.RoleSub);
            brightnessSlider = PageKit.MakeSlider(this, 560, 144, 160, 0, 100, 5, 100);
            brightnessLabel = PageKit.MakeValueLabel(this, 722, 142, 60, 9.5f, ContentAlignment.MiddleRight);
            PageKit.MakeLabel(this, "Скорость", 560, 180, 9, false, PageKit.RoleSub);
            speedSlider = PageKit.MakeSlider(this, 560, 198, 160, 1, ZoneLighting.MaximumSpeed, 1, 5);
            speedLabel = PageKit.MakeValueLabel(this, 722, 196, 60, 9.5f, ContentAlignment.MiddleRight);

            PageKit.MakeLabel(this, "Пресеты", 20, 236, 9, false, PageKit.RoleSub);
            presetGrid = new ChoiceGrid { Left = 20, Top = 256, Width = 762, Columns = 6 };
            Controls.Add(presetGrid);
            ActionButton saveButton = PageKit.MakeButton(this, "Сохранить пресет", 20, 306, 200, true);
            ActionButton deleteButton = PageKit.MakeButton(this, "Удалить выбранный", 232, 306, 200, false);

            effectGrid.SelectionChanged += delegate { Lighting.Effect = (LightingEffect)effectGrid.SelectedIndex; Changed(); };
            primaryPad.ColorChanged += delegate { Lighting.Primary = (byte[])primaryPad.SelectedRgb.Clone(); EnsureColorEffect(); Changed(); };
            secondaryPad.ColorChanged += delegate { Lighting.Secondary = (byte[])secondaryPad.SelectedRgb.Clone(); Changed(); };
            brightnessSlider.ValueChanged += delegate { Lighting.BrightnessPercent = brightnessSlider.Value; ShowNumbers(); Changed(); };
            speedSlider.ValueChanged += delegate { Lighting.Speed = speedSlider.Value; ShowNumbers(); Changed(); };
            presetGrid.SelectionChanged += delegate { ApplyPreset(); };
            saveButton.Click += delegate { SavePreset(); };
            deleteButton.Click += delegate { DeletePreset(); };
            LoadFromSettings();
        }

        private ColorPad MakePad(int left)
        {
            var pad = new ColorPad { Left = left, Top = 146, Width = 250, Height = 76 };
            Controls.Add(pad);
            return pad;
        }

        public void LoadFromSettings()
        {
            effectGrid.SelectedIndex = (int)Lighting.Effect;
            primaryPad.SetColor(Lighting.Primary[0], Lighting.Primary[1], Lighting.Primary[2]);
            secondaryPad.SetColor(Lighting.Secondary[0], Lighting.Secondary[1], Lighting.Secondary[2]);
            brightnessSlider.Value = Lighting.BrightnessPercent;
            speedSlider.Value = Lighting.Speed;
            ShowNumbers();
            RefreshPresetList(null);
        }

        private void ShowNumbers()
        {
            brightnessLabel.Text = brightnessSlider.Value + " %";
            speedLabel.Text = speedSlider.Value.ToString();
        }

        private void EnsureColorEffect()
        {
            if (Lighting.Effect != LightingEffect.Off) return;
            Lighting.Effect = LightingEffect.Static;
            effectGrid.SelectedIndex = (int)LightingEffect.Static;
        }

        private static void Changed()
        {
            AppSettings.MarkChanged();
        }

        private void RefreshPresetList(string select)
        {
            var names = LightingPresetStore.ListNames();
            presetGrid.Items = names.ToArray();
            presetGrid.SelectedIndex = select == null ? -1 : names.IndexOf(select);
        }

        private void ApplyPreset()
        {
            if (presetGrid.SelectedIndex < 0) return;
            ZoneLighting preset = LightingPresetStore.Load(presetGrid.Items[presetGrid.SelectedIndex]);
            if (preset == null) return;
            AppSettings.KeyboardLighting = preset;
            LoadFromSettings();
            presetGrid.SelectedIndex = -1;
            Changed();
        }

        private void SavePreset()
        {
            string name = TextPromptDialog.Ask("Сохранить пресет", "Название пресета:");
            if (string.IsNullOrEmpty(name)) return;
            LightingPresetStore.Save(name, Lighting);
            RefreshPresetList(name);
        }

        private void DeletePreset()
        {
            if (presetGrid.SelectedIndex < 0) return;
            LightingPresetStore.Delete(presetGrid.Items[presetGrid.SelectedIndex]);
            RefreshPresetList(null);
        }
    }
}
