using System;

namespace OmenHub.Hardware.Bios
{
    /// <summary>Fan-related BIOS commands (command 0x20008).</summary>
    public static class BiosFanCommands
    {
        private const uint CommandGetFanRpm = 0x2D;
        private const uint CommandSetFanMax = 0x27;
        private const uint CommandSetFanLevel = 0x2E;
        private const int RpmPerUnit = 100;
        private const int MaximumUnits = 60;   // 6000 RPM

        /// <summary>Real (measured) fan speeds in RPM.</summary>
        public static bool TryGetRpm(out int cpuFanRpm, out int gpuFanRpm)
        {
            cpuFanRpm = gpuFanRpm = -1;
            byte[] data;
            if (!BiosInterface.Call(BiosInterface.DefaultCommand, CommandGetFanRpm, null, 128, out data)) return false;
            cpuFanRpm = data[0] * RpmPerUnit;
            gpuFanRpm = data[1] * RpmPerUnit;
            return true;
        }

        /// <summary>Switches the fans to manual and sets both speeds; the level persists until FanMax is toggled off.</summary>
        public static bool SetManualLevel(int cpuFanRpm, int gpuFanRpm)
        {
            byte[] payload = { ToUnits(cpuFanRpm), ToUnits(gpuFanRpm), 0, 0 };
            byte[] ignored;
            return BiosInterface.Call(BiosInterface.DefaultCommand, CommandSetFanLevel, payload, 4, out ignored);
        }

        /// <summary>FanMax on = both fans at full speed. FanMax off hands control back to the BIOS.</summary>
        public static bool SetFanMax(bool enabled)
        {
            byte[] payload = { (byte)(enabled ? 1 : 0), 0, 0, 0 };
            byte[] ignored;
            return BiosInterface.Call(BiosInterface.DefaultCommand, CommandSetFanMax, payload, 4, out ignored);
        }

        private static byte ToUnits(int rpm)
        {
            return (byte)Math.Max(0, Math.Min(MaximumUnits, rpm / RpmPerUnit));
        }
    }
}
