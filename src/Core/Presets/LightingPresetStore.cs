using System;
using System.Collections.Generic;
using System.IO;
using OmenHub.Core.Logging;
using OmenHub.Core.Settings;

namespace OmenHub.Core.Presets
{
    /// <summary>
    /// Keyboard lighting presets, one text file per preset in the "presets" folder next to the executable:
    /// the file name is the preset name, the content is ZoneLighting.Serialize().
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

        public static void Save(string name, ZoneLighting lighting)
        {
            string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars())).Trim();
            if (safeName.Length == 0) return;
            File.WriteAllText(Path.Combine(Folder, safeName + FileExtension), lighting.Serialize());
            AppLog.Write("preset saved: " + safeName + " = " + lighting.Serialize());
        }

        public static ZoneLighting Load(string name)
        {
            string path = Path.Combine(Folder, name + FileExtension);
            return File.Exists(path) ? ZoneLighting.Deserialize(File.ReadAllText(path).Trim(), null) : null;
        }

        public static void Delete(string name)
        {
            string path = Path.Combine(Folder, name + FileExtension);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
