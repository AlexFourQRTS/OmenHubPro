using System.Globalization;
using OmenHub.Core.Processes;

namespace OmenHub.Hardware.Sensors
{
    /// <summary>Reads GPU temperature, power, clock and load through nvidia-smi (refreshed every other cycle).</summary>
    public sealed class GpuSmiReader
    {
        private const string QueryArguments =
            "--query-gpu=temperature.gpu,power.draw,clocks.gr,utilization.gpu --format=csv,noheader,nounits";
        private const int QueryTimeoutMilliseconds = 4000;
        private const int RefreshEveryCycles = 2;

        private string[] cachedFields;
        private int cycleCounter;

        public void ReadInto(SensorSnapshot snapshot)
        {
            if (cycleCounter++ % RefreshEveryCycles == 0 || cachedFields == null)
            {
                string[] fields = ProcessRunner.Run("nvidia-smi", QueryArguments, QueryTimeoutMilliseconds).Split(',');
                cachedFields = fields.Length >= 4 ? fields : null;
            }
            if (cachedFields == null) return;

            int integerValue;
            double decimalValue;
            if (int.TryParse(cachedFields[0].Trim(), out integerValue)) snapshot.GpuTemperature = integerValue;
            if (double.TryParse(cachedFields[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out decimalValue))
                snapshot.GpuPowerWatts = decimalValue;
            if (int.TryParse(cachedFields[2].Trim(), out integerValue)) snapshot.GpuClockMhz = integerValue;
            if (int.TryParse(cachedFields[3].Trim(), out integerValue)) snapshot.GpuUtilizationPercent = integerValue;
        }
    }
}
