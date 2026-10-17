using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace OmenHub
{
    // ---------------------------------------------------------------- theme
    static class Th
    {
        public static Color Bg, Side, Card, Border, Text, Sub, Accent, Accent2, Good, Bad, Track;
        public static void Set(bool dark)
        {
            if (dark)
            {
                Bg = C(0x0C0A12); Side = C(0x110E1A); Card = C(0x181322); Border = C(0x2B2340);
                Text = C(0xF3EFFA); Sub = C(0x9B91B0); Accent = C(0x9B6BFF); Accent2 = C(0xFF8A1F);
                Good = C(0x3DDC97); Bad = C(0xFF4D5E); Track = C(0x2A2238);
            }
            else
            {
                Bg = C(0xF8F9FA); Side = C(0xFFFFFF); Card = C(0xFFFFFF); Border = C(0xDADCE0);
                Text = C(0x202124); Sub = C(0x5F6368); Accent = C(0x1A73E8); Accent2 = C(0xF29900);
                Good = C(0x1E8E3E); Bad = C(0xD93025); Track = C(0xE8EAED);
            }
        }
        static Color C(int rgb) { return Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255); }
        static readonly Dictionary<string, Font> fonts = new Dictionary<string, Font>();
        public static Font F(float size, bool bold)
        {
            string k = size + (bold ? "b" : "r");
            Font f;
            if (!fonts.TryGetValue(k, out f)) { f = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular); fonts[k] = f; }
            return f;
        }
        public static GraphicsPath RR(RectangleF r, float rad)
        {
            var p = new GraphicsPath(); float d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }
        public static void Quality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        }
    }

    // ---------------------------------------------------------------- controls
    class Card : Panel
    {
        public Card() { DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Th.Quality(g);
            if (Parent != null) g.Clear(Parent.BackColor);
            var r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            using (var p = Th.RR(r, 12))
            using (var b = new SolidBrush(Th.Card))
            using (var pen = new Pen(Th.Border)) { g.FillPath(b, p); g.DrawPath(pen, p); }
        }
    }

    class Btn : Control
    {
        bool hover, down; public bool Accent = true;
        public Btn() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Cursor = Cursors.Hand; Height = 34; }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Th.Quality(g);
            if (Parent != null) g.Clear(Parent.BackColor);
            var r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            Color fill = Accent ? Th.Accent : Th.Track;
            if (hover) fill = ControlPaint.Light(fill, 0.15f);
            if (down) fill = ControlPaint.Dark(fill, 0.1f);
            using (var p = Th.RR(r, 8)) using (var b = new SolidBrush(fill)) g.FillPath(b, p);
            Color tc = Accent ? Color.White : Th.Text;
            TextRenderer.DrawText(g, Text, Th.F(9.5f, true), new Rectangle(0, 0, Width, Height), tc, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    class Nav : Control
    {
        public bool Active; bool hover;
        public Nav() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); Cursor = Cursors.Hand; Height = 44; }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Th.Quality(g); g.Clear(Th.Side);
            if (Active || hover)
            {
                var r = new RectangleF(8, 2, Width - 16, Height - 4);
                using (var p = Th.RR(r, 8)) using (var b = new SolidBrush(Active ? Color.FromArgb(40, Th.Accent) : Color.FromArgb(18, Th.Accent))) g.FillPath(b, p);
            }
            if (Active) using (var b = new SolidBrush(Th.Accent2)) g.FillRectangle(b, 0, 10, 4, Height - 20);
            TextRenderer.DrawText(g, Text, Th.F(10.5f, Active), new Rectangle(24, 0, Width - 24, Height), Active ? Th.Text : Th.Sub, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }

    class Seg : Control
    {
        public string[] Items = new string[0]; int sel; public event EventHandler Changed;
        public int Sel { get { return sel; } set { sel = value; Invalidate(); } }
        public Seg() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Cursor = Cursors.Hand; Height = 36; }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (Items.Length == 0) return;
            int i = Math.Max(0, Math.Min(Items.Length - 1, e.X * Items.Length / Math.Max(1, Width)));
            if (i != sel) { sel = i; Invalidate(); if (Changed != null) Changed(this, EventArgs.Empty); }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Th.Quality(g);
            if (Parent != null) g.Clear(Parent.BackColor);
            var full = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            using (var p = Th.RR(full, 10)) using (var b = new SolidBrush(Th.Track)) g.FillPath(b, p);
            if (Items.Length == 0) return;
            float w = (Width - 4f) / Items.Length;
            var r = new RectangleF(2 + sel * w, 2, w, Height - 4);
            using (var p = Th.RR(r, 8))
            using (var br = new LinearGradientBrush(r, Th.Accent, Cfg.Dark ? Th.Accent2 : Th.Accent, 0f)) g.FillPath(br, p);
            for (int i = 0; i < Items.Length; i++)
                TextRenderer.DrawText(g, Items[i], Th.F(9.5f, i == sel), new Rectangle((int)(2 + i * w), 0, (int)w, Height), i == sel ? Color.White : Th.Sub, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    class Slider : Control
    {
        public int Min = 0, Max = 100, Step = 1; int val; bool drag; public event EventHandler Changed;
        public int Value { get { return val; } set { val = Clamp(value); Invalidate(); } }
        public Slider() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Cursor = Cursors.Hand; Height = 28; }
        int Clamp(int v) { v = (int)Math.Round((double)(v - Min) / Step) * Step + Min; return Math.Max(Min, Math.Min(Max, v)); }
        void SetFromX(int x)
        {
            double f = (x - 10.0) / Math.Max(1, Width - 20);
            int v = Clamp((int)(Min + f * (Max - Min)));
            if (v != val) { val = v; Invalidate(); if (Changed != null) Changed(this, EventArgs.Empty); }
        }
        protected override void OnMouseDown(MouseEventArgs e) { drag = true; Capture = true; SetFromX(e.X); }
        protected override void OnMouseMove(MouseEventArgs e) { if (drag) SetFromX(e.X); }
        protected override void OnMouseUp(MouseEventArgs e) { drag = false; Capture = false; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Th.Quality(g);
            if (Parent != null) g.Clear(Parent.BackColor);
            float f = (float)(val - Min) / Math.Max(1, Max - Min);
            float x = 10 + f * (Width - 20), cy = Height / 2f;
            using (var b = new SolidBrush(Th.Track)) g.FillRectangle(b, 10, cy - 3, Width - 20, 6);
            if (x > 10) using (var br = new LinearGradientBrush(new RectangleF(10, cy - 3, Math.Max(2, x - 10), 6), Th.Accent, Cfg.Dark ? Th.Accent2 : Th.Accent, 0f)) g.FillRectangle(br, 10, cy - 3, x - 10, 6);
            using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, x - 9, cy - 9, 18, 18);
            using (var p = new Pen(Th.Accent, 3)) g.DrawEllipse(p, x - 8, cy - 8, 16, 16);
        }
    }

    class Bar : Control
    {
        public double V; public Color? Col;
        public Bar() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Height = 8; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Th.Quality(g);
            if (Parent != null) g.Clear(Parent.BackColor);
            var r = new RectangleF(0, 0, Width - 1, Height - 1);
            using (var p = Th.RR(r, Height / 2f)) using (var b = new SolidBrush(Th.Track)) g.FillPath(b, p);
            double v = Math.Max(0, Math.Min(1, V));
            if (v <= 0.01) return;
            var f = new RectangleF(0, 0, (float)(v * (Width - 1)), Height - 1);
            using (var p = Th.RR(f, Height / 2f))
            using (var br = new LinearGradientBrush(new RectangleF(0, 0, Math.Max(2, f.Width), Height), Col ?? Th.Accent, Col ?? (Cfg.Dark ? Th.Accent2 : Th.Accent), 0f)) g.FillPath(br, p);
        }
    }

    class Curve : Control
    {
        public int[] T = new int[0], R = new int[0]; public double Now = -1;
        public event EventHandler Edited; int drag = -1;
        const int X0 = 56, Y0 = 12, MinT = 30, MaxT = 100, MaxR = 6000;
        public Curve() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); }
        float PX(double t) { return (float)(X0 + (t - MinT) / (MaxT - MinT) * (Width - X0 - 16)); }
        float PY(double r) { return (float)(Y0 + (1 - r / MaxR) * (Height - Y0 - 30)); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Th.Quality(g);
            if (Parent != null) g.Clear(Parent.BackColor);
            using (var pen = new Pen(Th.Border)) using (var br = new SolidBrush(Th.Sub))
            {
                for (int r = 0; r <= MaxR; r += 1500)
                {
                    g.DrawLine(pen, X0, PY(r), Width - 16, PY(r));
                    TextRenderer.DrawText(g, r.ToString(), Th.F(8, false), new Rectangle(0, (int)PY(r) - 8, X0 - 6, 16), Th.Sub, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                }
                for (int t = 30; t <= 100; t += 10)
                {
                    g.DrawLine(pen, PX(t), Y0, PX(t), Height - 30);
                    TextRenderer.DrawText(g, t + "°", Th.F(8, false), new Rectangle((int)PX(t) - 20, Height - 26, 40, 16), Th.Sub, TextFormatFlags.HorizontalCenter);
                }
            }
            using (var rb = new SolidBrush(Color.FromArgb(34, Th.Bad))) g.FillRectangle(rb, PX(80), Y0, Width - 16 - PX(80), Height - 30 - Y0);
            TextRenderer.DrawText(g, "аварийный обдув ≥ 80°", Th.F(8, true), new Rectangle((int)PX(80) + 4, Y0 + 2, 150, 16), Th.Bad, TextFormatFlags.Left);
            if (Now >= MinT)
                using (var pen = new Pen(Th.Accent2, 1.5f) { DashStyle = DashStyle.Dash }) g.DrawLine(pen, PX(Math.Min(Now, MaxT)), Y0, PX(Math.Min(Now, MaxT)), Height - 30);
            if (T.Length < 2) return;
            var pts = new PointF[T.Length];
            for (int i = 0; i < T.Length; i++) pts[i] = new PointF(PX(T[i]), PY(R[i]));
            using (var pen = new Pen(Th.Accent, 3) { LineJoin = LineJoin.Round }) g.DrawLines(pen, pts);
            for (int i = 0; i < pts.Length; i++)
            {
                using (var b = new SolidBrush(i == drag ? Th.Accent2 : Color.White)) g.FillEllipse(b, pts[i].X - 7, pts[i].Y - 7, 14, 14);
                using (var p = new Pen(Th.Accent, 3)) g.DrawEllipse(p, pts[i].X - 6.5f, pts[i].Y - 6.5f, 13, 13);
            }
            if (drag >= 0)
                TextRenderer.DrawText(g, T[drag] + "° → " + R[drag] + " об/мин", Th.F(9, true), new Rectangle((int)pts[drag].X - 70, (int)pts[drag].Y - 30, 140, 18), Th.Text, TextFormatFlags.HorizontalCenter);
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || T.Length >= 12) return;
            double t = MinT + (e.X - X0) / (double)(Width - X0 - 16) * (MaxT - MinT);
            double r = (1 - (e.Y - Y0) / (double)(Height - Y0 - 30)) * MaxR;
            int tt = (int)Math.Round(Math.Max(MinT + 1, Math.Min(MaxT - 1, t))), rr = Math.Max(0, Math.Min(MaxR, (int)Math.Round(r / 100.0) * 100));
            foreach (int x in T) if (x == tt) return;
            var nt = new System.Collections.Generic.List<int>(T); var nr = new System.Collections.Generic.List<int>(R);
            int idx = 0; while (idx < nt.Count && nt[idx] < tt) idx++;
            nt.Insert(idx, tt); nr.Insert(idx, rr);
            T = nt.ToArray(); R = nr.ToArray(); Invalidate(); if (Edited != null) Edited(this, EventArgs.Empty);
        }
        void RemoveNear(int mx, int my)
        {
            if (T.Length <= 3) return;
            int best = -1; double bd = 14 * 14;
            for (int i = 0; i < T.Length; i++) { double dx = PX(T[i]) - mx, dy = PY(R[i]) - my, d = dx * dx + dy * dy; if (d < bd) { bd = d; best = i; } }
            if (best < 0) return;
            var nt = new System.Collections.Generic.List<int>(T); var nr = new System.Collections.Generic.List<int>(R);
            nt.RemoveAt(best); nr.RemoveAt(best);
            T = nt.ToArray(); R = nr.ToArray(); Invalidate(); if (Edited != null) Edited(this, EventArgs.Empty);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) { RemoveNear(e.X, e.Y); return; }
            drag = -1; double best = 14 * 14;
            for (int i = 0; i < T.Length; i++)
            {
                double dx = PX(T[i]) - e.X, dy = PY(R[i]) - e.Y, d = dx * dx + dy * dy;
                if (d < best) { best = d; drag = i; }
            }
            if (drag >= 0) { Capture = true; Invalidate(); }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (drag < 0) return;
            double t = MinT + (e.X - X0) / (double)(Width - X0 - 16) * (MaxT - MinT);
            double r = (1 - (e.Y - Y0) / (double)(Height - Y0 - 30)) * MaxR;
            int lo = drag == 0 ? MinT : T[drag - 1] + 1, hi = drag == T.Length - 1 ? MaxT : T[drag + 1] - 1;
            T[drag] = Math.Max(lo, Math.Min(hi, (int)Math.Round(t)));
            R[drag] = Math.Max(0, Math.Min(MaxR, (int)Math.Round(r / 100.0) * 100));
            Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (drag >= 0) { drag = -1; Capture = false; Invalidate(); if (Edited != null) Edited(this, EventArgs.Empty); }
        }
    }

    class Chip : Control
    {
        public string Title = "", Value = "—"; public bool Alert;
        public Chip() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Width = 170; Height = 48; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Th.Quality(g);
            if (Parent != null) g.Clear(Parent.BackColor);
            var r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            using (var p = Th.RR(r, 10)) using (var b = new SolidBrush(Th.Card)) using (var pen = new Pen(Alert ? Th.Bad : Th.Border)) { g.FillPath(b, p); g.DrawPath(pen, p); }
            using (var b = new SolidBrush(Alert ? Th.Bad : Th.Accent2)) g.FillRectangle(b, 0, 12, 3, Height - 24);
            TextRenderer.DrawText(g, Title, Th.F(8, false), new Rectangle(14, 5, Width - 20, 16), Th.Sub, TextFormatFlags.Left);
            TextRenderer.DrawText(g, Value, Th.F(13, true), new Rectangle(14, 20, Width - 20, 26), Alert ? Th.Bad : Th.Text, TextFormatFlags.Left);
        }
    }
}
