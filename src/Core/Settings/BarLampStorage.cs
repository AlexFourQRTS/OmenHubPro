using System.Text;

namespace OmenHub.Core.Settings
{
    /// <summary>
    /// Which HID lamps physically belong to the bottom light bar (the rest are keys). The lamps 100-119 are mixed:
    /// the bar shares them with the bottom keyboard row, so the user identifies them once on the keyboard page.
    /// </summary>
    public static class BarLampStorage
    {
        private const int DefaultFirstBarLamp = 103;
        private const int DefaultLastBarLamp = 119;

        public static void SetDefaults()
        {
            AppSettings.BarLamps.Clear();
            for (int lamp = DefaultFirstBarLamp; lamp <= DefaultLastBarLamp; lamp++) AppSettings.BarLamps[lamp] = true;
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
