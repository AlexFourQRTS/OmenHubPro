
namespace OmenHub.Core.Settings
{
    public enum FanMode { Auto = 0, Max = 1, Manual = 2, Curve = 3 }

    public enum TemperatureSource { System = 0, Gpu = 1, Hottest = 2 }

    /// <summary>The single source of truth for user configuration. Persistence lives in SettingsStorage.</summary>
    public static class AppSettings
    {
        public const int ProAutoWatts = 40;
        public const int BatteryWatts = 20;
        public const int MinimumCpuWatts = 20;
        public const int MaximumCpuWatts = 75;
        public const int CpuWattsStep = 5;
        public const int MaximumFanRpm = 6000;

        public static bool DarkTheme = true;
        public static FanMode Mode = FanMode.Auto;
        public static int ManualCpuFanRpm = 2400;
        public static int ManualGpuFanRpm = 2400;
        public static FanCurve CpuCurve = FanCurve.CreateDefault();
        public static FanCurve GpuCurve = FanCurve.CreateDefault();
        public static TemperatureSource CpuCurveSource = TemperatureSource.System;
        public static TemperatureSource GpuCurveSource = TemperatureSource.Gpu;
        public static int CpuWatts = 45;
        public static bool ProAutoEnabled = true;
        public static bool BatteryEcoEnabled = true;
        public static volatile bool LabEnabled = false;   // shows the lighting laboratory page in the menu

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
