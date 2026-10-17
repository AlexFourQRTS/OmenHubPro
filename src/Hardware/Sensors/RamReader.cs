using System.Runtime.InteropServices;

namespace OmenHub.Hardware.Sensors
{
    /// <summary>Physical memory in use and installed, in gigabytes.</summary>
    public static class RamReader
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatus
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhysical, AvailablePhysical, TotalPageFile, AvailablePageFile, TotalVirtual, AvailableVirtual, AvailableExtendedVirtual;
        }

        [DllImport("kernel32.dll")]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);

        private const double BytesPerGigabyte = 1024.0 * 1024 * 1024;

        public static void ReadInto(SensorSnapshot snapshot)
        {
            var status = new MemoryStatus();
            status.Length = (uint)Marshal.SizeOf(status);
            if (!GlobalMemoryStatusEx(ref status)) return;
            snapshot.RamTotalGb = status.TotalPhysical / BytesPerGigabyte;
            snapshot.RamUsedGb = (status.TotalPhysical - status.AvailablePhysical) / BytesPerGigabyte;
        }
    }
}
