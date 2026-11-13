
namespace OmenHub.Core.Settings
{
    public enum FanMode { Auto = 0, Max = 1, Manual = 2 }




    /// <summary>The single source of truth for user configuration. Persistence lives in SettingsStorage.</summary>
    public static class AppSettings
    {
        public const int ProAutoWatts = 40;
        public const int BatteryWatts = 20;
        public const int BatteryGpuWatts = 30;   // GPU limit while on battery (with the battery economy profile)
        public const int ProAutoGpuWatts = 110;  // GPU limit of the "Pro auto" profile on mains
        public const int MinimumCpuWatts = 20;
        public const int MaximumCpuWatts = 75;
        public const int CpuWattsStep = 5;
        public const int MaximumFanRpm = 6000;

        public static bool DarkTheme = true;
        public static FanMode Mode = FanMode.Auto;
        public static int ManualCpuFanRpm = 2400;
        public static int ManualGpuFanRpm = 2400;
        public static int CpuWatts = 45;
        public static volatile bool CpuLimitEnabled = false;   // false = no limit (the maximum of the range is applied)
        public static bool ProAutoEnabled = true;
        public static volatile bool LabEnabled = false;   // shows the lighting laboratory page in the menu

        public const int MinimumGpuWatts = 30;
        public const int MaximumGpuWatts = 130;   // the card supports 140 W, more than 130 makes no sense
        public const int GpuWattsStep = 5;
        public static volatile bool GpuLimitEnabled = false;
        public static volatile int GpuWatts = 80;


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

        /// <summary>Leaves the Pro auto profile but keeps its values as manual limits, so one value can be changed alone.</summary>
        public static void LeaveProAuto()
        {
            if (!ProAutoEnabled) return;
            ProAutoEnabled = false;
            CpuWatts = ProAutoWatts; CpuLimitEnabled = true;
            GpuWatts = ProAutoGpuWatts; GpuLimitEnabled = true;
        }

        /// <summary>Returns true once after each change so a timer can persist it.</summary>
        public static bool TakeUnsavedFlag()
        {
            bool wasChanged = hasUnsavedChanges;
            hasUnsavedChanges = false;
            return wasChanged;
        }
    }
}
