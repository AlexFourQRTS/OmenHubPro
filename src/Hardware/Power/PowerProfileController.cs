using System;
using OmenHub.Core.Logging;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Sensors;

namespace OmenHub.Hardware.Power
{
    /// <summary>
    /// CPU power: applies the chosen wattage. A changed target is applied at once and
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
            int targetWatts = AppSettings.CpuWatts;
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
                AppLog.Write("PowerProfile: " + "CPU" + " -> " + targetWatts + " W (was " + currentWatts + ")");
            }
        }

    }
}
