using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OmenHub.Core.Logging;

namespace OmenHub.Core.Settings
{
    /// <summary>Reads and writes %APPDATA%\OmenHub\settings.ini (key=value lines).</summary>
    public static class SettingsStorage
    {

        private static string FilePath
        {
            get
            {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OmenHub");
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                return Path.Combine(directory, "settings.ini");
            }
        }

        public static void Save()
        {
            try
            {
                var text = new StringBuilder();
                text.AppendLine("Dark=" + (AppSettings.DarkTheme ? 1 : 0));
                text.AppendLine("FanMode=" + (int)AppSettings.Mode);
                text.AppendLine("ManCpu=" + AppSettings.ManualCpuFanRpm);
                text.AppendLine("ManGpu=" + AppSettings.ManualGpuFanRpm);
                text.AppendLine("CpuW=" + AppSettings.CpuWatts);
                text.AppendLine("BarZones=" + BarZoneStorage.Serialize());
                text.AppendLine("BarBright=" + AppSettings.BarBrightnessPercent);
                text.AppendLine("BarOn=" + (AppSettings.BarEnabled ? 1 : 0));
                text.AppendLine("GpuW=" + AppSettings.GpuWatts);
                text.AppendLine("KeyboardLight=" + AppSettings.KeyboardLighting.Serialize());
                File.WriteAllText(FilePath, text.ToString());
            }
            catch (Exception error) { AppLog.Write("settings save failed: " + error.Message); }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                Dictionary<string, string> values = ReadKeyValues(File.ReadAllLines(FilePath));

                AppSettings.DarkTheme = ReadNumber(values, "Dark", 0) == 1;
                AppSettings.Mode = (FanMode)Math.Max(0, Math.Min(2, ReadNumber(values, "FanMode", 0)));
                AppSettings.ManualCpuFanRpm = ReadNumber(values, "ManCpu", AppSettings.ManualCpuFanRpm);
                AppSettings.ManualGpuFanRpm = ReadNumber(values, "ManGpu", AppSettings.ManualGpuFanRpm);
                AppSettings.CpuWatts = AppSettings.Snap(AppSettings.CpuWattChoices, ReadNumber(values, "CpuW", AppSettings.CpuWatts));
                AppSettings.BarEnabled = ReadNumber(values, "BarOn", 1) == 1;
                AppSettings.GpuWatts = AppSettings.Snap(AppSettings.GpuWattChoices, ReadNumber(values, "GpuW", 100));
                string barZoneText;
                values.TryGetValue("BarZones", out barZoneText);
                BarZoneStorage.Deserialize(barZoneText);
                AppSettings.BarBrightnessPercent = Math.Max(0, Math.Min(100, ReadNumber(values, "BarBright", 100)));
                string lightingText;
                values.TryGetValue("KeyboardLight", out lightingText);
                AppSettings.KeyboardLighting = ZoneLighting.Deserialize(lightingText, AppSettings.KeyboardLighting);
            }
            catch (Exception error) { AppLog.Write("settings load failed: " + error.Message); }
        }


        private static Dictionary<string, string> ReadKeyValues(string[] lines)
        {
            var values = new Dictionary<string, string>();
            foreach (string line in lines)
            {
                int separator = line.IndexOf('=');
                if (separator > 0) values[line.Substring(0, separator)] = line.Substring(separator + 1);
            }
            return values;
        }

        private static int ReadNumber(Dictionary<string, string> values, string key, int fallback)
        {
            string text;
            int number;
            return values.TryGetValue(key, out text) && int.TryParse(text, out number) ? number : fallback;
        }
    }
}
