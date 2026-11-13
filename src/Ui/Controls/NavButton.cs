using System;
using System.Drawing;
using System.Windows.Forms;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Controls
{
    /// <summary>Sidebar navigation entry in Google style: a rounded pill marks the active page.</summary>
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

        protected override void OnMouseEnter(EventArgs eventArgs) { isHovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs eventArgs) { isHovered = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            UiDrawing.SetHighQuality(eventArgs.Graphics);
            eventArgs.Graphics.Clear(Palette.Sidebar);
            if (IsActive || isHovered)
            {
                var area = new RectangleF(10, 2, Width - 20, Height - 4);
                Color pill = IsActive ? Palette.SelectedFill : Palette.Border;
                using (var path = UiDrawing.RoundedRectangle(area, (Height - 4) / 2f))
                using (var brush = new SolidBrush(IsActive ? pill : Color.FromArgb(70, pill)))
                    eventArgs.Graphics.FillPath(brush, path);
            }

            TextRenderer.DrawText(eventArgs.Graphics, Text, UiDrawing.GetFont(10.5f, IsActive), new Rectangle(24, 0, Width - 24, Height),
                IsActive ? Palette.Accent : Palette.SubText, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }
}
