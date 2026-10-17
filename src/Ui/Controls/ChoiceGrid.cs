using System;
using System.Drawing;
using System.Windows.Forms;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Controls
{
    /// <summary>A wrapping grid of option buttons (one selected). Code changes of SelectedIndex never raise the event.</summary>
    public class ChoiceGrid : Control
    {
        private const int CellHeight = 34;
        private const int Gap = 6;
        private string[] items = new string[0];
        private int selectedIndex = -1;

        public int Columns = 6;
        public event EventHandler SelectionChanged;

        public string[] Items
        {
            get { return items; }
            set { items = value ?? new string[0]; Height = Math.Max(CellHeight, RowCount * (CellHeight + Gap) - Gap); Invalidate(); }
        }

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set { selectedIndex = value; Invalidate(); }
        }

        private int RowCount { get { return Math.Max(1, (items.Length + Columns - 1) / Columns); } }

        public ChoiceGrid()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        private RectangleF CellOf(int index)
        {
            float cellWidth = (Width - (Columns - 1) * Gap) / (float)Columns;
            return new RectangleF((index % Columns) * (cellWidth + Gap), (index / Columns) * (CellHeight + Gap), cellWidth, CellHeight);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            for (int index = 0; index < items.Length; index++)
            {
                if (!CellOf(index).Contains(e.X, e.Y)) continue;
                selectedIndex = index;
                Invalidate();
                if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
                return;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            UiDrawing.SetHighQuality(e.Graphics);
            if (Parent != null) e.Graphics.Clear(Parent.BackColor);
            for (int index = 0; index < items.Length; index++)
            {
                RectangleF cell = CellOf(index);
                bool selected = index == selectedIndex;
                using (var path = UiDrawing.RoundedRectangle(cell, 8))
                using (var brush = new SolidBrush(selected ? Palette.Accent : Palette.Track))
                    e.Graphics.FillPath(brush, path);
                TextRenderer.DrawText(e.Graphics, items[index], UiDrawing.GetFont(9, selected), Rectangle.Round(cell),
                    selected ? Color.White : Palette.Text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }
    }
}
