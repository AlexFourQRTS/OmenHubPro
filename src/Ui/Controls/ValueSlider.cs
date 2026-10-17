using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Controls
{
    /// <summary>Horizontal slider with snapping. Assigning Value by code never raises ValueChanged.</summary>
    public class ValueSlider : Control
    {
        private const int HorizontalPadding = 10;
        private int currentValue;
        private bool isDragging;

        public int Minimum = 0;
        public int Maximum = 100;
        public int Step = 1;
        public event EventHandler ValueChanged;

        public int Value
        {
            get { return currentValue; }
            set { currentValue = SnapToStep(value); Invalidate(); }
        }

        public ValueSlider()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Height = 28;
        }

        private int SnapToStep(int value)
        {
            int snapped = (int)Math.Round((double)(value - Minimum) / Step) * Step + Minimum;
            return Math.Max(Minimum, Math.Min(Maximum, snapped));
        }

        private void MoveToPosition(int mouseX)
        {
            double fraction = (mouseX - (double)HorizontalPadding) / Math.Max(1, Width - 2 * HorizontalPadding);
            int newValue = SnapToStep((int)(Minimum + fraction * (Maximum - Minimum)));
            if (newValue == currentValue) return;
            currentValue = newValue;
            Invalidate();
            if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
        }

        protected override void OnMouseDown(MouseEventArgs e) { isDragging = true; Capture = true; MoveToPosition(e.X); }
        protected override void OnMouseMove(MouseEventArgs e) { if (isDragging) MoveToPosition(e.X); }
        protected override void OnMouseUp(MouseEventArgs e) { isDragging = false; Capture = false; }

        protected override void OnPaint(PaintEventArgs e)
        {
            UiDrawing.SetHighQuality(e.Graphics);
            if (Parent != null) e.Graphics.Clear(Parent.BackColor);
            float fraction = (float)(currentValue - Minimum) / Math.Max(1, Maximum - Minimum);
            float thumbX = HorizontalPadding + fraction * (Width - 2 * HorizontalPadding);
            float centerY = Height / 2f;
            float trackWidth = Width - 2 * HorizontalPadding;

            using (var track = new SolidBrush(Palette.Track))
                e.Graphics.FillRectangle(track, HorizontalPadding, centerY - 3, trackWidth, 6);

            float filledWidth = thumbX - HorizontalPadding;
            if (filledWidth > 0)
            {
                var filledArea = new RectangleF(HorizontalPadding, centerY - 3, Math.Max(2, filledWidth), 6);
                using (var gradient = new LinearGradientBrush(filledArea, Palette.Accent, Palette.GradientEnd, 0f))
                    e.Graphics.FillRectangle(gradient, filledArea);
            }
            using (var thumb = new SolidBrush(Color.White))
                e.Graphics.FillEllipse(thumb, thumbX - 9, centerY - 9, 18, 18);
            using (var outline = new Pen(Palette.Accent, 3))
                e.Graphics.DrawEllipse(outline, thumbX - 8, centerY - 8, 16, 16);
        }
    }
}
