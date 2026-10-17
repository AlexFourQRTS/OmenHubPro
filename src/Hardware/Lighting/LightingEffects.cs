using System;
using OmenHub.Core.Settings;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>
    /// Colour of one keyboard lamp for the chosen effect. x01 / y01 are the lamp's position on the keyboard,
    /// 0..1 from left to right and from top to bottom; seconds is the animation clock.
    /// </summary>
    public static class LightingEffects
    {
        private const double RainbowDegreesPerSecond = 40;

        public static byte[] ColorAt(ZoneLighting zone, int lamp, double x01, double y01, double seconds)
        {
            double time = seconds * (0.25 + zone.Speed * 0.25);   // speed 1..10 stretches the clock
            double level = 1;
            byte[] color = zone.Primary;
            switch (zone.Effect)
            {
                case LightingEffect.Off: return new byte[] { 0, 0, 0 };
                case LightingEffect.Gradient: color = Mix(zone.Primary, zone.Secondary, x01); break;
                case LightingEffect.Breathing: level = 0.5 + 0.5 * Math.Sin(2 * Math.PI * time / 3); break;
                case LightingEffect.Rainbow: return FromHue(x01 * 360 + time * RainbowDegreesPerSecond, Brightness(zone));
                case LightingEffect.RainbowCycle: return FromHue(time * RainbowDegreesPerSecond, Brightness(zone));
                case LightingEffect.ColorWave:
                    color = Mix(zone.Primary, zone.Secondary, 0.5 + 0.5 * Math.Sin(2 * Math.PI * (x01 - time * 0.2)));
                    break;
                case LightingEffect.Pulse: color = (int)(time * 2) % 2 == 0 ? zone.Primary : zone.Secondary; break;
                case LightingEffect.Twinkle:
                    color = Mix(zone.Secondary, zone.Primary, Math.Pow(Math.Max(0, Math.Sin(time * 2 + Hash(lamp) * 6.2832)), 6));
                    break;
                case LightingEffect.Scanner:
                    double position = 0.5 + 0.5 * Math.Sin(time * 1.6);
                    color = Mix(zone.Secondary, zone.Primary, Math.Max(0, 1 - Math.Abs(x01 - position) * 5));
                    break;
                case LightingEffect.Fire: color = FireColor(y01, lamp, time); break;
            }
            return Scale(color, level * Brightness(zone));
        }

        /// <summary>Scales an {red, green, blue} colour by a brightness factor (0..1).</summary>
        public static byte[] Dim(byte[] color, double factor)
        {
            return Scale(color, factor);
        }

        public static byte[] FromHue(double hueDegrees, double brightness)
        {
            double sector = (hueDegrees % 360 + 360) % 360 / 60.0;
            int sectorIndex = (int)sector;
            double rising = sector - sectorIndex, falling = 1 - rising;
            double red = 0, green = 0, blue = 0;
            switch (sectorIndex)
            {
                case 0: red = 1; green = rising; break;
                case 1: red = falling; green = 1; break;
                case 2: green = 1; blue = rising; break;
                case 3: green = falling; blue = 1; break;
                case 4: red = rising; blue = 1; break;
                default: red = 1; blue = falling; break;
            }
            return Scale(new[] { (byte)(red * 255), (byte)(green * 255), (byte)(blue * 255) }, brightness);
        }

        private static byte[] FireColor(double y01, int lamp, double time)
        {
            double heat = Math.Max(0, Math.Min(1, y01 * 0.9 + 0.35 * Math.Sin(time * 5 + Hash(lamp) * 12) * Math.Sin(time * 2.3 + lamp)));
            return new[] { (byte)255, (byte)(60 + 195 * heat * heat), (byte)(heat > 0.8 ? 120 * (heat - 0.8) * 5 : 0) };
        }

        private static double Brightness(ZoneLighting zone)
        {
            return zone.BrightnessPercent / 100.0;
        }

        private static double Hash(int lamp)
        {
            double value = Math.Sin(lamp * 12.9898) * 43758.5453;
            return value - Math.Floor(value);
        }

        private static byte[] Mix(byte[] first, byte[] second, double fraction)
        {
            fraction = Math.Max(0, Math.Min(1, fraction));
            return new[]
            {
                (byte)(first[0] + (second[0] - first[0]) * fraction),
                (byte)(first[1] + (second[1] - first[1]) * fraction),
                (byte)(first[2] + (second[2] - first[2]) * fraction)
            };
        }

        private static byte[] Scale(byte[] color, double factor)
        {
            return new[] { (byte)(color[0] * factor), (byte)(color[1] * factor), (byte)(color[2] * factor) };
        }
    }
}
