using System;
using OmenHub.Core.Logging;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Bios;
using OmenHub.Hardware.Sensors;

namespace OmenHub.Hardware.Fans
{
    /// <summary>Applies the selected fan mode (auto / max / manual / curve) once per engine cycle.</summary>
    public sealed class FanController
    {
        private const int ReassertMaxSeconds = 20;
        private const int ReassertLevelSeconds = 10;
        private const int AlertReassertSeconds = 8;
        private const int RampUpRpmPerCycle = 800;
        private const int RampDownRpmPerCycle = 200;
        private const int NoMode = -99;

        private readonly ThermalAlert alert = new ThermalAlert();
        private int appliedMode = NoMode;
        private int lastCpuRpm = -1;
        private int lastGpuRpm = -1;
        private int smoothedCpuRpm = -1;
        private int smoothedGpuRpm = -1;
        private DateTime lastCommandTime = DateTime.MinValue;

        public void Apply(SensorSnapshot snapshot)
        {
            double hottest = snapshot.HottestTemperature;
            bool alertWasActive = alert.IsActive;
            if (alert.Update(hottest))
            {
                if (!alertWasActive) lastCommandTime = DateTime.MinValue;
                ReassertMax(AlertReassertSeconds);
                snapshot.ThermalAlertActive = true;
                snapshot.StatusText = "АВАРИЙНЫЙ ОБДУВ: " + (int)hottest + " °C — оба кулера на максимум";
                return;
            }
            if (alertWasActive) ForgetAppliedState();

            FanMode mode = AppSettings.Mode;
            bool modeChanged = (int)mode != appliedMode;
            if (modeChanged) SwitchMode(mode);

            if (mode == FanMode.Max) { ReassertMax(ReassertMaxSeconds); return; }
            if (mode == FanMode.Auto) return;

            int cpuRpm, gpuRpm;
            if (mode == FanMode.Manual)
            {
                cpuRpm = AppSettings.ManualCpuFanRpm;
                gpuRpm = AppSettings.ManualGpuFanRpm;
            }
            else
            {
                cpuRpm = Smooth(ref smoothedCpuRpm, AppSettings.CpuCurve.RpmAt(TemperatureFor(AppSettings.CpuCurveSource, snapshot)));
                gpuRpm = Smooth(ref smoothedGpuRpm, AppSettings.GpuCurve.RpmAt(TemperatureFor(AppSettings.GpuCurveSource, snapshot)));
            }

            int floor = SafetyFloor(hottest);
            cpuRpm = Math.Max(cpuRpm, floor);
            gpuRpm = Math.Max(gpuRpm, floor);
            SendLevelIfNeeded(cpuRpm, gpuRpm, modeChanged);
        }

        private void SwitchMode(FanMode mode)
        {
            BiosFanCommands.SetFanMax(mode == FanMode.Max);
            appliedMode = (int)mode;
            lastCpuRpm = lastGpuRpm = -1;
            smoothedCpuRpm = smoothedGpuRpm = -1;
            AppLog.Write("fan mode -> " + mode);
        }

        private void ForgetAppliedState()
        {
            appliedMode = NoMode;
            smoothedCpuRpm = smoothedGpuRpm = -1;
        }

        private void ReassertMax(int intervalSeconds)
        {
            if ((DateTime.Now - lastCommandTime).TotalSeconds <= intervalSeconds) return;
            BiosFanCommands.SetFanMax(true);
            lastCommandTime = DateTime.Now;
        }

        private void SendLevelIfNeeded(int cpuRpm, int gpuRpm, bool force)
        {
            bool changed = cpuRpm != lastCpuRpm || gpuRpm != lastGpuRpm;
            bool stale = (DateTime.Now - lastCommandTime).TotalSeconds > ReassertLevelSeconds;
            if (!force && !changed && !stale) return;
            bool accepted = BiosFanCommands.SetManualLevel(cpuRpm, gpuRpm);
            if (!accepted || force) AppLog.Write("SetFanLevel " + cpuRpm + "/" + gpuRpm + " ok=" + accepted);
            lastCpuRpm = cpuRpm;
            lastGpuRpm = gpuRpm;
            lastCommandTime = DateTime.Now;
        }

        private static int Smooth(ref int smoothed, int target)
        {
            if (smoothed < 0) smoothed = target;
            else smoothed = target > smoothed
                ? Math.Min(target, smoothed + RampUpRpmPerCycle)
                : Math.Max(target, smoothed - RampDownRpmPerCycle);
            return smoothed;
        }

        private static int SafetyFloor(double hottest)
        {
            return hottest >= 75 ? 3000 : hottest >= 70 ? 2200 : 0;
        }

        private static double TemperatureFor(TemperatureSource source, SensorSnapshot snapshot)
        {
            double system = snapshot.SystemTemperature;
            double gpu = snapshot.GpuTemperature;
            if (source == TemperatureSource.System) return system;
            if (source == TemperatureSource.Gpu) return gpu >= 0 ? gpu : system;
            return Math.Max(system, gpu);
        }
    }
}
