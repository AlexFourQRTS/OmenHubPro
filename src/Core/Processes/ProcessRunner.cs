using System;
using System.Diagnostics;
using System.Text;

namespace OmenHub.Core.Processes
{
    /// <summary>Runs a console program without a window and returns its text output.</summary>
    public static class ProcessRunner
    {
        private const int ConsoleCodePage = 866;

        public static string Run(string fileName, string arguments, int timeoutMilliseconds)
        {
            int exitCode;
            return RunWithExitCode(fileName, arguments, timeoutMilliseconds, out exitCode);
        }

        public static string RunWithExitCode(string fileName, string arguments, int timeoutMilliseconds, out int exitCode)
        {
            exitCode = -1;
            try
            {
                var startInfo = new ProcessStartInfo(fileName, arguments);
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.StandardOutputEncoding = Encoding.GetEncoding(ConsoleCodePage);
                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                    process.WaitForExit(timeoutMilliseconds);
                    exitCode = process.HasExited ? process.ExitCode : -2;
                    return output.Trim();
                }
            }
            catch (Exception error)
            {
                return "Ошибка запуска " + fileName + ": " + error.Message;
            }
        }
    }
}
