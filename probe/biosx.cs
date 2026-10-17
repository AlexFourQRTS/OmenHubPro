using System;
using System.IO;
using System.Management;
using System.Text;

// Generic BIOS WMI runner. Script lines: "cmdHex typeHex outSize [inHex bytes...]"  (# = comment)
class BiosX
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

    static void Main(string[] a)
    {
        try
        {
            var c = new ManagementClass(@"root\wmi:hpqBIntM");
            ManagementObject inst = null;
            foreach (ManagementObject o in c.GetInstances()) { inst = o; break; }
            var inCls = new ManagementClass(@"root\wmi:hpqBDataIn");
            foreach (var raw in File.ReadAllLines(a[0]))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#")) { P(line); continue; }
                if (line.StartsWith("sleep ")) { System.Threading.Thread.Sleep(int.Parse(line.Substring(6))); continue; }
                var t = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                uint cmd = Convert.ToUInt32(t[0], 16), type = Convert.ToUInt32(t[1], 16);
                int outSize = int.Parse(t[2]);
                byte[] data = new byte[Math.Max(4, t.Length - 3)];
                int n = t.Length - 3;
                for (int i = 0; i < n; i++) data[i] = Convert.ToByte(t[3 + i], 16);
                string method = outSize == 0 ? "hpqBIOSInt0" : outSize <= 4 ? "hpqBIOSInt4" : outSize <= 128 ? "hpqBIOSInt128" : outSize <= 1024 ? "hpqBIOSInt1024" : "hpqBIOSInt4096";
                try
                {
                    var inObj = inCls.CreateInstance();
                    inObj["Sign"] = new byte[] { 0x53, 0x45, 0x43, 0x55 };
                    inObj["Command"] = cmd; inObj["CommandType"] = type;
                    inObj["Size"] = (uint)n; inObj["hpqBData"] = data;
                    var args = inst.GetMethodParameters(method);
                    args["InData"] = inObj;
                    var res = inst.InvokeMethod(method, args, null);
                    var o = (ManagementBaseObject)res["OutData"];
                    P(string.Format("{0}  -> rc={1} data={2}", line, o["rwReturnCode"], method == "hpqBIOSInt0" ? "" : Hex((byte[])o["Data"], 40)));
                }
                catch (Exception e) { P(line + "  -> EXC " + e.Message); }
            }
        }
        catch (Exception e) { P("FATAL: " + e); }
        File.WriteAllText(a.Length > 1 ? a[1] : "biosx-result.txt", log.ToString(), Encoding.UTF8);
    }
}
