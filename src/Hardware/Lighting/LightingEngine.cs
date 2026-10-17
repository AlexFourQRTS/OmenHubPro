using System.Diagnostics;
using System.Threading;
using OmenHub.Core.Logging;
using OmenHub.Core.Settings;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>
    /// While lighting control is enabled: renders the keyboard (lamps 0-102) about ten times a second and drives the
    /// light bar either through the BIOS colour table (only when its colour changes) or through HID lamps 103-119.
    /// Hands the keyboard back to its own effects when disabled or on exit.
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
                if (!AppSettings.LightingControlEnabled)
                {
                    keyboard = Release(keyboard);
                    lastFrame = null;
                    lastBiosBarState = "";
                    Thread.Sleep(FrameMilliseconds);
                    continue;
                }
                if (keyboard == null) keyboard = TakeControl();
                if (keyboard == null) { Thread.Sleep(RetryMilliseconds); continue; }

                ApplyBiosBarIfNeeded();
                byte[][] frame = RenderFrame(keyboard.LampCount, clock.Elapsed.TotalSeconds);
                bool changed = lastFrame == null || !SameFrame(frame, lastFrame);
                bool keepAliveDue = clock.ElapsedMilliseconds - lastSendMilliseconds > KeepAliveMilliseconds;
                if (changed || keepAliveDue)
                {
                    int lastLamp = AppSettings.BarMode == BarBackend.Lamps ? keyboard.LampCount - 1 : KeyboardLastLamp;
                    keyboard.PaintLamps(KeyboardFirstLamp, lastLamp, delegate (int lampIndex) { return frame[lampIndex]; });
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
            AppLog.Write("lighting: host control on, lamps=" + keyboard.LampCount + ", bar=" + AppSettings.BarMode);
            return keyboard;
        }

        /// <summary>Writes the bar colour to the BIOS only when it (or the mode) changed; the BIOS keeps it afterwards.</summary>
        private static void ApplyBiosBarIfNeeded()
        {
            if (AppSettings.BarMode != BarBackend.Bios) { lastBiosBarState = ""; return; }
            ZoneLighting bar = AppSettings.BarLighting;
            double factor = bar.Effect == LightingEffect.Off ? 0 : bar.BrightnessPercent / 100.0;
            byte red = (byte)(bar.Red * factor), green = (byte)(bar.Green * factor), blue = (byte)(bar.Blue * factor);
            string state = red + "," + green + "," + blue;
            if (state == lastBiosBarState) return;
            bool accepted = BiosKeyboardPainter.SetBarColor(red, green, blue);
            AppLog.Write("lighting: BIOS bar colour " + state + " accepted=" + accepted);
            lastBiosBarState = state;
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
                byte[] keyColor;
                if (!isBar && zone.Effect != LightingEffect.Off && AppSettings.KeyColors.TryGetValue(lamp, out keyColor))
                    frame[lamp] = LightingEffects.Dim(keyColor, zone.BrightnessPercent / 100.0);
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
