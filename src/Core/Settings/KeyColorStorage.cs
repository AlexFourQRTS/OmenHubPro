using System.Text;

namespace OmenHub.Core.Settings
{
    /// <summary>Text form of AppSettings.KeyColors: "lamp:red:green:blue;lamp:red:green:blue;...".</summary>
    public static class KeyColorStorage
    {
        public static string Serialize()
        {
            var text = new StringBuilder();
            foreach (var entry in AppSettings.KeyColors)
            {
                if (text.Length > 0) text.Append(';');
                text.Append(entry.Key).Append(':').Append(entry.Value[0]).Append(':').Append(entry.Value[1]).Append(':').Append(entry.Value[2]);
            }
            return text.ToString();
        }

        public static void Deserialize(string text)
        {
            AppSettings.KeyColors.Clear();
            if (string.IsNullOrEmpty(text)) return;
            foreach (string entry in text.Split(';'))
            {
                string[] parts = entry.Split(':');
                int lamp, red, green, blue;
                bool valid = parts.Length == 4 && int.TryParse(parts[0], out lamp) && int.TryParse(parts[1], out red)
                    && int.TryParse(parts[2], out green) && int.TryParse(parts[3], out blue);
                if (!valid) continue;
                int.TryParse(parts[0], out lamp); int.TryParse(parts[1], out red);
                int.TryParse(parts[2], out green); int.TryParse(parts[3], out blue);
                AppSettings.KeyColors[lamp] = new[] { (byte)red, (byte)green, (byte)blue };
            }
        }
    }
}
