using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

// Generates src\app.ico (own artwork: gradient ring on a dark rounded tile). Run: MakeIcon.exe <out.ico>
class MakeIcon
{
    static byte[] Render(int s)
    {
        using (var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.Transparent);
            float r = s * 0.22f; var tile = new RectangleF(s * 0.02f, s * 0.02f, s * 0.96f, s * 0.96f);
            using (var p = new GraphicsPath())
            {
                float d = r * 2;
                p.AddArc(tile.X, tile.Y, d, d, 180, 90); p.AddArc(tile.Right - d, tile.Y, d, d, 270, 90);
                p.AddArc(tile.Right - d, tile.Bottom - d, d, d, 0, 90); p.AddArc(tile.X, tile.Bottom - d, d, d, 90, 90); p.CloseFigure();
                using (var b = new LinearGradientBrush(tile, Color.FromArgb(0x1B, 0x14, 0x2B), Color.FromArgb(0x08, 0x06, 0x0E), 90f)) g.FillPath(b, p);
            }
            float w = s * 0.13f, m = s * 0.25f;
            var ring = new RectangleF(m, m, s - 2 * m, s - 2 * m);
            using (var br = new LinearGradientBrush(new RectangleF(0, 0, s, s), Color.FromArgb(0xA7, 0x7B, 0xFF), Color.FromArgb(0xFF, 0x8A, 0x1F), 45f))
            using (var pen = new Pen(br, w)) { pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round; g.DrawArc(pen, ring, -60, 330); }
            using (var ms = new MemoryStream()) { bmp.Save(ms, ImageFormat.Png); return ms.ToArray(); }
        }
    }

    static void Main(string[] a)
    {
        int[] sizes = { 16, 24, 32, 48, 64, 128, 256 };
        var imgs = new byte[sizes.Length][];
        for (int i = 0; i < sizes.Length; i++) imgs[i] = Render(sizes[i]);
        using (var f = new FileStream(a[0], FileMode.Create))
        using (var w = new BinaryWriter(f))
        {
            w.Write((short)0); w.Write((short)1); w.Write((short)sizes.Length);
            int off = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                int s = sizes[i];
                w.Write((byte)(s == 256 ? 0 : s)); w.Write((byte)(s == 256 ? 0 : s)); w.Write((byte)0); w.Write((byte)0);
                w.Write((short)1); w.Write((short)32); w.Write(imgs[i].Length); w.Write(off); off += imgs[i].Length;
            }
            foreach (var b in imgs) w.Write(b);
        }
    }
}
