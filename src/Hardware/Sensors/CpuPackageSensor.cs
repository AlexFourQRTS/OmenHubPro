using System.Threading;
using OmenHub.Core.Logging;
using OmenHub.Hardware.Power;

namespace OmenHub.Hardware.Sensors
{
    /// <summary>
    /// CPU package power from the RyzenAdj table, read in the background only while something needs it (the overlay).
    /// "PPT VALUE FAST" is the instantaneous package power; "STAPM VALUE" and "PPT VALUE SLOW" are smoothed averages.
    /// </summary>
    public static class CpuPackageSensor
    {
        private const int IntervalMilliseconds = 2000;
        private const int IdleMilliseconds = 500;
        private static Thread workerThread;

        /// <summary>Set by whoever shows the CPU power; the reading thread sleeps while this is false.</summary>
        public static volatile bool Wanted;
        public static volatile int ReadingCount;
        public static double FastWatts = -1, SlowWatts = -1, StapmWatts = -1;

        public static void Start()
        {
            if (workerThread != null) return;
            workerThread = new Thread(RunLoop);
            workerThread.IsBackground = true;
            workerThread.Start();
        }

        private static void RunLoop()
        {
            while (true)
            {
                if (!Wanted || !RyzenAdjTool.IsPresent) { Thread.Sleep(IdleMilliseconds); continue; }
                int exitCode;
                string table = RyzenAdjTool.Run("-i", out exitCode, false);
                FastWatts = RyzenAdjTool.FindValue(table, "PPT VALUE FAST");
                SlowWatts = RyzenAdjTool.FindValue(table, "PPT VALUE SLOW");
                StapmWatts = RyzenAdjTool.FindValue(table, "STAPM VALUE");
                if (++ReadingCount % 5 == 0)
                    AppLog.Write(string.Format("CPU power candidates: fast={0:0.0} slow={1:0.0} stapm={2:0.0}", FastWatts, SlowWatts, StapmWatts));
                Thread.Sleep(IntervalMilliseconds);
            }
        }
    }
}
