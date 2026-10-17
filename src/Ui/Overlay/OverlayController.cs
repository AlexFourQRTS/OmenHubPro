using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Engine;
using OmenHub.Hardware.Fps;
using OmenHub.Hardware.Sensors;

namespace OmenHub.Ui.Overlay
{
    /// <summary>Shows the overlay according to the settings (switch, mode, corner) and redraws it once per second.</summary>
    public sealed class OverlayController : IDisposable
    {
        private const int MarginPixels = 20;
        private readonly OverlayWindow window = new OverlayWindow();
        private readonly Timer timer = new Timer();

        public OverlayController()
        {
            timer.Interval = 1000;
            timer.Tick += delegate { Refresh(); };
            timer.Start();
        }

        private static float Scale { get { return AppSettings.OverlayScalePercent / 100f; } }

        private static bool ShouldShow()
        {
            if (!AppSettings.OverlayEnabled) return false;
            return AppSettings.Overlay == OverlayMode.Always || GameDetector.IsGameInForeground();
        }

        private void Refresh()
        {
            bool show = ShouldShow();
            CpuPackageSensor.Wanted = show && AppSettings.OverlayShowCpu && AppSettings.OverlayShowPower;
            FpsCounter.Wanted = show && AppSettings.OverlayShowFps;
            if (!show) { if (window.Visible) window.Hide(); return; }
            Size size = OverlayPainter.MeasureSize(Scale);
            if (!window.Visible) window.Show();
            using (var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                    OverlayPainter.Paint(graphics, size, ControlEngine.Latest, CurrentFps(), Scale);
                window.Present(bitmap, Corner(size));
            }
        }

        private static int CurrentFps()
        {
            return AppSettings.OverlayShowFps ? FpsCounter.FramesPerSecond(GameDetector.ForegroundProcessId()) : -1;
        }

        private static Point Corner(Size size)
        {
            Rectangle area = Screen.PrimaryScreen.Bounds;
            bool right = AppSettings.OverlayPosition == OverlayCorner.TopRight || AppSettings.OverlayPosition == OverlayCorner.BottomRight;
            bool bottom = AppSettings.OverlayPosition == OverlayCorner.BottomLeft || AppSettings.OverlayPosition == OverlayCorner.BottomRight;
            return new Point(right ? area.Right - size.Width - MarginPixels : area.Left + MarginPixels,
                bottom ? area.Bottom - size.Height - MarginPixels : area.Top + MarginPixels);
        }

        public void Dispose()
        {
            timer.Stop();
            window.Close();
        }
    }
}
