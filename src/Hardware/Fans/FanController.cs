using System;
using OmenHub.Core.Logging;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Bios;
using OmenHub.Hardware.Sensors;

namespace OmenHub.Hardware.Fans
{
    /// <summary>Applies the selected fan mode (auto / max / own RPM) once per engine cycle.</summary>
    public sealed class FanController
    {
        private const int ReassertMaxSeconds = 20;
        private const int ReassertLevelSeconds = 10;
        private const int AlertReassertSeconds = 8;
        private const int NoMode = -99;

        private readonly ThermalAlert alert = new ThermalAlert();
        private int appliedMode = NoMode;
        private int lastCpuRpm = -1;
        private int lastGpuRpm = -1;
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

            int floor = SafetyFloor(hottest);
            int cpuRpm = Math.Max(AppSettings.ManualCpuFanRpm, floor);
            int gpuRpm = Math.Max(AppSettings.ManualGpuFanRpm, floor);
            SendLevelIfNeeded(cpuRpm, gpuRpm, modeChanged);
        }

        private void SwitchMode(FanMode mode)
        {
            BiosFanCommands.SetFanMax(mode == FanMode.Max);
            appliedMode = (int)mode;
            lastCpuRpm = lastGpuRpm = -1;
            AppLog.Write("fan mode -> " + mode);
        }

        private void ForgetAppliedState()
        {
            appliedMode = NoMode;
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


        private static int SafetyFloor(double hottest)
        {
            return hottest >= 75 ? 3000 : hottest >= 70 ? 2200 : 0;
        }

    }
}
