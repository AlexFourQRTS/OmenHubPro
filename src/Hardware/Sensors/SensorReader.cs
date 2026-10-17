using System;
using System.Windows.Forms;
using OmenHub.Hardware.Bios;

namespace OmenHub.Hardware.Sensors
{
    /// <summary>Combines every sensor source into one snapshot per engine cycle.</summary>
    public sealed class SensorReader
    {
        private readonly bool biosAvailable;
        private readonly AcpiThermalReader acpiReader = new AcpiThermalReader();
        private readonly GpuSmiReader gpuReader = new GpuSmiReader();
        private readonly CpuCounterReader cpuReader = new CpuCounterReader();

        public SensorReader(bool biosAvailable)
        {
            this.biosAvailable = biosAvailable;
        }

        public SensorSnapshot Read()
        {
            var snapshot = new SensorSnapshot();
            if (biosAvailable)
            {
                int cpuFanRpm, gpuFanRpm;
                if (BiosFanCommands.TryGetRpm(out cpuFanRpm, out gpuFanRpm))
                {
                    snapshot.CpuFanRpm = cpuFanRpm;
                    snapshot.GpuFanRpm = gpuFanRpm;
                }
                snapshot.BiosTemperature = BiosSystemCommands.GetTemperature();
                snapshot.StatusText = "BIOS: подключён";
            }
            else snapshot.StatusText = "BIOS недоступен: " + BiosInterface.LastError;

            snapshot.OnBattery = IsRunningOnBattery();
            double hottestZone = acpiReader.ReadHottestZone();
            snapshot.SystemTemperature = (int)Math.Round(Math.Max(snapshot.BiosTemperature, hottestZone));
            cpuReader.ReadInto(snapshot);
            gpuReader.ReadInto(snapshot);
            RamReader.ReadInto(snapshot);
            return snapshot;
        }

        private static bool IsRunningOnBattery()
        {
            try { return SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Offline; }
            catch { return false; }
        }
    }
}
