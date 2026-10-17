using System;
using System.Drawing;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Sensors;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Overlay
{
    /// <summary>
    /// Draws the overlay. Rows: CPU and GPU (temperature, power, fan speed), memory with the C / G load in percent,
    /// and FPS. Which rows and values appear is chosen in the settings; the panel width follows the content.
    /// </summary>
    public static class OverlayPainter
    {
        private const int RowHeight = 28;
        private const int Padding = 10;
        private const int LabelWidth = 52;
        private const int TemperatureWidth = 78, PowerWidth = 88, FanWidth = 118, MemoryWidth = 270, FpsWidth = 90;
        private static readonly Color PanelBase = Color.FromArgb(18, 14, 28);
        private static readonly Color ValueBase = Color.FromArgb(240, 236, 250);

        private static int RowCount
        {
            get
            {
                return (AppSettings.OverlayShowCpu ? 1 : 0) + (AppSettings.OverlayShowGpu ? 1 : 0)
                    + (AppSettings.OverlayShowRam ? 1 : 0) + (AppSettings.OverlayShowFps ? 1 : 0);
            }
        }

        private static int ContentWidth
        {
            get
            {
                int devices = (AppSettings.OverlayShowTemperature ? TemperatureWidth : 0) + (AppSettings.OverlayShowPower ? PowerWidth : 0)
                    + (AppSettings.OverlayShowFan ? FanWidth : 0);
                bool deviceRows = AppSettings.OverlayShowCpu || AppSettings.OverlayShowGpu;
                return Math.Max(Math.Max(deviceRows ? devices : 0, AppSettings.OverlayShowRam ? MemoryWidth : 0), AppSettings.OverlayShowFps ? FpsWidth : 0);
            }
        }

        public static Size MeasureSize(float scale)
        {
            int width = Padding + LabelWidth + Math.Max(60, ContentWidth) + Padding;
            int height = Math.Max(1, RowCount) * RowHeight + 2 * Padding;
            return new Size((int)(width * scale), (int)(height * scale));
        }

        public static void Paint(Graphics graphics, Size size, SensorSnapshot snapshot, int fps, float scale)
        {
            UiDrawing.SetHighQuality(graphics);
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;   // ClearType cannot draw on a transparent bitmap
            using (var panel = UiDrawing.RoundedRectangle(new RectangleF(0, 0, size.Width - 1, size.Height - 1), 10 * scale))
            using (var fill = new SolidBrush(WithOpacity(PanelBase, AppSettings.OverlayFillOpacityPercent)))
                graphics.FillPath(fill, panel);

            using (var font = new Font("Consolas", 12.5f * scale, FontStyle.Bold))
            {
                Color label = Color.FromArgb(AppSettings.OverlayColor[0], AppSettings.OverlayColor[1], AppSettings.OverlayColor[2]);
                int row = 0;
                if (AppSettings.OverlayShowCpu)
                    DrawDevice(graphics, font, scale, row++, label, "CPU", snapshot.CpuTemperature, snapshot.CpuPowerWatts, snapshot.CpuFanRpm);
                if (AppSettings.OverlayShowGpu)
                    DrawDevice(graphics, font, scale, row++, label, "GPU", snapshot.GpuTemperature, snapshot.GpuPowerWatts, snapshot.GpuFanRpm);
                if (AppSettings.OverlayShowRam) DrawMemory(graphics, font, scale, row++, label, snapshot);
                if (AppSettings.OverlayShowFps) DrawFps(graphics, font, scale, row, label, fps);
            }
        }

        private static void DrawDevice(Graphics graphics, Font font, float scale, int row, Color label, string name, int temperature, double watts, int fanRpm)
        {
            float top = (Padding + row * RowHeight) * scale;
            Draw(graphics, font, name, Padding * scale, top, label);
            float left = (Padding + LabelWidth) * scale;
            if (AppSettings.OverlayShowTemperature)
            {
                Draw(graphics, font, temperature < 0 ? "--°C" : temperature + "°C", left, top, TemperatureColor(temperature));
                left += TemperatureWidth * scale;
            }
            if (AppSettings.OverlayShowPower)
            {
                Draw(graphics, font, watts < 0 ? "--Вт" : watts.ToString("0") + "Вт", left, top, ValueBase);
                left += PowerWidth * scale;
            }
            if (AppSettings.OverlayShowFan)
                Draw(graphics, font, fanRpm < 0 ? "---- об/м" : fanRpm + " об/м", left, top, ValueBase);
        }

        /// <summary>Memory in use, then the load in percent: C for the processor, G for the NVIDIA graphics card.</summary>
        private static void DrawMemory(Graphics graphics, Font font, float scale, int row, Color label, SensorSnapshot snapshot)
        {
            float top = (Padding + row * RowHeight) * scale;
            Draw(graphics, font, "RAM", Padding * scale, top, label);
            string memory = snapshot.RamUsedGb < 0 ? "--" : snapshot.RamUsedGb.ToString("0.0") + "ГБ";
            string cpuLoad = snapshot.CpuLoadPercent < 0 ? "--" : snapshot.CpuLoadPercent.ToString("0");
            string gpuLoad = snapshot.GpuUtilizationPercent < 0 ? "--" : snapshot.GpuUtilizationPercent.ToString();
            float left = (Padding + LabelWidth) * scale;
            Draw(graphics, font, memory, left, top, ValueBase);
            Draw(graphics, font, "C " + cpuLoad + "%", left + 88 * scale, top, ValueBase);
            Draw(graphics, font, "G " + gpuLoad + "%", left + 178 * scale, top, ValueBase);
        }

        private static void DrawFps(Graphics graphics, Font font, float scale, int row, Color label, int fps)
        {
            float top = (Padding + row * RowHeight) * scale;
            Draw(graphics, font, "FPS", Padding * scale, top, label);
            Draw(graphics, font, fps < 0 ? "--" : fps.ToString(), (Padding + LabelWidth) * scale, top, ValueBase);
        }

        private static void Draw(Graphics graphics, Font font, string text, float left, float top, Color color)
        {
            using (var brush = new SolidBrush(WithOpacity(color, AppSettings.OverlayTextOpacityPercent)))
                graphics.DrawString(text, font, brush, left, top);
        }

        private static Color WithOpacity(Color color, int percent)
        {
            return Color.FromArgb(Math.Max(0, Math.Min(255, 255 * percent / 100)), color.R, color.G, color.B);
        }

        private static Color TemperatureColor(int temperature)
        {
            if (temperature >= 85) return Color.FromArgb(255, 77, 94);
            return temperature >= 75 ? Color.FromArgb(255, 160, 60) : ValueBase;
        }
    }
}
