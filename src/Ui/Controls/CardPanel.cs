using System.Drawing;
using System.Windows.Forms;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Controls
{
    /// <summary>Rounded panel with a thin border; the basic container of every page.</summary>
    public class CardPanel : Panel
    {
        private const float CornerRadius = 12;

        public CardPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            UiDrawing.SetHighQuality(eventArgs.Graphics);
            if (Parent != null) eventArgs.Graphics.Clear(Parent.BackColor);
            var area = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            using (var path = UiDrawing.RoundedRectangle(area, CornerRadius))
            using (var fill = new SolidBrush(Palette.CardSurface))
            using (var border = new Pen(Palette.Border))
            {
                eventArgs.Graphics.FillPath(fill, path);
                eventArgs.Graphics.DrawPath(border, path);
            }
        }
    }
}
