namespace OmenHub.Hardware.Bios
{
    /// <summary>Read-only system queries over the BIOS interface.</summary>
    public static class BiosSystemCommands
    {
        private const uint CommandGetTemperature = 0x23;
        private const uint CommandGetGpuPower = 0x21;

        /// <summary>BIOS thermal sensor in °C, or -1 when unavailable.</summary>
        public static int GetTemperature()
        {
            byte[] data;
            return BiosInterface.Call(BiosInterface.DefaultCommand, CommandGetTemperature, null, 4, out data) ? data[0] : -1;
        }

        public static string DescribeGpuPowerMode()
        {
            byte[] data;
            if (!BiosInterface.Call(BiosInterface.DefaultCommand, CommandGetGpuPower, null, 4, out data)) return "н/д";
            return string.Format("Custom TGP: {0}   Dynamic Boost: {1}",
                data[0] != 0 ? "вкл" : "выкл", data[1] != 0 ? "вкл" : "выкл");
        }
    }
}
