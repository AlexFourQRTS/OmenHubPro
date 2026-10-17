using System.Diagnostics;
using System.Threading;
using OmenHub.Core.Logging;
using OmenHub.Core.Settings;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>
    /// Keeps the lighting alive while the application runs: sends frames to the keyboard's lamps about ten times a second
    /// and, when the bar colours change, writes them to the BIOS colour table too (the BIOS keeps them after exit).
    /// Hands the keyboard back to its own effects when the application exits.
    /// </summary>
    public static class LightingEngine
    {
        private const int FrameMilliseconds = 100;
        private const int KeepAliveMilliseconds = 2000;
        private const int RetryMilliseconds = 3000;

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

                ApplyBiosBarIfChanged();
                byte[][] frame = LightingFrameRenderer.Render(keyboard.LampCount, clock.Elapsed.TotalSeconds);
                bool changed = lastFrame == null || !SameFrame(frame, lastFrame);
                bool keepAliveDue = clock.ElapsedMilliseconds - lastSendMilliseconds > KeepAliveMilliseconds;
                if (changed || keepAliveDue)
                {
                    keyboard.PaintEach(delegate (int lampIndex) { return frame[lampIndex]; });
                    lastFrame = frame;
                    lastSendMilliseconds = clock.ElapsedMilliseconds;
                }
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
            string state = BarZoneStorage.Serialize() + "@" + AppSettings.BarBrightnessPercent;
            if (state == lastBiosBarState) return;
            bool accepted = BiosKeyboardPainter.SetBarZones(LightingFrameRenderer.ScaledBarColors());
            AppLog.Write("lighting: BIOS bar zones " + state + " accepted=" + accepted);
            lastBiosBarState = state;
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
