using System.Drawing;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>
    /// CPU power limit (20-75 W, step 5), laid out like the GPU card. It is applied by the engine within a moment of
    /// changing the slider. While the "from mains" profile (Pro auto) is on, the profile decides and the slider is locked.
    /// </summary>
    public sealed class CpuPowerCard : CardPanel
    {
        private readonly SegmentedControl limitSegments;
        private readonly ValueSlider wattsSlider;
        private readonly Label wattsLabel;

        public CpuPowerCard(int top)
        {
            Left = 24; Top = top; Width = 802; Height = 92;
            PageKit.MakeLabel(this, "CPU power", 20, 12, 11, true, PageKit.RoleText);
            limitSegments = PageKit.MakeSegments(this, 20, 42, 220, new[] { "Без лимита", "Лимит" }, AppSettings.CpuLimitEnabled ? 1 : 0);
            wattsSlider = PageKit.MakeSlider(this, 260, 46, 420, AppSettings.MinimumCpuWatts, AppSettings.MaximumCpuWatts,
                AppSettings.CpuWattsStep, AppSettings.CpuWatts);
            wattsLabel = PageKit.MakeValueLabel(this, 690, 46, 92, 10.5f, ContentAlignment.MiddleRight);

            limitSegments.SelectionChanged += delegate { OnLimitChosen(); };
            wattsSlider.ValueChanged += delegate { OnSliderMoved(); };
            LoadFromSettings();
        }

        public void LoadFromSettings()
        {
            bool profileLocked = AppSettings.ProAutoEnabled;
            limitSegments.SelectedIndex = (profileLocked || AppSettings.CpuLimitEnabled) ? 1 : 0;
            wattsSlider.Value = profileLocked ? AppSettings.ProAutoWatts : AppSettings.CpuWatts;
            wattsLabel.Text = wattsSlider.Value + " Вт";
        }

        private void OnLimitChosen()
        {
            if (AppSettings.ProAutoEnabled) { LoadFromSettings(); return; }   // the profile decides
            AppSettings.CpuLimitEnabled = limitSegments.SelectedIndex == 1;
            AppSettings.MarkChanged();
        }

        private void OnSliderMoved()
        {
            if (AppSettings.ProAutoEnabled) { LoadFromSettings(); return; }
            AppSettings.CpuWatts = wattsSlider.Value;
            AppSettings.CpuLimitEnabled = true;
            limitSegments.SelectedIndex = 1;
            wattsLabel.Text = wattsSlider.Value + " Вт";
            AppSettings.MarkChanged();
        }
    }
}
