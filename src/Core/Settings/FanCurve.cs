using System;

namespace OmenHub.Core.Settings
{
    /// <summary>Piecewise-linear fan curve: temperature (°C) to fan speed (RPM).</summary>
    public sealed class FanCurve
    {
        public const int MinimumPoints = 3;
        public const int MaximumPoints = 12;

        public int[] Temperatures = new int[0];
        public int[] Rpms = new int[0];

        public static FanCurve CreateDefault()
        {
            return new FanCurve
            {
                Temperatures = new[] { 40, 50, 60, 70, 78 },
                Rpms = new[] { 1800, 2200, 3000, 4000, 5500 }
            };
        }

        public int RpmAt(double temperature)
        {
            int[] temperatureSnapshot = Temperatures;
            int[] rpmSnapshot = Rpms;
            int pointCount = Math.Min(temperatureSnapshot.Length, rpmSnapshot.Length);
            if (pointCount == 0) return 0;
            if (temperature <= temperatureSnapshot[0]) return rpmSnapshot[0];

            for (int index = 1; index < pointCount; index++)
            {
                if (temperature > temperatureSnapshot[index]) continue;
                double span = Math.Max(1, temperatureSnapshot[index] - temperatureSnapshot[index - 1]);
                double fraction = (temperature - temperatureSnapshot[index - 1]) / span;
                return (int)(rpmSnapshot[index - 1] + fraction * (rpmSnapshot[index] - rpmSnapshot[index - 1]));
            }
            return rpmSnapshot[pointCount - 1];
        }

        public string Serialize()
        {
            return JoinNumbers(Temperatures) + ";" + JoinNumbers(Rpms);
        }

        public static FanCurve Deserialize(string text)
        {
            if (text == null) return null;
            string[] halves = text.Split(';');
            if (halves.Length != 2) return null;
            int[] temperatures = ParseNumbers(halves[0]);
            int[] rpms = ParseNumbers(halves[1]);
            bool valid = temperatures != null && rpms != null && temperatures.Length == rpms.Length
                && temperatures.Length >= MinimumPoints && temperatures.Length <= MaximumPoints;
            return valid ? new FanCurve { Temperatures = temperatures, Rpms = rpms } : null;
        }

        private static string JoinNumbers(int[] numbers)
        {
            string[] parts = new string[numbers.Length];
            for (int index = 0; index < numbers.Length; index++) parts[index] = numbers[index].ToString();
            return string.Join(",", parts);
        }

        private static int[] ParseNumbers(string text)
        {
            string[] parts = text.Split(',');
            int[] numbers = new int[parts.Length];
            for (int index = 0; index < parts.Length; index++)
                if (!int.TryParse(parts[index], out numbers[index])) return null;
            return numbers;
        }
    }
}
