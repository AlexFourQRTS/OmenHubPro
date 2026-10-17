using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OmenHub.Core.Logging;
using OmenHub.Core.Settings;

namespace OmenHub.Core.Presets
{
    /// <summary>
    /// Lighting presets (keyboard and bottom bar together), one text file per preset in the "presets" folder next to the
    /// executable. The file name is the preset name. Lines: Keyboard=..., BarZones=..., BarBright=..., BarOn=...
    /// Older files with a single keyboard line are still readable and leave the bar unchanged.
    /// </summary>
    public static class LightingPresetStore
    {
        private const string FileExtension = ".preset";

        private static string Folder
        {
            get
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "presets");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                return folder;
            }
        }

        public static List<string> ListNames()
        {
            var names = new List<string>();
            try
            {
                foreach (string path in Directory.GetFiles(Folder, "*" + FileExtension))
                    names.Add(Path.GetFileNameWithoutExtension(path));
                names.Sort();
            }
            catch (Exception error) { AppLog.Write("presets list failed: " + error.Message); }
            return names;
        }

        /// <summary>Saves the current keyboard and bar lighting under the given name.</summary>
        public static void SaveCurrent(string name)
        {
            string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars())).Trim();
            if (safeName.Length == 0) return;
            var text = new StringBuilder();
            text.AppendLine("Keyboard=" + AppSettings.KeyboardLighting.Serialize());
            text.AppendLine("BarZones=" + BarZoneStorage.Serialize());
            text.AppendLine("BarBright=" + AppSettings.BarBrightnessPercent);
            text.AppendLine("BarOn=" + (AppSettings.BarEnabled ? 1 : 0));
            File.WriteAllText(Path.Combine(Folder, safeName + FileExtension), text.ToString());
            AppLog.Write("preset saved: " + safeName);
        }

        /// <summary>Applies a saved preset to the live settings; returns false when it cannot be read.</summary>
        public static bool Apply(string name)
        {
            string path = Path.Combine(Folder, name + FileExtension);
            if (!File.Exists(path)) return false;
            foreach (string line in File.ReadAllLines(path))
            {
                int separator = line.IndexOf('=');
                string key = separator > 0 ? line.Substring(0, separator) : "";
                string value = separator > 0 ? line.Substring(separator + 1).Trim() : line.Trim();
                ApplyLine(key, value);
            }
            return true;
        }

        private static void ApplyLine(string key, string value)
        {
            int number;
            if (key == "Keyboard" || key == "")   // a bare line is the old keyboard-only format
                AppSettings.KeyboardLighting = ZoneLighting.Deserialize(value, AppSettings.KeyboardLighting);
            else if (key == "BarZones") BarZoneStorage.Deserialize(value);
            else if (key == "BarBright" && int.TryParse(value, out number)) AppSettings.BarBrightnessPercent = Math.Max(0, Math.Min(100, number));
            else if (key == "BarOn" && int.TryParse(value, out number)) AppSettings.BarEnabled = number == 1;
        }

        public static void Delete(string name)
        {
            string path = Path.Combine(Folder, name + FileExtension);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
