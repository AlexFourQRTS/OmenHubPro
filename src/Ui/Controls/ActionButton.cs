using System;
using System.Drawing;
using System.Windows.Forms;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Controls
{
    /// <summary>Flat rounded button; accent-filled when IsAccent is true, subdued otherwise.</summary>
    public class ActionButton : Control
    {
        private bool isHovered;
        private bool isPressed;

        public bool IsAccent = true;

        public ActionButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Height = 34;
        }

        protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { isHovered = false; isPressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { isPressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { isPressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            UiDrawing.SetHighQuality(e.Graphics);
            if (Parent != null) e.Graphics.Clear(Parent.BackColor);

            Color fill = IsAccent ? Palette.Accent : Palette.Track;
            if (!Enabled) fill = Palette.Track;
            else if (isPressed) fill = ControlPaint.Dark(fill, 0.1f);
            else if (isHovered) fill = ControlPaint.Light(fill, 0.15f);

            var area = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            using (var path = UiDrawing.RoundedRectangle(area, 8))
            using (var brush = new SolidBrush(fill))
                e.Graphics.FillPath(brush, path);

            Color textColor = !Enabled ? Palette.SubText : IsAccent ? Color.White : Palette.Text;
            TextRenderer.DrawText(e.Graphics, Text, UiDrawing.GetFont(9.5f, true), new Rectangle(0, 0, Width, Height),
                textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
