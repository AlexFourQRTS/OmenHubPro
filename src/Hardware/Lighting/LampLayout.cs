namespace OmenHub.Hardware.Lighting
{
    /// <summary>Physical positions (millimetres) of all keyboard lamps, read from the device once and cached.</summary>
    public static class LampLayout
    {
        private const int LampCount = 120;
        private const int FallbackColumns = 20;
        private const float FallbackColumnSpacing = 17f;
        private const float FallbackRowSpacing = 18f;
        private static float[] cachedX, cachedY;

        public static void Get(out float[] xMillimetres, out float[] yMillimetres)
        {
            if (cachedX == null) Load();
            xMillimetres = cachedX;
            yMillimetres = cachedY;
        }

        private static void Load()
        {
            cachedX = new float[LampCount];
            cachedY = new float[LampCount];
            using (DirectLampArray keyboard = DirectLampArray.Open())
            {
                DirectLampArray.LampInfo[] infos = keyboard == null ? null : keyboard.ReadLampInfos();
                for (int lamp = 0; lamp < LampCount; lamp++)
                {
                    bool known = infos != null && lamp < infos.Length && infos[lamp] != null;
                    cachedX[lamp] = known ? infos[lamp].XMicrometers / 1000f : (lamp % FallbackColumns) * FallbackColumnSpacing;
                    cachedY[lamp] = known ? infos[lamp].YMicrometers / 1000f : (lamp / FallbackColumns) * FallbackRowSpacing;
                }
            }
        }
    }
}
