using System;
using OmenHub.Core.Settings;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>Computes the colour of one lamp for a zone's effect at a given moment.</summary>
    public static class LightingEffects
    {
        private const double RainbowDegreesPerSecond = 60;
        private const double BreathingPeriodSeconds = 3;

        /// <summary>Returns {red, green, blue}. lampPosition is the lamp's index inside its zone (0..zoneSize-1).</summary>
        public static byte[] ColorAt(ZoneLighting zone, int lampPosition, int zoneSize, double seconds)
        {
            double brightness = zone.BrightnessPercent / 100.0;
            switch (zone.Effect)
            {
                case LightingEffect.Off:
                    return new byte[] { 0, 0, 0 };
                case LightingEffect.Rainbow:
                    double hue = (360.0 * lampPosition / Math.Max(1, zoneSize) + seconds * RainbowDegreesPerSecond) % 360;
                    return FromHue(hue, brightness);
                case LightingEffect.Breathing:
                    double wave = 0.5 + 0.5 * Math.Sin(2 * Math.PI * seconds / BreathingPeriodSeconds);
                    return Scale(zone.Red, zone.Green, zone.Blue, brightness * wave);
                default:
                    return Scale(zone.Red, zone.Green, zone.Blue, brightness);
            }
        }

        private static byte[] Scale(int red, int green, int blue, double factor)
        {
            return new[] { (byte)(red * factor), (byte)(green * factor), (byte)(blue * factor) };
        }

        public static byte[] FromHue(double hueDegrees, double brightness)
        {
            double sector = hueDegrees / 60.0;
            int sectorIndex = (int)sector % 6;
            double rising = sector - Math.Floor(sector);
            double falling = 1 - rising;
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
            return Scale((int)(red * 255), (int)(green * 255), (int)(blue * 255), brightness);
        }
    }
}
