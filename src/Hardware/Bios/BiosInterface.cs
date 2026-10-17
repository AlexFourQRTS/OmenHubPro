using System;
using System.Management;
using OmenHub.Core.Logging;

namespace OmenHub.Hardware.Bios
{
    /// <summary>
    /// Raw access to the HP BIOS WMI interface (root\wmi, class hpqBIntM).
    /// Payloads shorter than 4 bytes are zero-padded because the BIOS rejects other sizes.
    /// </summary>
    public static class BiosInterface
    {
        public const uint DefaultCommand = 0x20008;
        public const uint KeyboardCommand = 0x20009;
        private const int MinimumPayloadBytes = 4;

        private static readonly byte[] Signature = { 0x53, 0x45, 0x43, 0x55 };   // "SECU"
        private static readonly object callLock = new object();
        private static ManagementObject biosInstance;
        private static ManagementClass inputClass;

        public static string LastError = "";

        public static bool Initialize()
        {
            try
            {
                var biosClass = new ManagementClass(@"root\wmi:hpqBIntM");
                foreach (ManagementObject instance in biosClass.GetInstances()) { biosInstance = instance; break; }
                inputClass = new ManagementClass(@"root\wmi:hpqBDataIn");
                if (biosInstance == null)
                {
                    LastError = "hpqBIntM не найден";
                    AppLog.Write("BIOS init: hpqBIntM not found");
                    return false;
                }
                AppLog.Write("BIOS init OK: " + biosInstance["InstanceName"]);
                return true;
            }
            catch (Exception error)
            {
                LastError = error.Message;
                AppLog.Write("BIOS init FAILED: " + error.Message);
                return false;
            }
        }

        public static bool Call(uint command, uint commandType, byte[] payload, int outputSize, out byte[] result)
        {
            result = null;
            if (biosInstance == null) return false;
            lock (callLock)
            {
                try
                {
                    byte[] data = new byte[Math.Max(MinimumPayloadBytes, payload == null ? 0 : payload.Length)];
                    if (payload != null) Array.Copy(payload, data, payload.Length);

                    ManagementBaseObject input = inputClass.CreateInstance();
                    input["Sign"] = Signature;
                    input["Command"] = command;
                    input["CommandType"] = commandType;
                    input["Size"] = (uint)data.Length;
                    input["hpqBData"] = data;

                    string methodName = MethodForOutputSize(outputSize);
                    ManagementBaseObject arguments = biosInstance.GetMethodParameters(methodName);
                    arguments["InData"] = input;
                    ManagementBaseObject response = biosInstance.InvokeMethod(methodName, arguments, null);
                    var output = (ManagementBaseObject)response["OutData"];

                    result = (byte[])output["Data"];
                    uint returnCode = Convert.ToUInt32(output["rwReturnCode"]);
                    if (returnCode != 0)
                        AppLog.Write(string.Format("BIOS call cmd=0x{0:X} type=0x{1:X2} rc={2}", command, commandType, returnCode));
                    return returnCode == 0;
                }
                catch (Exception error)
                {
                    LastError = error.Message;
                    AppLog.Write(string.Format("BIOS call cmd=0x{0:X} type=0x{1:X2} EXC {2}", command, commandType, error.Message));
                    return false;
                }
            }
        }

        private static string MethodForOutputSize(int outputSize)
        {
            if (outputSize <= 4) return "hpqBIOSInt4";
            if (outputSize <= 128) return "hpqBIOSInt128";
            if (outputSize <= 1024) return "hpqBIOSInt1024";
            return "hpqBIOSInt4096";
        }
    }
}
