using System;
using OmenHub.Core.Logging;

namespace OmenHub.Hardware.Fans
{
    /// <summary>
    /// Hard safety alert that ignores every user setting: at 80 °C both fans go to maximum.
    /// It releases only after the temperature drops to 74 °C and at least 20 seconds have passed.
    /// </summary>
    public sealed class ThermalAlert
    {
        public const double ActivationCelsius = 80;
        public const double ReleaseCelsius = 74;
        private const int MinimumHoldSeconds = 20;

        private DateTime activatedAt = DateTime.MinValue;

        public bool IsActive { get; private set; }

        /// <summary>Returns true while the alert is active.</summary>
        public bool Update(double hottestTemperature)
        {
            if (!IsActive && hottestTemperature >= ActivationCelsius)
            {
                IsActive = true;
                activatedAt = DateTime.Now;
                AppLog.Write("ALERT: " + (int)hottestTemperature + "C -> both fans MAX");
            }
            else if (IsActive && hottestTemperature <= ReleaseCelsius
                && (DateTime.Now - activatedAt).TotalSeconds >= MinimumHoldSeconds)
            {
                IsActive = false;
                AppLog.Write("ALERT cleared at " + (int)hottestTemperature + "C");
            }
            return IsActive;
        }
    }
}
