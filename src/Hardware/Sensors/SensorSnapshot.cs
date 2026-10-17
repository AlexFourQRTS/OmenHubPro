namespace OmenHub.Hardware.Sensors
{
    /// <summary>One immutable-by-convention reading of every sensor, produced once per engine cycle.</summary>
    public sealed class SensorSnapshot
    {
        public int CpuFanRpm = -1;
        public int GpuFanRpm = -1;
        public int BiosTemperature = -1;
        public int SystemTemperature = -1;
        public int GpuTemperature = -1;
        public int GpuClockMhz = -1;
        public int GpuUtilizationPercent = -1;
        public int CpuFrequencyMhz = -1;
        public double GpuPowerWatts = -1;
        public double CpuLoadPercent = -1;
        public bool OnBattery;
        public bool ThermalAlertActive;
        public string StatusText = "";

        /// <summary>The hotter of the system and GPU temperatures; drives the thermal alert.</summary>
        public double HottestTemperature
        {
            get { return System.Math.Max(SystemTemperature, GpuTemperature); }
        }
    }
}
