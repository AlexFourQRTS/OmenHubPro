using System.Text;

namespace OmenHub.Core.Settings
{
    /// <summary>Text form of AppSettings.BarZoneColors: "r,g,b;r,g,b;r,g,b;r,g,b".</summary>
    public static class BarZoneStorage
    {
        public static string Serialize()
        {
            var text = new StringBuilder();
            for (int zone = 0; zone < AppSettings.BarZoneCount; zone++)
            {
                if (zone > 0) text.Append(';');
                byte[] color = AppSettings.BarZoneColors[zone];
                text.Append(color[0]).Append(',').Append(color[1]).Append(',').Append(color[2]);
            }
            return text.ToString();
        }

        public static void Deserialize(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            string[] zones = text.Split(';');
            if (zones.Length != AppSettings.BarZoneCount) return;
            var parsed = new byte[AppSettings.BarZoneCount][];
            for (int zone = 0; zone < zones.Length; zone++)
            {
                string[] channels = zones[zone].Split(',');
                byte red, green, blue;
                if (channels.Length != 3 || !byte.TryParse(channels[0], out red)
                    || !byte.TryParse(channels[1], out green) || !byte.TryParse(channels[2], out blue)) return;
                parsed[zone] = new[] { red, green, blue };
            }
            AppSettings.BarZoneColors = parsed;
        }
    }
}
