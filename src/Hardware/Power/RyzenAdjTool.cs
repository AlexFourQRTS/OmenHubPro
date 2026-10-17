using System;
using System.Globalization;
using System.IO;
using OmenHub.Core.Logging;
using OmenHub.Core.Processes;

namespace OmenHub.Hardware.Power
{
    /// <summary>Wrapper around tools\ryzenadj\ryzenadj.exe (sets and reads the CPU power limits).</summary>
    public static class RyzenAdjTool
    {
        private const int TimeoutMilliseconds = 20000;
        private const int MaximumLoggedCharacters = 400;

        public static string ExecutablePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"tools\ryzenadj\ryzenadj.exe"); }
        }

        public static bool IsPresent { get { return File.Exists(ExecutablePath); } }

        public static string Run(string arguments, out int exitCode)
        {
            exitCode = -1;
            if (!IsPresent)
            {
                AppLog.Write("ryzenadj missing: " + ExecutablePath);
                return "Не найден " + ExecutablePath;
            }
            string output = ProcessRunner.RunWithExitCode(ExecutablePath, arguments, TimeoutMilliseconds, out exitCode);
            string shown = output.Length > MaximumLoggedCharacters ? output.Substring(0, MaximumLoggedCharacters) + "..." : output;
            AppLog.Write("ryzenadj " + arguments + "  -> exit=" + exitCode + "\r\n" + shown);
            return output;
        }

        /// <summary>Sets sustained, fast and slow package limits to the same wattage.</summary>
        public static string SetPowerLimit(int watts)
        {
            int milliwatts = watts * 1000;
            int exitCode;
            return Run("--stapm-limit=" + milliwatts + " --fast-limit=" + milliwatts + " --slow-limit=" + milliwatts, out exitCode);
        }

        public static double ReadSustainedLimit()
        {
            int exitCode;
            return FindValue(Run("-i", out exitCode), "STAPM LIMIT");
        }

        /// <summary>Finds a numeric value in the "| Name | Value | Parameter |" table; NaN when absent.</summary>
        public static double FindValue(string tableText, string rowName)
        {
            foreach (string line in tableText.Split('\n'))
            {
                if (line.IndexOf("| " + rowName, StringComparison.Ordinal) < 0) continue;
                string[] cells = line.Split('|');
                double value;
                if (cells.Length > 2 && double.TryParse(cells[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    return value;
            }
            return double.NaN;
        }
    }
}
