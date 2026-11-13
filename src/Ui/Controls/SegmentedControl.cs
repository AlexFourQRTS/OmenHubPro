using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Controls
{
    /// <summary>A row of mutually exclusive options. SelectedIndex changes by code never raise the event.</summary>
    public class SegmentedControl : Control
    {
        private int selectedIndex;

        public string[] Items = new string[0];
        public event EventHandler SelectionChanged;

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set { selectedIndex = value; Invalidate(); }
        }

        public SegmentedControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Height = 36;
        }

        protected override void OnMouseDown(MouseEventArgs eventArgs)
        {
            if (Items.Length == 0) return;
            int clickedIndex = Math.Max(0, Math.Min(Items.Length - 1, eventArgs.X * Items.Length / Math.Max(1, Width)));
            if (clickedIndex == selectedIndex) return;
            selectedIndex = clickedIndex;
            Invalidate();
            if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            UiDrawing.SetHighQuality(eventArgs.Graphics);
            if (Parent != null) eventArgs.Graphics.Clear(Parent.BackColor);
            var outer = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            if (Items.Length == 0) return;

            float segmentWidth = (Width - 4f) / Items.Length;
            var selected = new RectangleF(2 + selectedIndex * segmentWidth, 2.5f, segmentWidth, Height - 5);
            using (var path = UiDrawing.RoundedRectangle(selected, selected.Height / 2f))
            using (var brush = new SolidBrush(Palette.SelectedFill))
                eventArgs.Graphics.FillPath(brush, path);
            using (var path = UiDrawing.RoundedRectangle(outer, outer.Height / 2f))
            using (var border = new Pen(Palette.Border))
                eventArgs.Graphics.DrawPath(border, path);

            for (int index = 0; index < Items.Length; index++)
            {
                var textArea = new Rectangle((int)(2 + index * segmentWidth), 0, (int)segmentWidth, Height);
                TextRenderer.DrawText(eventArgs.Graphics, Items[index], UiDrawing.GetFont(9.5f, index == selectedIndex), textArea,
                    index == selectedIndex ? Palette.Accent : Palette.Text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }
    }
}
