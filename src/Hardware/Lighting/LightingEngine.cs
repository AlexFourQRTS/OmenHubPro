using System.Diagnostics;
using System.Threading;
using OmenHub.Core.Logging;
using OmenHub.Core.Settings;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>
    /// Keeps the lighting alive while the application runs. The keyboard lamps 0-102 are sent about ten times a second.
    /// The lamps 103-119 are shared with the bottom light bar: they are written only when their colour changes, and the
    /// BIOS bar colours are rewritten right after, because the BIOS wins at the moment of writing while a later lamp
    /// update would light the bar again. The keyboard is handed back to its own effects on exit.
    /// </summary>
    public static class LightingEngine
    {
        private const int FrameMilliseconds = 100;
        private const int KeepAliveMilliseconds = 2000;
        private const int RetryMilliseconds = 3000;
        private const int FirstSharedLamp = LightingFrameRenderer.AnimatedLastLamp + 1;

        private static Thread workerThread;
        private static volatile bool running;
        private static readonly Stopwatch clock = Stopwatch.StartNew();
        private static string lastBiosBarState = "";

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
            long lastSendMilliseconds = 0;
            while (running)
            {
                if (keyboard == null) keyboard = TakeControl();
                if (keyboard == null) { Thread.Sleep(RetryMilliseconds); continue; }

                byte[][] frame = LightingFrameRenderer.Render(keyboard.LampCount, clock.Elapsed.TotalSeconds);
                bool animatedChanged = lastFrame == null || RegionDiffers(frame, lastFrame, 0, FirstSharedLamp - 1);
                bool keepAliveDue = clock.ElapsedMilliseconds - lastSendMilliseconds > KeepAliveMilliseconds;
                if (animatedChanged || keepAliveDue)
                {
                    keyboard.PaintLamps(0, FirstSharedLamp - 1, delegate (int lampIndex) { return frame[lampIndex]; });
                    lastSendMilliseconds = clock.ElapsedMilliseconds;
                }
                bool sharedChanged = lastFrame == null || RegionDiffers(frame, lastFrame, FirstSharedLamp, frame.Length - 1);
                if (sharedChanged)
                {
                    keyboard.PaintLamps(FirstSharedLamp, frame.Length - 1, delegate (int lampIndex) { return frame[lampIndex]; });
                    lastBiosBarState = "";   // the lamp write may have lit the bar: force the BIOS colours again
                }
                lastFrame = frame;
                ApplyBiosBarIfChanged();
                Thread.Sleep(FrameMilliseconds);
            }
            Release(keyboard);
        }

        private static DirectLampArray TakeControl()
        {
            DirectLampArray keyboard = DirectLampArray.Open();
            if (keyboard == null) { AppLog.Write("lighting: keyboard LampArray did not open"); return null; }
            keyboard.SetAutonomous(false);
            AppLog.Write("lighting: host control on, lamps=" + keyboard.LampCount);
            return keyboard;
        }

        private static void ApplyBiosBarIfChanged()
        {
            string state = BarZoneStorage.Serialize() + "@" + AppSettings.BarBrightnessPercent + (AppSettings.BarEnabled ? "+on" : "+off");
            if (state == lastBiosBarState) return;
            bool accepted = BiosKeyboardPainter.SetBarZones(LightingFrameRenderer.ScaledBarColors());
            AppLog.Write("lighting: BIOS bar zones " + state + " accepted=" + accepted);
            lastBiosBarState = state;
        }

        private static bool RegionDiffers(byte[][] first, byte[][] second, int firstLamp, int lastLamp)
        {
            for (int lamp = firstLamp; lamp <= lastLamp && lamp < first.Length; lamp++)
                for (int channel = 0; channel < 3; channel++)
                    if (first[lamp][channel] != second[lamp][channel]) return true;
            return false;
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
