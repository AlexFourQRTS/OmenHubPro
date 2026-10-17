using System;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Sensors;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>Card with the fan curve editor for the CPU fan or the GPU fan, and the temperature source for it.</summary>
    public sealed class CurveCard : CardPanel
    {
        private readonly SegmentedControl fanSegments;
        private readonly SegmentedControl sourceSegments;
        private readonly CurveEditor editor;

        private bool EditingCpuFan { get { return fanSegments.SelectedIndex == 0; } }

        public CurveCard()
        {
            Left = 24; Top = 298; Width = 802; Height = 322;
            PageKit.MakeLabel(this, "Кривая: температура → обороты", 20, 14, 11, true, PageKit.RoleText);
            fanSegments = PageKit.MakeSegments(this, 330, 8, 200, new[] { "Кулер CPU", "Кулер GPU" }, 0);
            sourceSegments = PageKit.MakeSegments(this, 546, 8, 236, new[] { "Система", "GPU", "Макс." }, 0);
            editor = new CurveEditor { Left = 16, Top = 54, Width = 770, Height = 256 };
            Controls.Add(editor);

            fanSegments.SelectionChanged += delegate { LoadFromSettings(); };
            sourceSegments.SelectionChanged += delegate { StoreSource(); };
            editor.Edited += delegate { AppSettings.MarkChanged(); };
            LoadFromSettings();
        }

        public void LoadFromSettings()
        {
            editor.Curve = EditingCpuFan ? AppSettings.CpuCurve : AppSettings.GpuCurve;
            sourceSegments.SelectedIndex = (int)(EditingCpuFan ? AppSettings.CpuCurveSource : AppSettings.GpuCurveSource);
            editor.Invalidate();
        }

        public void ShowSnapshot(SensorSnapshot snapshot)
        {
            double system = snapshot.SystemTemperature;
            double gpu = snapshot.GpuTemperature;
            var source = (TemperatureSource)sourceSegments.SelectedIndex;
            editor.CurrentTemperature = source == TemperatureSource.Gpu ? gpu
                : source == TemperatureSource.System ? system : Math.Max(system, gpu);
            editor.Invalidate();
        }

        private void StoreSource()
        {
            var source = (TemperatureSource)sourceSegments.SelectedIndex;
            if (EditingCpuFan) AppSettings.CpuCurveSource = source;
            else AppSettings.GpuCurveSource = source;
            AppSettings.MarkChanged();
        }
    }
}
