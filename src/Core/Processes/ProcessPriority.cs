using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OmenHub.Core.Logging;

namespace OmenHub.Core.Processes
{
    /// <summary>
    /// Raises this process to High priority and switches off Windows "efficiency mode" throttling.
    /// Real-time is deliberately not used: it can starve input threads and freeze the machine.
    /// </summary>
    public static class ProcessPriority
    {
        private const int ProcessPowerThrottlingInformationClass = 4;
        private const uint PowerThrottlingCurrentVersion = 1;
        private const uint PowerThrottlingExecutionSpeed = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct PowerThrottlingState
        {
            public uint Version;
            public uint ControlMask;
            public uint StateMask;
        }

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessInformation(IntPtr process, int informationClass, ref PowerThrottlingState state, int size);

        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint milliseconds);

        public static void RaiseToHigh()
        {
            Process current = Process.GetCurrentProcess();
            try { current.PriorityClass = ProcessPriorityClass.High; }
            catch (Exception error) { AppLog.Write("priority failed: " + error.Message); }

            try
            {
                var state = new PowerThrottlingState
                {
                    Version = PowerThrottlingCurrentVersion,
                    ControlMask = PowerThrottlingExecutionSpeed,
                    StateMask = 0
                };
                SetProcessInformation(current.Handle, ProcessPowerThrottlingInformationClass, ref state, Marshal.SizeOf(state));
            }
            catch { }

            try { timeBeginPeriod(1); }
            catch { }
            AppLog.Write("priority: " + current.PriorityClass + ", power throttling off");
        }
    }
}
