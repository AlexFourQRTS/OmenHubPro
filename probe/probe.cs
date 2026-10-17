using System;
using System.IO;
using System.Management;
using System.Text;

// Read-only probe of the HP BIOS WMI interface (root\wmi, hpqBIntM).
class Probe
{
    static StringBuilder log = new StringBuilder();
    static void P(string s) { Console.WriteLine(s); log.AppendLine(s); }

    static string Hex(byte[] b, int max)
    {
        if (b == null) return "(null)";
        var sb = new StringBuilder();
        for (int i = 0; i < b.Length && i < max; i++) sb.Append(b[i].ToString("X2")).Append(' ');
        return sb.ToString().Trim();
    }

    static ManagementObject inst;
    static ManagementClass inCls;

    static bool Call(uint cmd, uint type, byte[] data, string method, out uint rc, out byte[] outData)
    {
        rc = 0xFFFFFFFF; outData = null;
        try
        {
            var inObj = inCls.CreateInstance();
            inObj["Sign"] = new byte[] { 0x53, 0x45, 0x43, 0x55 };
            inObj["Command"] = cmd;
            inObj["CommandType"] = type;
            inObj["Size"] = (uint)(data == null ? 0 : data.Length);
            inObj["hpqBData"] = data == null ? new byte[4] : data;
            var args = inst.GetMethodParameters(method);
            args["InData"] = inObj;
            var res = inst.InvokeMethod(method, args, null);
            var o = (ManagementBaseObject)res["OutData"];
            rc = Convert.ToUInt32(o["rwReturnCode"]);
            outData = (byte[])o["Data"];
            return true;
        }
        catch (Exception e) { P("   ! " + method + " exception: " + e.Message); return false; }
    }

    static void Try(string name, uint cmd, uint type, string method)
    {
        uint rc; byte[] d;
        if (Call(cmd, type, null, method, out rc, out d))
            P(string.Format("{0,-22} cmd=0x{1:X5} type=0x{2:X2} {3,-15} rc={4}  data={5}", name, cmd, type, method, rc, Hex(d, 32)));
    }

    static void Main(string[] a)
    {
        try
        {
            var scope = new ManagementScope(@"root\wmi");
            scope.Connect();
            var c = new ManagementClass(@"root\wmi:hpqBIntM");
            foreach (ManagementObject o in c.GetInstances()) { inst = o; break; }
            inCls = new ManagementClass(@"root\wmi:hpqBDataIn");
            P("Connected. Instance: " + inst["InstanceName"]);

            const uint D = 0x20008, K = 0x20009;
            P("== Fans / thermal (Default cmd 0x20008) ==");
            Try("FanCount",      D, 0x10, "hpqBIOSInt4");
            Try("FanType",       D, 0x2C, "hpqBIOSInt4");
            Try("FanLevel(krpm*100)", D, 0x2D, "hpqBIOSInt4");
            Try("FanMax(0/1)",   D, 0x26, "hpqBIOSInt4");
            Try("FanMode/SysData?", D, 0x28, "hpqBIOSInt128");
            Try("FanTable",      D, 0x2F, "hpqBIOSInt128");
            Try("Temp",          D, 0x23, "hpqBIOSInt4");
            Try("GpuPower",      D, 0x21, "hpqBIOSInt4");
            Try("GpuMode",       D, 0x52, "hpqBIOSInt4");
            Try("Thermal policy",D, 0x2B, "hpqBIOSInt4");
            P("== Keyboard (cmd 0x20009) ==");
            Try("KbdType",       K, 0x01, "hpqBIOSInt4");
            Try("ColorTable",    K, 0x02, "hpqBIOSInt128");
            Try("Backlight",     K, 0x04, "hpqBIOSInt4");
            P("== Other ==");
            Try("BIOS-feature 0x01", D, 0x01, "hpqBIOSInt128");
            Try("Cmd 0x20008/0x03",  D, 0x03, "hpqBIOSInt128");
        }
        catch (Exception e) { P("FATAL: " + e); }
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "probe-result.txt"), log.ToString(), Encoding.UTF8);
        P("\nSaved probe-result.txt.");
        if (a.Length == 0) Console.ReadLine();
    }
}


