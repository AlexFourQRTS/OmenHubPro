using System;
using System.Drawing;
using System.Windows.Forms;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Controls
{
    /// <summary>Sidebar navigation entry with an orange marker on the active page.</summary>
    public class NavButton : Control
    {
        private bool isHovered;

        public bool IsActive;

        public NavButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            Height = 44;
        }

        protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            UiDrawing.SetHighQuality(e.Graphics);
            e.Graphics.Clear(Palette.Sidebar);
            if (IsActive || isHovered)
            {
                var area = new RectangleF(8, 2, Width - 16, Height - 4);
                int alpha = IsActive ? 40 : 18;
                using (var path = UiDrawing.RoundedRectangle(area, 8))
                using (var brush = new SolidBrush(Color.FromArgb(alpha, Palette.Accent)))
                    e.Graphics.FillPath(brush, path);
            }
            if (IsActive)
                using (var marker = new SolidBrush(Palette.AccentSecondary))
                    e.Graphics.FillRectangle(marker, 0, 10, 4, Height - 20);

            TextRenderer.DrawText(e.Graphics, Text, UiDrawing.GetFont(10.5f, IsActive), new Rectangle(24, 0, Width - 24, Height),
                IsActive ? Palette.Text : Palette.SubText, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }
}
