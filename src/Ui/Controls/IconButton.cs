using System;
using System.Drawing;
using System.Windows.Forms;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Controls
{
    /// <summary>Round Google-style icon button: a sun / moon (theme) or a power symbol (autostart, filled while on).</summary>
    public class IconButton : Control
    {
        public enum Glyph { Theme, Power }

        private bool isHovered;
        private Color currentBackground;

        public Glyph Icon = Glyph.Theme;
        public bool IsOn;   // theme: dark; power: autostart enabled

        public IconButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Width = Height = 40;
        }

        protected override void OnMouseEnter(EventArgs eventArgs) { isHovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs eventArgs) { isHovered = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            Graphics graphics = eventArgs.Graphics;
            UiDrawing.SetHighQuality(graphics);
            if (Parent != null) graphics.Clear(Parent.BackColor);
            bool filled = Icon == Glyph.Power && IsOn;
            currentBackground = filled ? Palette.SelectedFill : isHovered ? Palette.Track : Palette.CardSurface;
            using (var brush = new SolidBrush(currentBackground)) graphics.FillEllipse(brush, 1, 1, Width - 3, Height - 3);
            using (var border = new Pen(Palette.Border)) graphics.DrawEllipse(border, 1, 1, Width - 3, Height - 3);
            Color ink = filled ? Palette.Accent : Palette.SubText;
            if (Icon == Glyph.Theme) DrawTheme(graphics, ink); else DrawPower(graphics, ink);
        }

        private void DrawTheme(Graphics graphics, Color ink)
        {
            float centerX = Width / 2f, centerY = Height / 2f;
            using (var brush = new SolidBrush(ink))
            using (var pen = new Pen(ink, 1.8f))
            {
                if (IsOn)   // dark theme now: show a sun (click switches to light)
                {
                    graphics.FillEllipse(brush, centerX - 4, centerY - 4, 8, 8);
                    for (int ray = 0; ray < 8; ray++)
                    {
                        double angle = ray * Math.PI / 4;
                        graphics.DrawLine(pen, centerX + (float)Math.Cos(angle) * 7, centerY + (float)Math.Sin(angle) * 7,
                            centerX + (float)Math.Cos(angle) * 10, centerY + (float)Math.Sin(angle) * 10);
                    }
                }
                else        // light theme now: show a moon
                {
                    graphics.FillEllipse(brush, centerX - 8, centerY - 8, 16, 16);
                        using (var cut = new SolidBrush(currentBackground)) graphics.FillEllipse(cut, centerX - 2, centerY - 11, 15, 15);
                }
            }
        }

        private void DrawPower(Graphics graphics, Color ink)
        {
            float centerX = Width / 2f, centerY = Height / 2f;
            using (var pen = new Pen(ink, 2f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round })
            {
                graphics.DrawArc(pen, centerX - 8, centerY - 7, 16, 16, -60, 300);
                graphics.DrawLine(pen, centerX, centerY - 10, centerX, centerY - 1);
            }
        }
    }
}
