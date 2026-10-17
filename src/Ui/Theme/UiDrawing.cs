using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace OmenHub.Ui.Theme
{
    /// <summary>Shared drawing helpers: cached fonts, rounded rectangles, quality settings.</summary>
    public static class UiDrawing
    {
        private const string FontFamily = "Segoe UI";
        private static readonly Dictionary<string, Font> fontCache = new Dictionary<string, Font>();

        public static Font GetFont(float size, bool bold)
        {
            string key = size + (bold ? "b" : "r");
            Font font;
            if (!fontCache.TryGetValue(key, out font))
            {
                font = new Font(FontFamily, size, bold ? FontStyle.Bold : FontStyle.Regular);
                fontCache[key] = font;
            }
            return font;
        }

        public static GraphicsPath RoundedRectangle(RectangleF area, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2;
            path.AddArc(area.X, area.Y, diameter, diameter, 180, 90);
            path.AddArc(area.Right - diameter, area.Y, diameter, diameter, 270, 90);
            path.AddArc(area.Right - diameter, area.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(area.X, area.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void SetHighQuality(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        }
    }
}
