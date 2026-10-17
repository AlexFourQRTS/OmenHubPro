using System;

namespace OmenHub.Core.Settings
{
    public enum LightingEffect
    {
        Off = 0, Static = 1, Gradient = 2, Breathing = 3, Rainbow = 4, RainbowCycle = 5,
        ColorWave = 6, Pulse = 7, Twinkle = 8, Scanner = 9, Fire = 10
    }

    /// <summary>Lighting configuration of the keyboard: effect, two colours, brightness and speed.</summary>
    public sealed class ZoneLighting
    {
        public const int MaximumSpeed = 10;
        private const int FieldCount = 9;

        public byte[] Primary = { 255, 255, 255 };
        public byte[] Secondary = { 0, 80, 255 };
        public int BrightnessPercent = 100;
        public int Speed = 5;
        public LightingEffect Effect = LightingEffect.Static;

        public string Serialize()
        {
            return Primary[0] + "," + Primary[1] + "," + Primary[2] + "," + Secondary[0] + "," + Secondary[1] + "," + Secondary[2]
                + "," + BrightnessPercent + "," + (int)Effect + "," + Speed;
        }

        public static ZoneLighting Deserialize(string text, ZoneLighting fallback)
        {
            if (text == null) return fallback;
            string[] parts = text.Split(',');
            if (parts.Length != FieldCount) return fallback;
            int[] numbers = new int[FieldCount];
            for (int index = 0; index < FieldCount; index++)
                if (!int.TryParse(parts[index], out numbers[index])) return fallback;
            return new ZoneLighting
            {
                Primary = new[] { ToByte(numbers[0]), ToByte(numbers[1]), ToByte(numbers[2]) },
                Secondary = new[] { ToByte(numbers[3]), ToByte(numbers[4]), ToByte(numbers[5]) },
                BrightnessPercent = Math.Max(0, Math.Min(100, numbers[6])),
                Effect = (LightingEffect)Math.Max(0, Math.Min((int)LightingEffect.Fire, numbers[7])),
                Speed = Math.Max(1, Math.Min(MaximumSpeed, numbers[8]))
            };
        }

        private static byte ToByte(int value)
        {
            return (byte)Math.Max(0, Math.Min(255, value));
        }
    }
}
