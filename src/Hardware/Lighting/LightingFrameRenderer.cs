using System;
using System.Collections.Generic;
using OmenHub.Core.Settings;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>
    /// Builds one frame (an {red, green, blue} colour per lamp). Key lamps follow the keyboard zone and the per-key overrides;
    /// bar lamps follow the four bar zones, left to right by their physical position.
    /// </summary>
    public static class LightingFrameRenderer
    {
        private const double IdentifyBlinkHertz = 2;

        public static byte[][] Render(int lampCount, double seconds)
        {
            var frame = new byte[lampCount][];
            ZoneLighting keyboardZone = AppSettings.KeyboardLighting;
            byte[][] barColors = ScaledBarColors();
            Dictionary<int, int> barZoneOfLamp = AssignBarZones();

            for (int lamp = 0; lamp < lampCount; lamp++)
            {
                int barZone;
                if (barZoneOfLamp.TryGetValue(lamp, out barZone)) frame[lamp] = barColors[barZone];
                else frame[lamp] = KeyColor(keyboardZone, lamp, lampCount, seconds);
            }
            int identified = AppSettings.IdentifiedLamp;
            if (identified >= 0 && identified < lampCount)
                frame[identified] = (int)(seconds * IdentifyBlinkHertz * 2) % 2 == 0 ? new byte[] { 255, 255, 255 } : new byte[] { 0, 0, 0 };
            return frame;
        }

        public static byte[][] ScaledBarColors()
        {
            double factor = AppSettings.BarBrightnessPercent / 100.0;
            var colors = new byte[AppSettings.BarZoneCount][];
            for (int zone = 0; zone < colors.Length; zone++)
                colors[zone] = LightingEffects.Dim(AppSettings.BarZoneColors[zone], factor);
            return colors;
        }

        private static byte[] KeyColor(ZoneLighting keyboardZone, int lamp, int lampCount, double seconds)
        {
            byte[] overrideColor;
            if (keyboardZone.Effect != LightingEffect.Off && AppSettings.KeyColors.TryGetValue(lamp, out overrideColor))
                return LightingEffects.Dim(overrideColor, keyboardZone.BrightnessPercent / 100.0);
            return LightingEffects.ColorAt(keyboardZone, lamp, lampCount, seconds);
        }

        /// <summary>Splits the bar lamps into four equal groups by horizontal position; returns lamp index to zone index.</summary>
        private static Dictionary<int, int> AssignBarZones()
        {
            float[] xMillimetres, yMillimetres;
            LampLayout.Get(out xMillimetres, out yMillimetres);
            var barLamps = new List<int>(AppSettings.BarLamps.Keys);
            barLamps.Sort(delegate (int first, int second)
            {
                float firstX = first < xMillimetres.Length ? xMillimetres[first] : first;
                float secondX = second < xMillimetres.Length ? xMillimetres[second] : second;
                return firstX.CompareTo(secondX);
            });
            var zoneOfLamp = new Dictionary<int, int>();
            for (int rank = 0; rank < barLamps.Count; rank++)
                zoneOfLamp[barLamps[rank]] = rank * AppSettings.BarZoneCount / Math.Max(1, barLamps.Count);
            return zoneOfLamp;
        }
    }
}
