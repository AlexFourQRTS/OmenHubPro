using System;
using System.Threading;
using OmenHub.Core.Logging;
using OmenHub.Core.Settings;

namespace OmenHub.Hardware.Gpu
{
    /// <summary>
    /// Holds the GPU at a power target in watts. The driver of this laptop refuses direct power limits, so the governor
    /// measures the power through NVML about five times a second and lowers or raises the locked maximum graphics clock
    /// to follow the target. With no limit the clocks are unlocked. Clocks are always unlocked again on exit.
    /// </summary>
    public static class GpuPowerGovernor
    {
        private const int LoopMilliseconds = 200;
        private const int RetryMilliseconds = 5000;
        private const int MinimumClockMhz = 210;
        private const int MinimumCapMhz = 300;
        private const int MaximumCapMhz = 3090;
        private const double OverToleranceWatts = 1.5;
        private const double HeadroomWatts = 6;
        private const int MinimumChangeMhz = 15;

        private static Thread workerThread;
        private static volatile bool running;

        /// <summary>Latest measurements for the UI (-1 = unknown).</summary>
        public static volatile int LockedCapMhz = 0;
        public static double MeasuredWatts = -1;

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
            NvmlGpu gpu = null;
            double cap = MaximumCapMhz;
            int appliedCap = 0;
            while (running)
            {
                if (gpu == null) { gpu = NvmlGpu.TryOpen(); if (gpu == null) { Thread.Sleep(RetryMilliseconds); continue; } }
                MeasuredWatts = gpu.ReadPowerWatts();

                if (!AppSettings.GpuLimitEnabled)
                {
                    if (appliedCap != 0) { gpu.UnlockClocks(); appliedCap = 0; LockedCapMhz = 0; AppLog.Write("GPU governor: unlocked"); }
                    cap = MaximumCapMhz;
                }
                else
                {
                    cap = NextCap(cap, MeasuredWatts, AppSettings.GpuWatts, gpu);
                    if (Math.Abs(cap - appliedCap) >= MinimumChangeMhz && gpu.LockClocks(MinimumClockMhz, (int)cap))
                    {
                        appliedCap = (int)cap;
                        LockedCapMhz = appliedCap;
                    }
                }
                Thread.Sleep(LoopMilliseconds);
            }
            if (gpu != null) { gpu.UnlockClocks(); gpu.Dispose(); AppLog.Write("GPU governor: clocks unlocked on exit"); }
        }

        /// <summary>Proportional step: lower the cap fast when over the target, raise it slowly when there is headroom.</summary>
        private static double NextCap(double cap, double watts, int targetWatts, NvmlGpu gpu)
        {
            if (watts < 0) return cap;
            double error = watts - targetWatts;
            if (error > OverToleranceWatts) cap -= Math.Max(15, Math.Min(300, error * 12));
            else if (error < -HeadroomWatts && gpu.ReadClockMhz() >= cap - 120) cap += Math.Max(10, Math.Min(90, -error * 5));
            return Math.Max(MinimumCapMhz, Math.Min(MaximumCapMhz, cap));
        }
    }
}
