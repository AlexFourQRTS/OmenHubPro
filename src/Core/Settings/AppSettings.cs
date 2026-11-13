
namespace OmenHub.Core.Settings
{
    public enum FanMode { Auto = 0, Max = 1, Manual = 2 }




    /// <summary>The single source of truth for user configuration. Persistence lives in SettingsStorage.</summary>
    public static class AppSettings
    {
        public const int MaximumFanRpm = 6000;

        public static bool DarkTheme = true;
        public static FanMode Mode = FanMode.Auto;
        public static int ManualCpuFanRpm = 2400;
        public static int ManualGpuFanRpm = 2400;
        public static readonly int[] CpuWattChoices = { 20, 40, 50, 60 };
        public static readonly int[] GpuWattChoices = { 50, 80, 100, 130 };
        public static int CpuWatts = 40;
        public static volatile bool LabEnabled = false;   // shows the lighting laboratory page in the menu

        public static volatile int GpuWatts = 100;

        /// <summary>The allowed value closest to the given one (old settings may hold anything).</summary>
        public static int Snap(int[] choices, int watts)
        {
            int best = choices[0];
            foreach (int choice in choices) if (System.Math.Abs(choice - watts) < System.Math.Abs(best - watts)) best = choice;
            return best;
        }


        public static ZoneLighting KeyboardLighting = new ZoneLighting();   // plain white by default

        public const int BarZoneCount = 4;

        /// <summary>The four zones of the bottom light bar, left to right: {red, green, blue} each.</summary>
        public static byte[][] BarZoneColors =
        {
            new byte[] { 155, 107, 255 }, new byte[] { 155, 107, 255 }, new byte[] { 255, 138, 31 }, new byte[] { 255, 138, 31 }
        };

        public static int BarBrightnessPercent = 100;
        public static volatile bool BarEnabled = true;   // false = bar switched off (black) through the BIOS table

        private static volatile bool hasUnsavedChanges;

        public static void MarkChanged() { hasUnsavedChanges = true; }


        /// <summary>Returns true once after each change so a timer can persist it.</summary>
        public static bool TakeUnsavedFlag()
        {
            bool wasChanged = hasUnsavedChanges;
            hasUnsavedChanges = false;
            return wasChanged;
        }
    }
}
