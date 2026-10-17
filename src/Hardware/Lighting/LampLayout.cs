namespace OmenHub.Hardware.Lighting
{
    /// <summary>Physical positions (millimetres) of the keyboard lamps, read from the device once and cached.</summary>
    public static class LampLayout
    {
        private static float[] cachedX, cachedY;

        public static void Get(out float[] xMillimetres, out float[] yMillimetres)
        {
            if (cachedX == null) Load();
            xMillimetres = cachedX;
            yMillimetres = cachedY;
        }

        private static void Load()
        {
            int lampCount = LightingEngine.KeyboardLastLamp + 1;
            cachedX = new float[lampCount];
            cachedY = new float[lampCount];
            using (DirectLampArray keyboard = DirectLampArray.Open())
            {
                DirectLampArray.LampInfo[] infos = keyboard == null ? null : keyboard.ReadLampInfos();
                for (int lamp = 0; lamp < lampCount; lamp++)
                {
                    bool known = infos != null && lamp < infos.Length && infos[lamp] != null;
                    cachedX[lamp] = known ? infos[lamp].XMicrometers / 1000f : (lamp % 20) * 17f;   // fallback: even 20-column grid
                    cachedY[lamp] = known ? infos[lamp].YMicrometers / 1000f : (lamp / 20) * 18f;
                }
            }
        }
    }
}
