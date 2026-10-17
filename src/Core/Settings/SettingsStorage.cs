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
        private const int CurveFormatVersion = 3;

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
                text.AppendLine("CurveV=" + CurveFormatVersion);
                text.AppendLine("CpuCurve=" + AppSettings.CpuCurve.Serialize());
                text.AppendLine("GpuCurve=" + AppSettings.GpuCurve.Serialize());
                text.AppendLine("SrcCpu=" + (int)AppSettings.CpuCurveSource);
                text.AppendLine("SrcGpu=" + (int)AppSettings.GpuCurveSource);
                text.AppendLine("CpuW=" + AppSettings.CpuWatts);
                text.AppendLine("ProAuto=" + (AppSettings.ProAutoEnabled ? 1 : 0));
                text.AppendLine("BatteryEco=" + (AppSettings.BatteryEcoEnabled ? 1 : 0));
                text.AppendLine("LightCtl=" + (AppSettings.LightingControlEnabled ? 1 : 0));
                text.AppendLine("KeyboardLight=" + AppSettings.KeyboardLighting.Serialize());
                text.AppendLine("BarLight=" + AppSettings.BarLighting.Serialize());
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

                AppSettings.DarkTheme = ReadNumber(values, "Dark", 1) == 1;
                AppSettings.Mode = (FanMode)ReadNumber(values, "FanMode", 0);
                AppSettings.ManualCpuFanRpm = ReadNumber(values, "ManCpu", AppSettings.ManualCpuFanRpm);
                AppSettings.ManualGpuFanRpm = ReadNumber(values, "ManGpu", AppSettings.ManualGpuFanRpm);
                AppSettings.CpuCurveSource = (TemperatureSource)ReadNumber(values, "SrcCpu", (int)AppSettings.CpuCurveSource);
                AppSettings.GpuCurveSource = (TemperatureSource)ReadNumber(values, "SrcGpu", (int)AppSettings.GpuCurveSource);
                AppSettings.CpuWatts = ReadNumber(values, "CpuW", AppSettings.CpuWatts);
                AppSettings.ProAutoEnabled = ReadNumber(values, "ProAuto", 1) == 1;
                AppSettings.BatteryEcoEnabled = ReadNumber(values, "BatteryEco", 1) == 1;
                AppSettings.LightingControlEnabled = ReadNumber(values, "LightCtl", 0) == 1;
                string lightingText;
                values.TryGetValue("KeyboardLight", out lightingText);
                AppSettings.KeyboardLighting = ZoneLighting.Deserialize(lightingText, AppSettings.KeyboardLighting);
                values.TryGetValue("BarLight", out lightingText);
                AppSettings.BarLighting = ZoneLighting.Deserialize(lightingText, AppSettings.BarLighting);

                if (ReadNumber(values, "CurveV", 1) >= CurveFormatVersion)   // older formats had unsafe defaults
                {
                    string text;
                    FanCurve cpuCurve = values.TryGetValue("CpuCurve", out text) ? FanCurve.Deserialize(text) : null;
                    FanCurve gpuCurve = values.TryGetValue("GpuCurve", out text) ? FanCurve.Deserialize(text) : null;
                    if (cpuCurve != null) AppSettings.CpuCurve = cpuCurve;
                    if (gpuCurve != null) AppSettings.GpuCurve = gpuCurve;
                }
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
