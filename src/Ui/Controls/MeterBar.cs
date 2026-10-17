using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Controls
{
    /// <summary>Thin progress bar. Fraction is 0..1; WarningColor overrides the gradient (heat warnings).</summary>
    public class MeterBar : Control
    {
        public double Fraction;
        public Color? WarningColor;

        public MeterBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 8;
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            UiDrawing.SetHighQuality(eventArgs.Graphics);
            if (Parent != null) eventArgs.Graphics.Clear(Parent.BackColor);

            var trackArea = new RectangleF(0, 0, Width - 1, Height - 1);
            using (var path = UiDrawing.RoundedRectangle(trackArea, Height / 2f))
            using (var brush = new SolidBrush(Palette.Track))
                eventArgs.Graphics.FillPath(brush, path);

            double clamped = Math.Max(0, Math.Min(1, Fraction));
            if (clamped <= 0.01) return;
            var filledArea = new RectangleF(0, 0, (float)(clamped * (Width - 1)), Height - 1);
            Color startColor = WarningColor ?? Palette.Accent;
            Color endColor = WarningColor ?? Palette.GradientEnd;
            using (var path = UiDrawing.RoundedRectangle(filledArea, Height / 2f))
            using (var gradient = new LinearGradientBrush(new RectangleF(0, 0, Math.Max(2, filledArea.Width), Height), startColor, endColor, 0f))
                eventArgs.Graphics.FillPath(gradient, path);
        }
    }
}
