using System.Drawing;

namespace OmenHub.Ui.Theme
{
    /// <summary>Colours of the two themes: dark (purple / orange / black) and light (Google-style).</summary>
    public static class Palette
    {
        public static bool IsDark = true;
        public static Color Background, Sidebar, CardSurface, Border, Text, SubText;
        public static Color Accent, AccentSecondary, Good, Bad, Track;

        public static void Apply(bool dark)
        {
            IsDark = dark;
            if (dark)
            {
                Background = Rgb(0x0C0A12); Sidebar = Rgb(0x110E1A); CardSurface = Rgb(0x181322); Border = Rgb(0x2B2340);
                Text = Rgb(0xF3EFFA); SubText = Rgb(0x9B91B0); Accent = Rgb(0x9B6BFF); AccentSecondary = Rgb(0xFF8A1F);
                Good = Rgb(0x3DDC97); Bad = Rgb(0xFF4D5E); Track = Rgb(0x2A2238);
            }
            else
            {
                Background = Rgb(0xF8F9FA); Sidebar = Rgb(0xFFFFFF); CardSurface = Rgb(0xFFFFFF); Border = Rgb(0xDADCE0);
                Text = Rgb(0x202124); SubText = Rgb(0x5F6368); Accent = Rgb(0x1A73E8); AccentSecondary = Rgb(0xF29900);
                Good = Rgb(0x1E8E3E); Bad = Rgb(0xD93025); Track = Rgb(0xE8EAED);
            }
        }

        /// <summary>The far end of a gradient: orange in the dark theme, a flat accent in the light theme.</summary>
        public static Color GradientEnd { get { return IsDark ? AccentSecondary : Accent; } }

        private static Color Rgb(int value)
        {
            return Color.FromArgb((value >> 16) & 255, (value >> 8) & 255, value & 255);
        }
    }
}
