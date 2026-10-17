using System;
using System.Diagnostics;
using System.Management;

namespace OmenHub.Hardware.Sensors
{
    /// <summary>CPU load and an estimated frequency from Windows performance counters.</summary>
    public sealed class CpuCounterReader
    {
        private const string CounterCategory = "Processor Information";
        private PerformanceCounter utilityCounter;
        private PerformanceCounter performanceCounter;
        private double baseFrequencyMhz;

        public CpuCounterReader()
        {
            utilityCounter = CreateCounter("% Processor Utility");
            performanceCounter = CreateCounter("% Processor Performance");
            baseFrequencyMhz = ReadBaseFrequency();
        }

        public void ReadInto(SensorSnapshot snapshot)
        {
            if (utilityCounter != null)
            {
                try { snapshot.CpuLoadPercent = Math.Min(100, utilityCounter.NextValue()); }
                catch { }
            }
            if (performanceCounter != null && baseFrequencyMhz > 0)
            {
                try { snapshot.CpuFrequencyMhz = (int)(performanceCounter.NextValue() / 100.0 * baseFrequencyMhz); }
                catch { }
            }
        }

        private static PerformanceCounter CreateCounter(string counterName)
        {
            try
            {
                var counter = new PerformanceCounter(CounterCategory, counterName, "_Total");
                counter.NextValue();
                return counter;
            }
            catch { return null; }
        }

        private static double ReadBaseFrequency()
        {
            try
            {
                foreach (ManagementObject processor in new ManagementObjectSearcher("select MaxClockSpeed from Win32_Processor").Get())
                    return Convert.ToDouble(processor["MaxClockSpeed"]);
            }
            catch { }
            return 0;
        }
    }
}
