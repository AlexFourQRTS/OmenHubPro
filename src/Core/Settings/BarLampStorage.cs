using System.Text;

namespace OmenHub.Core.Settings
{
    /// <summary>
    /// Which HID lamps physically belong to the bottom light bar (the rest are keys). The lamps 100-119 are mixed:
    /// the bar shares them with the bottom keyboard row. By default every lamp is treated as a key (so the whole keyboard
    /// lights up); the user may mark bar lamps on the keyboard page.
    /// </summary>
    public static class BarLampStorage
    {

        public static void SetDefaults()
        {
            AppSettings.BarLamps.Clear();
        }

        public static string Serialize()
        {
            var text = new StringBuilder();
            foreach (int lamp in AppSettings.BarLamps.Keys)
            {
                if (text.Length > 0) text.Append(',');
                text.Append(lamp);
            }
            return text.ToString();
        }

        public static void Deserialize(string text)
        {
            if (text == null) return;
            AppSettings.BarLamps.Clear();
            foreach (string part in text.Split(','))
            {
                int lamp;
                if (int.TryParse(part, out lamp)) AppSettings.BarLamps[lamp] = true;
            }
        }
    }
}
