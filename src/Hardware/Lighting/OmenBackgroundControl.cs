using System.Diagnostics;
using Microsoft.Win32;
using OmenHub.Core.Logging;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>Temporarily removes things that may hold the lighting hardware (HP background app, Windows ambient effects).</summary>
    public static class OmenBackgroundControl
    {
        private const string OmenBackgroundProcess = "OmenCommandCenterBackground";
        private const string LightingKeyPath = @"Software\Microsoft\Lighting";
        private const string AmbientValueName = "AmbientLightingEnabled";

        /// <summary>Closes the HP OMEN background process; it starts again with OMEN Gaming Hub.</summary>
        public static string StopOmenBackground()
        {
            Process[] processes = Process.GetProcessesByName(OmenBackgroundProcess);
            foreach (Process process in processes)
            {
                try { process.Kill(); }
                catch { }
            }
            AppLog.Write("stopped OMEN background processes: " + processes.Length);
            return "закрыто процессов OMEN: " + processes.Length;
        }

        /// <summary>Switches Windows "ambient lighting" off or on; returns the previous value (or -1).</summary>
        public static int SetWindowsAmbientLighting(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(LightingKeyPath))
            {
                object previous = key.GetValue(AmbientValueName);
                key.SetValue(AmbientValueName, enabled ? 1 : 0, RegistryValueKind.DWord);
                AppLog.Write("windows ambient lighting -> " + enabled);
                return previous is int ? (int)previous : -1;
            }
        }
    }
}
