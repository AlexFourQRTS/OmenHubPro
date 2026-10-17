using System;
using System.Drawing;
using System.Windows.Forms;
using OmenHub.Hardware.Lighting;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Controls
{
    /// <summary>
    /// Two-dimensional colour chooser: hue left to right, saturation top (vivid) to bottom (white).
    /// Brightness is a separate slider. ColorChanged is raised only by user input.
    /// </summary>
    public class ColorPad : Control
    {
        private const int CornerRadius = 8;
        private Bitmap gradient;
        private bool isDragging;
        private PointF marker = new PointF(0.08f, 0.0f);

        public byte[] SelectedRgb = { 255, 138, 31 };
        public event EventHandler ColorChanged;

        public ColorPad()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Cross;
            Size = new Size(360, 120);
        }

        /// <summary>Moves the marker to the position that best matches the given colour.</summary>
        public void SetColor(int red, int green, int blue)
        {
            SelectedRgb = new[] { (byte)red, (byte)green, (byte)blue };
            float maximum = Math.Max(red, Math.Max(green, blue)) / 255f;
            float minimum = Math.Min(red, Math.Min(green, blue)) / 255f;
            float saturation = maximum <= 0 ? 0 : (maximum - minimum) / maximum;
            marker = new PointF(HueOf(red, green, blue) / 360f, 1f - saturation);
            Invalidate();
        }

        private static float HueOf(int red, int green, int blue)
        {
            float maximum = Math.Max(red, Math.Max(green, blue));
            float minimum = Math.Min(red, Math.Min(green, blue));
            if (maximum == minimum) return 0;
            float delta = maximum - minimum;
            float hue;
            if (maximum == red) hue = (green - blue) / delta;
            else if (maximum == green) hue = 2 + (blue - red) / delta;
            else hue = 4 + (red - green) / delta;
            hue *= 60;
            return hue < 0 ? hue + 360 : hue;
        }

        private void PickAt(int mouseX, int mouseY)
        {
            float horizontal = Math.Max(0, Math.Min(1, mouseX / (float)Math.Max(1, Width - 1)));
            float vertical = Math.Max(0, Math.Min(1, mouseY / (float)Math.Max(1, Height - 1)));
            marker = new PointF(horizontal, vertical);
            byte[] vivid = LightingEffects.FromHue(Math.Min(359.9, horizontal * 360), 1.0);
            SelectedRgb = new[]
            {
                (byte)(vivid[0] + (255 - vivid[0]) * vertical),
                (byte)(vivid[1] + (255 - vivid[1]) * vertical),
                (byte)(vivid[2] + (255 - vivid[2]) * vertical)
            };
            Invalidate();
            if (ColorChanged != null) ColorChanged(this, EventArgs.Empty);
        }

        protected override void OnMouseDown(MouseEventArgs eventArgs) { isDragging = true; Capture = true; PickAt(eventArgs.X, eventArgs.Y); }
        protected override void OnMouseMove(MouseEventArgs eventArgs) { if (isDragging) PickAt(eventArgs.X, eventArgs.Y); }
        protected override void OnMouseUp(MouseEventArgs eventArgs) { isDragging = false; Capture = false; }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            UiDrawing.SetHighQuality(eventArgs.Graphics);
            if (Parent != null) eventArgs.Graphics.Clear(Parent.BackColor);
            EnsureGradient();
            var area = new RectangleF(0, 0, Width - 1, Height - 1);
            using (var path = UiDrawing.RoundedRectangle(area, CornerRadius))
            {
                eventArgs.Graphics.SetClip(path);
                eventArgs.Graphics.DrawImage(gradient, 0, 0, Width, Height);
                eventArgs.Graphics.ResetClip();
                using (var border = new Pen(Palette.Border)) eventArgs.Graphics.DrawPath(border, path);
            }
            float markerX = marker.X * (Width - 1), markerY = marker.Y * (Height - 1);
            using (var outer = new Pen(Color.White, 3)) eventArgs.Graphics.DrawEllipse(outer, markerX - 8, markerY - 8, 16, 16);
            using (var inner = new Pen(Color.Black, 1)) eventArgs.Graphics.DrawEllipse(inner, markerX - 9, markerY - 9, 18, 18);
        }

        private void EnsureGradient()
        {
            if (gradient != null && gradient.Width == Width && gradient.Height == Height) return;
            if (gradient != null) gradient.Dispose();
            gradient = new Bitmap(Math.Max(1, Width), Math.Max(1, Height));
            for (int column = 0; column < gradient.Width; column++)
            {
                byte[] vivid = LightingEffects.FromHue(Math.Min(359.9, 360.0 * column / gradient.Width), 1.0);
                for (int row = 0; row < gradient.Height; row++)
                {
                    float whiteMix = row / (float)Math.Max(1, gradient.Height - 1);
                    gradient.SetPixel(column, row, Color.FromArgb(
                        (int)(vivid[0] + (255 - vivid[0]) * whiteMix),
                        (int)(vivid[1] + (255 - vivid[1]) * whiteMix),
                        (int)(vivid[2] + (255 - vivid[2]) * whiteMix)));
                }
            }
        }
    }
}
