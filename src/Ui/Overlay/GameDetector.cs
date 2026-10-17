using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace OmenHub.Ui.Overlay
{
    /// <summary>Decides whether a game is in the foreground: a full-screen window of another program (not the desktop shell).</summary>
    public static class GameDetector
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out NativeRect rectangle);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder name, int capacity);

        private static readonly string[] IgnoredClasses = { "Progman", "WorkerW", "Shell_TrayWnd", "ApplicationFrameWindow" };
        private static readonly string[] IgnoredProcesses = { "explorer", "OmenHub", "SearchHost", "ShellExperienceHost", "StartMenuExperienceHost" };

        public static bool IsGameInForeground()
        {
            IntPtr window = GetForegroundWindow();
            if (window == IntPtr.Zero) return false;

            var className = new StringBuilder(128);
            GetClassName(window, className, className.Capacity);
            if (Array.IndexOf(IgnoredClasses, className.ToString()) >= 0) return false;
            if (IsIgnoredProcess(window)) return false;

            NativeRect rectangle;
            if (!GetWindowRect(window, out rectangle)) return false;
            Rectangle screen = Screen.FromRectangle(Rectangle.FromLTRB(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom)).Bounds;
            return rectangle.Left <= screen.Left && rectangle.Top <= screen.Top
                && rectangle.Right >= screen.Right && rectangle.Bottom >= screen.Bottom;
        }

        private static bool IsIgnoredProcess(IntPtr window)
        {
            uint processId;
            GetWindowThreadProcessId(window, out processId);
            try { return Array.IndexOf(IgnoredProcesses, Process.GetProcessById((int)processId).ProcessName) >= 0; }
            catch { return true; }
        }
    }
}
