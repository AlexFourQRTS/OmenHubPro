using System.Drawing;

namespace OmenHub.Ui.Theme
{
    /// <summary>Colours of the two themes, both in Google (Material) style: light and dark.</summary>
    public static class Palette
    {
        public static bool IsDark;
        public static Color Background, Sidebar, CardSurface, Border, Text, SubText;
        public static Color Accent, Good, Bad, Track, SelectedFill, OnAccent;

        public static void Apply(bool dark)
        {
            IsDark = dark;
            if (dark)
            {
                Background = Rgb(0x202124); Sidebar = Rgb(0x202124); CardSurface = Rgb(0x292A2D); Border = Rgb(0x3C4043);
                Text = Rgb(0xE8EAED); SubText = Rgb(0x9AA0A6); Accent = Rgb(0x8AB4F8); SelectedFill = Rgb(0x394457);
                Good = Rgb(0x81C995); Bad = Rgb(0xF28B82); Track = Rgb(0x3C4043); OnAccent = Rgb(0x202124);
            }
            else
            {
                Background = Rgb(0xF8F9FA); Sidebar = Rgb(0xF8F9FA); CardSurface = Rgb(0xFFFFFF); Border = Rgb(0xDADCE0);
                Text = Rgb(0x202124); SubText = Rgb(0x5F6368); Accent = Rgb(0x1A73E8); SelectedFill = Rgb(0xE8F0FE);
                Good = Rgb(0x1E8E3E); Bad = Rgb(0xD93025); Track = Rgb(0xE8EAED); OnAccent = Rgb(0xFFFFFF);
            }
        }

        /// <summary>Material style is flat: the "gradient" end is the accent itself.</summary>
        public static Color GradientEnd { get { return Accent; } }

        private static Color Rgb(int value)
        {
            return Color.FromArgb((value >> 16) & 255, (value >> 8) & 255, value & 255);
        }
    }
}
