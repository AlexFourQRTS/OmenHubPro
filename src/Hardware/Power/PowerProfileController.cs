using System;
using OmenHub.Core.Logging;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Sensors;

namespace OmenHub.Hardware.Power
{
    /// <summary>
    /// CPU power profile: on battery the limit is BatteryWatts, on AC it is ProAutoWatts (when enabled),
    /// otherwise the controller leaves the CPU alone. A changed target is applied at once and
    /// re-verified every 30 seconds because firmware may reset the limit.
    /// </summary>
    public sealed class PowerProfileController
    {
        private const int VerifyIntervalSeconds = 30;
        private const double ToleranceWatts = 0.5;
        private const int NoTarget = -1;

        private DateTime lastVerification = DateTime.MinValue;
        private int appliedTargetWatts = NoTarget;

        public void Apply(SensorSnapshot snapshot)
        {
            int targetWatts = ChooseTarget(snapshot.OnBattery);
            if (targetWatts == NoTarget) { appliedTargetWatts = NoTarget; return; }
            if (!RyzenAdjTool.IsPresent) return;

            bool targetChanged = targetWatts != appliedTargetWatts;
            bool verificationDue = (DateTime.Now - lastVerification).TotalSeconds >= VerifyIntervalSeconds;
            if (!targetChanged && !verificationDue) return;

            appliedTargetWatts = targetWatts;
            lastVerification = DateTime.Now;
            double currentWatts = targetChanged ? double.NaN : RyzenAdjTool.ReadSustainedLimit();
            if (double.IsNaN(currentWatts) || Math.Abs(currentWatts - targetWatts) > ToleranceWatts)
            {
                RyzenAdjTool.SetPowerLimit(targetWatts);
                AppLog.Write("PowerProfile: " + (snapshot.OnBattery ? "battery" : "AC") + " -> " + targetWatts + " W (was " + currentWatts + ")");
            }
        }

        private static int ChooseTarget(bool onBattery)
        {
            if (onBattery) return AppSettings.BatteryWatts;
            if (AppSettings.ProAutoEnabled) return AppSettings.ProAutoWatts;
            return AppSettings.CpuLimitEnabled ? AppSettings.CpuWatts : AppSettings.MaximumCpuWatts;
        }
    }
}
