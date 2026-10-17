using System;

namespace OmenHub.Core.Settings
{
    public enum LightingEffect { Off = 0, Static = 1, Rainbow = 2, Breathing = 3 }

    /// <summary>How the light bar is driven: BIOS table (4 zones, survives app exit) or HID lamps 103-119 (17 lamps, effects).</summary>
    public enum BarBackend { Bios = 0, Lamps = 1 }

    /// <summary>Lighting configuration of one zone (the keyboard or the light bar).</summary>
    public sealed class ZoneLighting
    {
        public int Red = 255;
        public int Green = 255;
        public int Blue = 255;
        public int BrightnessPercent = 100;
        public LightingEffect Effect = LightingEffect.Static;

        public static ZoneLighting Create(int red, int green, int blue, LightingEffect effect)
        {
            return new ZoneLighting { Red = red, Green = green, Blue = blue, Effect = effect };
        }

        public string Serialize()
        {
            return Red + "," + Green + "," + Blue + "," + BrightnessPercent + "," + (int)Effect;
        }

        public static ZoneLighting Deserialize(string text, ZoneLighting fallback)
        {
            if (text == null) return fallback;
            string[] parts = text.Split(',');
            if (parts.Length != 5) return fallback;
            int[] numbers = new int[5];
            for (int index = 0; index < 5; index++)
                if (!int.TryParse(parts[index], out numbers[index])) return fallback;
            return new ZoneLighting
            {
                Red = Clamp(numbers[0], 255), Green = Clamp(numbers[1], 255), Blue = Clamp(numbers[2], 255),
                BrightnessPercent = Clamp(numbers[3], 100), Effect = (LightingEffect)Clamp(numbers[4], 3)
            };
        }

        private static int Clamp(int value, int maximum)
        {
            return Math.Max(0, Math.Min(maximum, value));
        }
    }
}
