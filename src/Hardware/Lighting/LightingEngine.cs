using System;
using System.Diagnostics;
using System.Threading;
using OmenHub.Core.Logging;
using OmenHub.Core.Settings;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>
    /// Renders the keyboard (lamps 0-102) and the light bar (lamps 103-119) about ten times a second
    /// while lighting control is enabled. Hands the keyboard back to its own effects when disabled or on exit.
    /// </summary>
    public static class LightingEngine
    {
        public const int KeyboardFirstLamp = 0;
        public const int KeyboardLastLamp = 102;
        public const int BarFirstLamp = 103;
        public const int BarLastLamp = 119;

        private const int FrameMilliseconds = 100;
        private const int KeepAliveMilliseconds = 2000;
        private const int RetryMilliseconds = 3000;

        private static Thread workerThread;
        private static volatile bool running;
        private static readonly Stopwatch clock = Stopwatch.StartNew();

        public static void Start()
        {
            running = true;
            workerThread = new Thread(RunLoop);
            workerThread.IsBackground = true;
            workerThread.Priority = ThreadPriority.AboveNormal;
            workerThread.Start();
        }

        public static void Stop()
        {
            running = false;
            try { if (workerThread != null) workerThread.Join(2000); }
            catch { }
        }

        private static void RunLoop()
        {
            DirectLampArray keyboard = null;
            byte[][] lastFrame = null;
            long lastSendTicks = 0;
            while (running)
            {
                if (!AppSettings.LightingControlEnabled) { keyboard = Release(keyboard); lastFrame = null; Thread.Sleep(FrameMilliseconds); continue; }
                if (keyboard == null)
                {
                    keyboard = DirectLampArray.Open();
                    if (keyboard == null) { AppLog.Write("lighting: keyboard LampArray did not open"); Thread.Sleep(RetryMilliseconds); continue; }
                    keyboard.SetAutonomous(false);
                    AppLog.Write("lighting: host control on, lamps=" + keyboard.LampCount);
                }
                byte[][] frame = RenderFrame(keyboard.LampCount, clock.Elapsed.TotalSeconds);
                bool changed = lastFrame == null || !SameFrame(frame, lastFrame);
                bool keepAliveDue = clock.ElapsedMilliseconds - lastSendTicks > KeepAliveMilliseconds;
                if (changed || keepAliveDue)
                {
                    keyboard.PaintEach(delegate (int lampIndex) { return frame[lampIndex]; });
                    lastFrame = frame;
                    lastSendTicks = clock.ElapsedMilliseconds;
                }
                Thread.Sleep(FrameMilliseconds);
            }
            Release(keyboard);
        }

        private static byte[][] RenderFrame(int lampCount, double seconds)
        {
            var frame = new byte[lampCount][];
            for (int lamp = 0; lamp < lampCount; lamp++)
            {
                bool isBar = lamp >= BarFirstLamp;
                ZoneLighting zone = isBar ? AppSettings.BarLighting : AppSettings.KeyboardLighting;
                int first = isBar ? BarFirstLamp : KeyboardFirstLamp;
                int size = isBar ? BarLastLamp - BarFirstLamp + 1 : KeyboardLastLamp - KeyboardFirstLamp + 1;
                frame[lamp] = LightingEffects.ColorAt(zone, lamp - first, size, seconds);
            }
            return frame;
        }

        private static bool SameFrame(byte[][] first, byte[][] second)
        {
            for (int lamp = 0; lamp < first.Length; lamp++)
                for (int channel = 0; channel < 3; channel++)
                    if (first[lamp][channel] != second[lamp][channel]) return false;
            return true;
        }

        private static DirectLampArray Release(DirectLampArray keyboard)
        {
            if (keyboard == null) return null;
            keyboard.SetAutonomous(true);
            keyboard.Dispose();
            AppLog.Write("lighting: keyboard handed back to its own effects");
            return null;
        }
    }
}
