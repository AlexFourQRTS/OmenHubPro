using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OmenHub.Ui.Overlay
{
    /// <summary>
    /// A borderless, always-on-top layered window that never takes focus and lets every mouse click through to the game.
    /// Its picture is a bitmap with per-pixel alpha, so the panel and the text can have different transparency.
    /// Nothing is injected into any game: it is a normal window drawn above the others.
    /// </summary>
    public sealed class OverlayWindow : Form
    {
        private const int ExtendedLayered = 0x80000;
        private const int ExtendedTransparent = 0x20;
        private const int ExtendedToolWindow = 0x80;
        private const int ExtendedNoActivate = 0x08000000;
        private const int UpdateLayeredAlpha = 2;
        private const byte AlphaSourceOver = 0, AlphaFormatPerPixel = 1;

        [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] private struct NativeSize { public int Width, Height; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] private struct BlendFunction { public byte Operation, Flags, SourceAlpha, Format; }

        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr context);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr context);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr context);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr context, IntPtr gdiObject);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr gdiObject);
        [DllImport("user32.dll")]
        private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr destination, ref NativePoint position, ref NativeSize size,
            IntPtr source, ref NativePoint sourcePosition, int colorKey, ref BlendFunction blend, int flags);

        public OverlayWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= ExtendedLayered | ExtendedTransparent | ExtendedToolWindow | ExtendedNoActivate;
                return parameters;
            }
        }

        /// <summary>Shows the bitmap (with per-pixel alpha) at the given screen position.</summary>
        public void Present(Bitmap bitmap, Point location)
        {
            IntPtr screenContext = GetDC(IntPtr.Zero);
            IntPtr memoryContext = CreateCompatibleDC(screenContext);
            IntPtr bitmapHandle = bitmap.GetHbitmap(Color.FromArgb(0));
            IntPtr previous = SelectObject(memoryContext, bitmapHandle);
            try
            {
                var position = new NativePoint { X = location.X, Y = location.Y };
                var size = new NativeSize { Width = bitmap.Width, Height = bitmap.Height };
                var origin = new NativePoint();
                var blend = new BlendFunction { Operation = AlphaSourceOver, SourceAlpha = 255, Format = AlphaFormatPerPixel };
                UpdateLayeredWindow(Handle, screenContext, ref position, ref size, memoryContext, ref origin, 0, ref blend, UpdateLayeredAlpha);
            }
            finally
            {
                SelectObject(memoryContext, previous);
                DeleteObject(bitmapHandle);
                DeleteDC(memoryContext);
                ReleaseDC(IntPtr.Zero, screenContext);
            }
        }
    }
}
