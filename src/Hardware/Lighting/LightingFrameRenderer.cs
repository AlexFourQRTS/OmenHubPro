using System;
using OmenHub.Core.Settings;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>
    /// Builds one frame (an {red, green, blue} colour per lamp). The lamps 0-102 get the animated effect. The lamps
    /// 103-119 are shared between the bottom keyboard row and the light bar, so they only get the effect's base colour
    /// (and the engine rewrites the BIOS bar colours right after writing them).
    /// </summary>
    public static class LightingFrameRenderer
    {
        public const int AnimatedLastLamp = 102;

        public static byte[][] Render(int lampCount, double seconds)
        {
            float[] xMillimetres, yMillimetres;
            LampLayout.Get(out xMillimetres, out yMillimetres);
            float maximumX = 1, maximumY = 1;
            for (int lamp = 0; lamp < xMillimetres.Length; lamp++)
            {
                maximumX = Math.Max(maximumX, xMillimetres[lamp]);
                maximumY = Math.Max(maximumY, yMillimetres[lamp]);
            }

            ZoneLighting zone = AppSettings.KeyboardLighting;
            var frame = new byte[lampCount][];
            for (int lamp = 0; lamp < lampCount; lamp++)
            {
                double x01 = lamp < xMillimetres.Length ? xMillimetres[lamp] / maximumX : 0;
                double y01 = lamp < yMillimetres.Length ? yMillimetres[lamp] / maximumY : 0;
                frame[lamp] = lamp <= AnimatedLastLamp
                    ? LightingEffects.ColorAt(zone, lamp, x01, y01, seconds)
                    : BaseColor(zone);
            }
            return frame;
        }

        private static byte[] BaseColor(ZoneLighting zone)
        {
            if (zone.Effect == LightingEffect.Off) return new byte[] { 0, 0, 0 };
            return LightingEffects.Dim(zone.Primary, zone.BrightnessPercent / 100.0);
        }

        /// <summary>Bar zone colours scaled by the bar brightness; all black while the bar is switched off.</summary>
        public static byte[][] ScaledBarColors()
        {
            double factor = AppSettings.BarEnabled ? AppSettings.BarBrightnessPercent / 100.0 : 0;
            var colors = new byte[AppSettings.BarZoneCount][];
            for (int zone = 0; zone < colors.Length; zone++)
                colors[zone] = LightingEffects.Dim(AppSettings.BarZoneColors[zone], factor);
            return colors;
        }
    }
}
