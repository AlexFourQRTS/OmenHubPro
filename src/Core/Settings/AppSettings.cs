using System.Collections.Concurrent;

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

        /// <summary>Per-key colour overrides on top of the keyboard zone colour: lamp index to {red, green, blue}.</summary>
        public static readonly ConcurrentDictionary<int, byte[]> KeyColors = new ConcurrentDictionary<int, byte[]>();

        /// <summary>Lamps that physically belong to the bottom light bar (see BarLampStorage).</summary>
        public static readonly ConcurrentDictionary<int, bool> BarLamps = new ConcurrentDictionary<int, bool>();

        /// <summary>Lamp that blinks white so the user can see which LED it is (-1 = none).</summary>
        public static volatile int IdentifiedLamp = -1;
        public static ZoneLighting KeyboardLighting = ZoneLighting.Create(255, 138, 31, LightingEffect.Static);

        public const int BarZoneCount = 4;

        /// <summary>The four zones of the bottom light bar, left to right: {red, green, blue} each.</summary>
        public static byte[][] BarZoneColors =
        {
            new byte[] { 155, 107, 255 }, new byte[] { 155, 107, 255 }, new byte[] { 255, 138, 31 }, new byte[] { 255, 138, 31 }
        };

        public static int BarBrightnessPercent = 100;

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
