using System.Drawing;
using System.Windows.Forms;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Controls
{
    /// <summary>Small always-visible readout (title above a value) used in the header for fan speeds.</summary>
    public class StatChip : Control
    {
        public string Title = "";
        public string Value = "—";
        public bool IsAlert;

        public StatChip()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Width = 170;
            Height = 48;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            UiDrawing.SetHighQuality(e.Graphics);
            if (Parent != null) e.Graphics.Clear(Parent.BackColor);
            var area = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            using (var path = UiDrawing.RoundedRectangle(area, 10))
            using (var fill = new SolidBrush(Palette.CardSurface))
            using (var border = new Pen(IsAlert ? Palette.Bad : Palette.Border))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }
            using (var marker = new SolidBrush(IsAlert ? Palette.Bad : Palette.AccentSecondary))
                e.Graphics.FillRectangle(marker, 0, 12, 3, Height - 24);

            TextRenderer.DrawText(e.Graphics, Title, UiDrawing.GetFont(8, false), new Rectangle(14, 5, Width - 20, 16),
                Palette.SubText, TextFormatFlags.Left);
            TextRenderer.DrawText(e.Graphics, Value, UiDrawing.GetFont(13, true), new Rectangle(14, 20, Width - 20, 26),
                IsAlert ? Palette.Bad : Palette.Text, TextFormatFlags.Left);
        }
    }
}
