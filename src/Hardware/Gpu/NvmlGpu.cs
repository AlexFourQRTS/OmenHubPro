using System;
using System.Runtime.InteropServices;
using OmenHub.Core.Logging;

namespace OmenHub.Hardware.Gpu
{
    /// <summary>Direct access to the NVIDIA driver through nvml.dll: power reading and clock locking (needs administrator).</summary>
    public sealed class NvmlGpu : IDisposable
    {
        private const uint GraphicsClock = 0;
        private IntPtr device;

        [StructLayout(LayoutKind.Sequential)]
        private struct Utilization { public uint Gpu; public uint Memory; }

        [DllImport("nvml.dll")] private static extern int nvmlInit_v2();
        [DllImport("nvml.dll")] private static extern int nvmlShutdown();
        [DllImport("nvml.dll")] private static extern int nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);
        [DllImport("nvml.dll")] private static extern int nvmlDeviceGetPowerUsage(IntPtr device, out uint milliwatts);
        [DllImport("nvml.dll")] private static extern int nvmlDeviceGetClockInfo(IntPtr device, uint clockType, out uint megahertz);
        [DllImport("nvml.dll")] private static extern int nvmlDeviceGetUtilizationRates(IntPtr device, out Utilization utilization);
        [DllImport("nvml.dll")] private static extern int nvmlDeviceSetGpuLockedClocks(IntPtr device, uint minimumMegahertz, uint maximumMegahertz);
        [DllImport("nvml.dll")] private static extern int nvmlDeviceResetGpuLockedClocks(IntPtr device);

        public static NvmlGpu TryOpen()
        {
            try
            {
                if (nvmlInit_v2() != 0) return null;
                IntPtr handle;
                if (nvmlDeviceGetHandleByIndex_v2(0, out handle) != 0) return null;
                return new NvmlGpu { device = handle };
            }
            catch (Exception error)
            {
                AppLog.Write("NVML unavailable: " + error.Message);
                return null;
            }
        }

        public double ReadPowerWatts()
        {
            uint milliwatts;
            return nvmlDeviceGetPowerUsage(device, out milliwatts) == 0 ? milliwatts / 1000.0 : -1;
        }

        public int ReadClockMhz()
        {
            uint megahertz;
            return nvmlDeviceGetClockInfo(device, GraphicsClock, out megahertz) == 0 ? (int)megahertz : -1;
        }

        public int ReadUtilizationPercent()
        {
            Utilization utilization;
            return nvmlDeviceGetUtilizationRates(device, out utilization) == 0 ? (int)utilization.Gpu : -1;
        }

        public bool LockClocks(int minimumMegahertz, int maximumMegahertz)
        {
            int result = nvmlDeviceSetGpuLockedClocks(device, (uint)minimumMegahertz, (uint)maximumMegahertz);
            if (result != 0) AppLog.Write("NVML lock clocks " + minimumMegahertz + "-" + maximumMegahertz + " failed: " + result);
            return result == 0;
        }

        public bool UnlockClocks()
        {
            return nvmlDeviceResetGpuLockedClocks(device) == 0;
        }

        public void Dispose()
        {
            try { nvmlShutdown(); }
            catch { }
        }
    }
}
