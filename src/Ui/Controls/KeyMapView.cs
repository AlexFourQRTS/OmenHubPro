using System;
using System.Drawing;
using System.Windows.Forms;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Controls
{
    /// <summary>
    /// Keyboard map: one square per lamp at its physical position (millimetres from the device).
    /// Each square shows the lamp's current colour and its lamp number. Left/right clicks raise LampClicked.
    /// </summary>
    public class KeyMapView : Control
    {
        private const int MapPadding = 10;
        private const int CellSize = 33;
        private float[] lampX = new float[0], lampY = new float[0];
        private float scale = 1;
        private int hoveredLamp = -1;

        public Func<int, Color> ColorOfLamp = delegate (int lampIndex) { return Color.Gray; };
        public Func<int, bool> IsBarLamp = delegate (int lampIndex) { return false; };
        public int SelectedLamp = -1;
        public event Action<int, MouseButtons> LampClicked;

        public KeyMapView()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        /// <summary>Sets lamp positions in millimetres (index = lamp number) and fits them into the control.</summary>
        public void SetPositions(float[] xMillimetres, float[] yMillimetres)
        {
            lampX = xMillimetres;
            lampY = yMillimetres;
            float maximumX = 1, maximumY = 1;
            for (int lamp = 0; lamp < lampX.Length; lamp++)
            {
                maximumX = Math.Max(maximumX, lampX[lamp]);
                maximumY = Math.Max(maximumY, lampY[lamp]);
            }
            scale = Math.Min((Width - 2 * MapPadding - CellSize) / maximumX, (Height - 2 * MapPadding - CellSize) / maximumY);
            Invalidate();
        }

        private RectangleF CellOf(int lamp)
        {
            return new RectangleF(MapPadding + lampX[lamp] * scale, MapPadding + lampY[lamp] * scale, CellSize, CellSize);
        }

        private int LampAt(int mouseX, int mouseY)
        {
            for (int lamp = lampX.Length - 1; lamp >= 0; lamp--)
                if (CellOf(lamp).Contains(mouseX, mouseY)) return lamp;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int lamp = LampAt(e.X, e.Y);
            if (lamp == hoveredLamp) return;
            hoveredLamp = lamp;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e) { hoveredLamp = -1; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int lamp = LampAt(e.X, e.Y);
            if (lamp >= 0 && LampClicked != null) LampClicked(lamp, e.Button);
            Invalidate();
        }

        private Color OutlineColor(int lamp)
        {
            if (lamp == SelectedLamp) return Palette.Accent;
            if (IsBarLamp(lamp)) return Palette.AccentSecondary;
            return lamp == hoveredLamp ? Palette.Text : Palette.Border;
        }

        private float OutlineWidth(int lamp)
        {
            return lamp == SelectedLamp ? 3f : IsBarLamp(lamp) ? 2f : lamp == hoveredLamp ? 2f : 1f;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            UiDrawing.SetHighQuality(e.Graphics);
            if (Parent != null) e.Graphics.Clear(Parent.BackColor);
            for (int lamp = 0; lamp < lampX.Length; lamp++)
            {
                RectangleF cell = CellOf(lamp);
                Color fill = ColorOfLamp(lamp);
                using (var path = UiDrawing.RoundedRectangle(cell, 6))
                using (var brush = new SolidBrush(fill))
                using (var border = new Pen(OutlineColor(lamp), OutlineWidth(lamp)))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(border, path);
                }
                bool darkFill = fill.GetBrightness() < 0.5f;
                TextRenderer.DrawText(e.Graphics, lamp.ToString(), UiDrawing.GetFont(7.5f, false), Rectangle.Round(cell),
                    darkFill ? Color.FromArgb(200, 255, 255, 255) : Color.FromArgb(200, 0, 0, 0),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }
}
