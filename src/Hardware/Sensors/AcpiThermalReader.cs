using System;
using System.Management;

namespace OmenHub.Hardware.Sensors
{
    /// <summary>Hottest ACPI thermal zone in °C (works without administrator rights).</summary>
    public sealed class AcpiThermalReader
    {
        private const double KelvinOffset = 273.15;
        private readonly ManagementObjectSearcher zoneSearcher;

        public AcpiThermalReader()
        {
            try
            {
                zoneSearcher = new ManagementObjectSearcher(@"root\cimv2",
                    "select Name,Temperature from Win32_PerfFormattedData_Counters_ThermalZoneInformation");
            }
            catch { zoneSearcher = null; }
        }

        public double ReadHottestZone()
        {
            double hottest = -1;
            if (zoneSearcher == null) return hottest;
            try
            {
                foreach (ManagementObject zone in zoneSearcher.Get())
                {
                    double celsius = Convert.ToDouble(zone["Temperature"]) - KelvinOffset;
                    if (celsius > hottest) hottest = celsius;
                }
            }
            catch { }
            return hottest;
        }
    }
}
