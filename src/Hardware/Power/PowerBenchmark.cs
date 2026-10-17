using System;
using System.Text;
using System.Threading;
using OmenHub.Core.Logging;
using OmenHub.Hardware.Sensors;

namespace OmenHub.Hardware.Power
{
    /// <summary>Loads every core for a few seconds at each tested limit and logs the measured package power.</summary>
    public sealed class PowerBenchmark
    {
        private const int SamplesPerLimit = 6;
        private const int SampleIntervalMilliseconds = 2000;
        private const int AbortTemperature = 90;

        private static readonly int[] TestedLimits = { 35, 75 };
        private readonly Func<SensorSnapshot> snapshotProvider;
        private volatile bool stopBurning;

        public PowerBenchmark(Func<SensorSnapshot> snapshotProvider)
        {
            this.snapshotProvider = snapshotProvider;
        }

        public string Run()
        {
            var summary = new StringBuilder();
            try
            {
                foreach (int limitWatts in TestedLimits) summary.AppendLine(MeasureLimit(limitWatts));
            }
            catch (Exception error)
            {
                AppLog.Write("TEST error: " + error);
                summary.AppendLine("Ошибка: " + error.Message);
            }
            stopBurning = true;
            AppLog.Write("TEST done\r\n" + summary);
            return summary.ToString();
        }

        private string MeasureLimit(int limitWatts)
        {
            AppLog.Write("TEST: set " + limitWatts + " W");
            RyzenAdjTool.SetPowerLimit(limitWatts);
            Thread.Sleep(1500);
            StartBurning();

            double peakFast = 0, peakSlow = 0, peakSustained = 0;
            int peakMhz = 0;
            for (int sample = 1; sample <= SamplesPerLimit; sample++)
            {
                Thread.Sleep(SampleIntervalMilliseconds);
                string table = ReadTable();
                double fast = RyzenAdjTool.FindValue(table, "PPT VALUE FAST");
                double slow = RyzenAdjTool.FindValue(table, "PPT VALUE SLOW");
                double sustained = RyzenAdjTool.FindValue(table, "STAPM VALUE");
                SensorSnapshot snapshot = snapshotProvider();
                AppLog.Write(string.Format("TEST limit={0}W t={1}s  VALUE fast={2:0.0} slow={3:0.0} stapm={4:0.0}  cpuMHz={5} load={6:0}% sysT={7}",
                    limitWatts, sample * 2, fast, slow, sustained, snapshot.CpuFrequencyMhz, snapshot.CpuLoadPercent, snapshot.SystemTemperature));
                if (!double.IsNaN(fast)) peakFast = Math.Max(peakFast, fast);
                if (!double.IsNaN(slow)) peakSlow = Math.Max(peakSlow, slow);
                if (!double.IsNaN(sustained)) peakSustained = Math.Max(peakSustained, sustained);
                peakMhz = Math.Max(peakMhz, snapshot.CpuFrequencyMhz);
                if (snapshot.SystemTemperature >= AbortTemperature) { AppLog.Write("TEST aborted: too hot"); break; }
            }
            stopBurning = true;
            Thread.Sleep(5000);
            return string.Format("Лимит {0} Вт: пик fast={1:0.0} Вт, slow={2:0.0} Вт, stapm={3:0.0} Вт, частота до {4} МГц",
                limitWatts, peakFast, peakSlow, peakSustained, peakMhz);
        }

        private static string ReadTable()
        {
            int exitCode;
            return RyzenAdjTool.Run("-i", out exitCode);
        }

        private void StartBurning()
        {
            stopBurning = false;
            for (int index = 0; index < Environment.ProcessorCount; index++)
            {
                var burner = new Thread(BurnProcessor);
                burner.IsBackground = true;
                burner.Start();
            }
        }

        private void BurnProcessor()
        {
            double value = 1.0001;
            while (!stopBurning)
                for (int step = 0; step < 100000; step++) value = Math.Sqrt(value * 1.0000001 + 0.5);
        }
    }
}
