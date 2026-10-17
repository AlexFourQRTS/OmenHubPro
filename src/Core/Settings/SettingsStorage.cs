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
                text.AppendLine("CpuLimit=" + (AppSettings.CpuLimitEnabled ? 1 : 0));
                text.AppendLine("ProAuto=" + (AppSettings.ProAutoEnabled ? 1 : 0));
                text.AppendLine("BarZones=" + BarZoneStorage.Serialize());
                text.AppendLine("BarBright=" + AppSettings.BarBrightnessPercent);
                text.AppendLine("BarOn=" + (AppSettings.BarEnabled ? 1 : 0));
                text.AppendLine("Lab=" + (AppSettings.LabEnabled ? 1 : 0));
                text.AppendLine("GpuLimit=" + (AppSettings.GpuLimitEnabled ? 1 : 0));
                text.AppendLine("GpuW=" + AppSettings.GpuWatts);
                text.AppendLine("OverlayOn=" + (AppSettings.OverlayEnabled ? 1 : 0));
                text.AppendLine("Overlay=" + (int)AppSettings.Overlay);
                text.AppendLine("OverlayFill=" + AppSettings.OverlayFillOpacityPercent);
                text.AppendLine("OverlayText=" + AppSettings.OverlayTextOpacityPercent);
                text.AppendLine("OverlayColor=" + AppSettings.OverlayColor[0] + "," + AppSettings.OverlayColor[1] + "," + AppSettings.OverlayColor[2]);
                text.AppendLine("OverlayRows=" + (AppSettings.OverlayShowCpu ? 1 : 0) + (AppSettings.OverlayShowGpu ? 1 : 0) + (AppSettings.OverlayShowRam ? 1 : 0) + (AppSettings.OverlayShowFps ? 1 : 0)
                    + (AppSettings.OverlayShowTemperature ? 1 : 0) + (AppSettings.OverlayShowPower ? 1 : 0) + (AppSettings.OverlayShowFan ? 1 : 0));
                text.AppendLine("OverlayPos=" + (int)AppSettings.OverlayPosition);
                text.AppendLine("OverlayScale=" + AppSettings.OverlayScalePercent);
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

                AppSettings.DarkTheme = ReadNumber(values, "Dark", 1) == 1;
                AppSettings.Mode = (FanMode)ReadNumber(values, "FanMode", 0);
                AppSettings.ManualCpuFanRpm = ReadNumber(values, "ManCpu", AppSettings.ManualCpuFanRpm);
                AppSettings.ManualGpuFanRpm = ReadNumber(values, "ManGpu", AppSettings.ManualGpuFanRpm);
                AppSettings.CpuCurveSource = (TemperatureSource)ReadNumber(values, "SrcCpu", (int)AppSettings.CpuCurveSource);
                AppSettings.GpuCurveSource = (TemperatureSource)ReadNumber(values, "SrcGpu", (int)AppSettings.GpuCurveSource);
                AppSettings.CpuWatts = ReadNumber(values, "CpuW", AppSettings.CpuWatts);
                AppSettings.CpuLimitEnabled = ReadNumber(values, "CpuLimit", 0) == 1;
                AppSettings.ProAutoEnabled = ReadNumber(values, "ProAuto", 1) == 1;
                AppSettings.BarEnabled = ReadNumber(values, "BarOn", 1) == 1;
                AppSettings.LabEnabled = ReadNumber(values, "Lab", 0) == 1;
                AppSettings.GpuLimitEnabled = ReadNumber(values, "GpuLimit", 0) == 1;
                AppSettings.OverlayEnabled = ReadNumber(values, "OverlayOn", 1) == 1;
                AppSettings.Overlay = (OverlayMode)Math.Max(1, Math.Min(2, ReadNumber(values, "Overlay", 1)));
                AppSettings.OverlayFillOpacityPercent = Math.Max(0, Math.Min(100, ReadNumber(values, "OverlayFill", 85)));
                AppSettings.OverlayTextOpacityPercent = Math.Max(20, Math.Min(100, ReadNumber(values, "OverlayText", 100)));
                LoadOverlayAppearance(values);
                AppSettings.OverlayPosition = (OverlayCorner)Math.Max(0, Math.Min(3, ReadNumber(values, "OverlayPos", 0)));
                AppSettings.OverlayScalePercent = Math.Max(60, Math.Min(200, ReadNumber(values, "OverlayScale", 100)));
                AppSettings.GpuWatts = Math.Max(AppSettings.MinimumGpuWatts, Math.Min(AppSettings.MaximumGpuWatts, ReadNumber(values, "GpuW", 80)));
                string barZoneText;
                values.TryGetValue("BarZones", out barZoneText);
                BarZoneStorage.Deserialize(barZoneText);
                AppSettings.BarBrightnessPercent = Math.Max(0, Math.Min(100, ReadNumber(values, "BarBright", 100)));
                string lightingText;
                values.TryGetValue("KeyboardLight", out lightingText);
                AppSettings.KeyboardLighting = ZoneLighting.Deserialize(lightingText, AppSettings.KeyboardLighting);

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

        private static void LoadOverlayAppearance(Dictionary<string, string> values)
        {
            string colorText, rowsText;
            if (values.TryGetValue("OverlayColor", out colorText))
            {
                string[] channels = colorText.Split(',');
                byte red, green, blue;
                if (channels.Length == 3 && byte.TryParse(channels[0], out red) && byte.TryParse(channels[1], out green) && byte.TryParse(channels[2], out blue))
                    AppSettings.OverlayColor = new[] { red, green, blue };
            }
            if (values.TryGetValue("OverlayRows", out rowsText) && (rowsText.Length == 6 || rowsText.Length == 7))
            {
                AppSettings.OverlayShowCpu = rowsText[0] == '1';
                AppSettings.OverlayShowGpu = rowsText[1] == '1';
                AppSettings.OverlayShowRam = rowsText[2] == '1';
                int shift = rowsText.Length == 7 ? 1 : 0;   // the 7-character form has the FPS flag after memory
                if (shift == 1) AppSettings.OverlayShowFps = rowsText[3] == '1';
                AppSettings.OverlayShowTemperature = rowsText[3 + shift] == '1';
                AppSettings.OverlayShowPower = rowsText[4 + shift] == '1';
                AppSettings.OverlayShowFan = rowsText[5 + shift] == '1';
            }
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
