using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Text;
using System.Threading;

namespace OmenHub
{
    // ---------------------------------------------------------------- settings
    static class Cfg
    {
        public static bool Dark = true;
        public static int FanMode = 0;            // 0 auto, 1 max, 2 manual, 3 curve
        public static int ManCpu = 2400, ManGpu = 2400;
        public static int[] TCpu = { 40, 50, 60, 70, 78 };
        public static int[] RCpu = { 1800, 2200, 3000, 4000, 5500 };
        public static int[] TGpu = { 40, 50, 60, 70, 78 };
        public static int[] RGpu = { 1800, 2200, 3000, 4000, 5500 };
        public static int SrcCpu = 0, SrcGpu = 1; // 0 system, 1 gpu, 2 max of all
        public static int CpuW = 45;
        public const int ProAutoW = 40;
        public static bool ProAuto = false;          // "Pro auto" profile: CPU held at ProAutoW

        public static string Dir
        {
            get
            {
                string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OmenHub");
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                return d;
            }
        }
        static string FilePath { get { return Path.Combine(Dir, "settings.ini"); } }

        static string J(int[] a) { string[] s = new string[a.Length]; for (int i = 0; i < a.Length; i++) s[i] = a[i].ToString(); return string.Join(",", s); }
        static int[] Parse(string v)
        {
            if (v == null) return null;
            string[] s = v.Split(',');
            if (s.Length < 3 || s.Length > 12) return null;
            int[] t = new int[s.Length];
            for (int i = 0; i < s.Length; i++) { if (!int.TryParse(s[i], out t[i])) return null; }
            return t;
        }
        static void P(string v, int[] dst)
        {
            if (v == null) return;
            string[] s = v.Split(',');
            if (s.Length != dst.Length) return;
            int[] t = new int[s.Length];
            for (int i = 0; i < s.Length; i++) { if (!int.TryParse(s[i], out t[i])) return; }
            Array.Copy(t, dst, t.Length);
        }

        public static void Save()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Dark=" + (Dark ? 1 : 0));
                sb.AppendLine("FanMode=" + FanMode);
                sb.AppendLine("ManCpu=" + ManCpu); sb.AppendLine("ManGpu=" + ManGpu);
                sb.AppendLine("CurveV=2");
                sb.AppendLine("TCpu=" + J(TCpu)); sb.AppendLine("RCpu=" + J(RCpu));
                sb.AppendLine("TGpu=" + J(TGpu)); sb.AppendLine("RGpu=" + J(RGpu));
                sb.AppendLine("SrcCpu=" + SrcCpu); sb.AppendLine("SrcGpu=" + SrcGpu);
                sb.AppendLine("CpuW=" + CpuW); sb.AppendLine("ProAuto=" + (ProAuto ? 1 : 0));
                File.WriteAllText(FilePath, sb.ToString());
            }
            catch { }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                var d = new Dictionary<string, string>();
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    int i = line.IndexOf('=');
                    if (i > 0) d[line.Substring(0, i)] = line.Substring(i + 1);
                }
                Dark = G(d, "Dark", 1) == 1;
                FanMode = G(d, "FanMode", 0);
                ManCpu = G(d, "ManCpu", ManCpu); ManGpu = G(d, "ManGpu", ManGpu);
                string v;
                if (G(d, "CurveV", 1) >= 2)   // v1 curves had unsafe near-zero defaults: ignore them
                {
                    int[] a, b;
                    d.TryGetValue("TCpu", out v); a = Parse(v); d.TryGetValue("RCpu", out v); b = Parse(v);
                    if (a != null && b != null && a.Length == b.Length) { TCpu = a; RCpu = b; }
                    d.TryGetValue("TGpu", out v); a = Parse(v); d.TryGetValue("RGpu", out v); b = Parse(v);
                    if (a != null && b != null && a.Length == b.Length) { TGpu = a; RGpu = b; }
                }
                SrcCpu = G(d, "SrcCpu", SrcCpu); SrcGpu = G(d, "SrcGpu", SrcGpu);
                CpuW = G(d, "CpuW", CpuW); ProAuto = G(d, "ProAuto", 0) == 1;
            }
            catch { }
        }
        static int G(Dictionary<string, string> d, string k, int def)
        {
            string v; int r;
            return d.TryGetValue(k, out v) && int.TryParse(v, out r) ? r : def;
        }
    }

    // ---------------------------------------------------------------- HP BIOS WMI
    static class Bios
    {
        static ManagementObject inst;
        static ManagementClass inCls;
        static readonly object lk = new object();
        public static string Error = "";
        const uint D = 0x20008;

        public static bool Init()
        {
            try
            {
                var c = new ManagementClass(@"root\wmi:hpqBIntM");
                foreach (ManagementObject o in c.GetInstances()) { inst = o; break; }
                inCls = new ManagementClass(@"root\wmi:hpqBDataIn");
                if (inst == null) { Error = "hpqBIntM не найден"; Log.W("BIOS init: hpqBIntM not found"); return false; }
                Log.W("BIOS init OK: " + inst["InstanceName"]);
                return true;
            }
            catch (Exception e) { Error = e.Message; Log.W("BIOS init FAILED: " + e.Message); return false; }
        }

        static bool Call(uint cmd, uint type, byte[] payload, int outSize, out byte[] res)
        {
            res = null;
            if (inst == null) return false;
            lock (lk)
            {
                try
                {
                    byte[] data = new byte[Math.Max(4, payload == null ? 0 : payload.Length)];
                    if (payload != null) Array.Copy(payload, data, payload.Length);
                    string m = outSize <= 4 ? "hpqBIOSInt4" : outSize <= 128 ? "hpqBIOSInt128" : outSize <= 1024 ? "hpqBIOSInt1024" : "hpqBIOSInt4096";
                    var inObj = inCls.CreateInstance();
                    inObj["Sign"] = new byte[] { 0x53, 0x45, 0x43, 0x55 };
                    inObj["Command"] = cmd; inObj["CommandType"] = type;
                    inObj["Size"] = (uint)data.Length; inObj["hpqBData"] = data;
                    var args = inst.GetMethodParameters(m);
                    args["InData"] = inObj;
                    var r = inst.InvokeMethod(m, args, null);
                    var o = (ManagementBaseObject)r["OutData"];
                    res = (byte[])o["Data"];
                    uint rc = Convert.ToUInt32(o["rwReturnCode"]);
                    if (rc != 0) Log.W(string.Format("BIOS call cmd=0x{0:X} type=0x{1:X2} rc={2}", cmd, type, rc));
                    return rc == 0;
                }
                catch (Exception e) { Error = e.Message; Log.W(string.Format("BIOS call cmd=0x{0:X} type=0x{1:X2} EXC {2}", cmd, type, e.Message)); return false; }
            }
        }

        // real fan speeds in RPM
        public static bool GetFanRpm(out int cpu, out int gpu)
        {
            cpu = gpu = -1; byte[] d;
            if (!Call(D, 0x2D, null, 128, out d)) return false;
            cpu = d[0] * 100; gpu = d[1] * 100; return true;
        }
        public static bool SetFanLevel(int cpuRpm, int gpuRpm)
        {
            byte[] d;
            return Call(D, 0x2E, new byte[] { (byte)Math.Max(0, Math.Min(60, cpuRpm / 100)), (byte)Math.Max(0, Math.Min(60, gpuRpm / 100)), 0, 0 }, 4, out d);
        }
        // FanMax=false also hands fan control back to the BIOS
        public static bool SetFanMax(bool on)
        {
            byte[] d;
            return Call(D, 0x27, new byte[] { (byte)(on ? 1 : 0), 0, 0, 0 }, 4, out d);
        }
        public static int GetTemp()
        {
            byte[] d;
            return Call(D, 0x23, null, 4, out d) ? d[0] : -1;
        }
        public static string GetGpuPower()
        {
            byte[] d;
            if (!Call(D, 0x21, null, 4, out d)) return "н/д";
            return string.Format("Custom TGP: {0}   Dynamic Boost: {1}", d[0] != 0 ? "вкл" : "выкл", d[1] != 0 ? "вкл" : "выкл");
        }
    }

    // ---------------------------------------------------------------- helpers
    static class Log
    {
        static readonly object lk = new object();
        public static string Path_
        {
            get
            {
                string d = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                try { if (!Directory.Exists(d)) Directory.CreateDirectory(d); } catch { }
                return Path.Combine(d, "omenhub.log");
            }
        }
        static string last = ""; static DateTime lastAt = DateTime.MinValue;
        public static void W(string msg)
        {
            try
            {
                lock (lk)
                {
                    if (msg == last && (DateTime.Now - lastAt).TotalSeconds < 60) return;   // de-dup repeated errors
                    last = msg; lastAt = DateTime.Now;
                    string f = Path_;
                    if (File.Exists(f) && new FileInfo(f).Length > 1000000) File.Delete(f);
                    File.AppendAllText(f, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + msg + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }
        }
    }

    static class Proc
    {
        public static string Run(string file, string args, int timeoutMs)
        {
            int code;
            return RunCode(file, args, timeoutMs, out code);
        }

        public static string RunCode(string file, string args, int timeoutMs, out int code)
        {
            code = -1;
            try
            {
                var psi = new ProcessStartInfo(file, args);
                psi.UseShellExecute = false; psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true; psi.RedirectStandardError = true;
                psi.StandardOutputEncoding = Encoding.GetEncoding(866);
                using (var p = Process.Start(psi))
                {
                    string o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                    p.WaitForExit(timeoutMs);
                    code = p.HasExited ? p.ExitCode : -2;
                    return o.Trim();
                }
            }
            catch (Exception e) { return "Ошибка запуска " + file + ": " + e.Message; }
        }
    }

    class Snap
    {
        public int FanCpu = -1, FanGpu = -1, BiosTemp = -1, GpuTemp = -1, GpuClock = -1, GpuUtil = -1, CpuMhz = -1;
        public double GpuPower = -1, CpuLoad = -1;
        public string[] ZoneNames = new string[0];
        public double[] Zones = new double[0];
        public int SysTemp = -1;
        public string Status = "";
        public bool Alert;
    }



    // ---------------------------------------------------------------- autostart + priority
    static class Autostart
    {
        const string TaskName = "OmenHub";

        public static bool IsOn()
        {
            int code; Proc.RunCode("schtasks", "/Query /TN " + TaskName, 8000, out code);
            return code == 0;
        }

        static string Esc(string s) { return System.Security.SecurityElement.Escape(s); }

        public static string Enable()
        {
            try
            {
                string user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                string exe = System.Windows.Forms.Application.ExecutablePath;
                string xml =
"<?xml version=\"1.0\" encoding=\"UTF-16\"?>\r\n" +
"<Task version=\"1.4\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">\r\n" +
"  <RegistrationInfo><Description>OmenHub autostart (elevated, no UAC prompt)</Description></RegistrationInfo>\r\n" +
"  <Triggers><LogonTrigger><Enabled>true</Enabled><UserId>" + Esc(user) + "</UserId><Delay>PT10S</Delay></LogonTrigger></Triggers>\r\n" +
"  <Principals><Principal id=\"Author\"><UserId>" + Esc(user) + "</UserId><LogonType>InteractiveToken</LogonType><RunLevel>HighestAvailable</RunLevel></Principal></Principals>\r\n" +
"  <Settings>\r\n" +
"    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>\r\n" +
"    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>\r\n" +
"    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>\r\n" +
"    <AllowHardTerminate>false</AllowHardTerminate>\r\n" +
"    <StartWhenAvailable>true</StartWhenAvailable>\r\n" +
"    <AllowStartOnDemand>true</AllowStartOnDemand>\r\n" +
"    <Enabled>true</Enabled>\r\n" +
"    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>\r\n" +
"    <Priority>1</Priority>\r\n" +
"  </Settings>\r\n" +
"  <Actions Context=\"Author\"><Exec><Command>" + Esc(exe) + "</Command><Arguments>/autostart</Arguments><WorkingDirectory>" + Esc(Path.GetDirectoryName(exe)) + "</WorkingDirectory></Exec></Actions>\r\n" +
"</Task>\r\n";
                string tmp = Path.Combine(Path.GetTempPath(), "omenhub-task.xml");
                File.WriteAllText(tmp, xml, Encoding.Unicode);
                int code; string o = Proc.RunCode("schtasks", "/Create /TN " + TaskName + " /XML \"" + tmp + "\" /F", 15000, out code);
                try { File.Delete(tmp); } catch { }
                Log.W("autostart enable -> exit=" + code + " " + o);
                return code == 0 ? "" : o;
            }
            catch (Exception e) { Log.W("autostart enable EXC " + e.Message); return e.Message; }
        }

        public static string Disable()
        {
            int code; string o = Proc.RunCode("schtasks", "/Delete /TN " + TaskName + " /F", 10000, out code);
            Log.W("autostart disable -> exit=" + code + " " + o);
            return code == 0 ? "" : o;
        }
    }

    static class Prio
    {
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct PowerThrottling { public uint Version; public uint ControlMask; public uint StateMask; }
        [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern bool SetProcessInformation(IntPtr h, int cls, ref PowerThrottling info, int size);
        [System.Runtime.InteropServices.DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint ms);

        // High priority (not Realtime: Realtime can starve the input/system threads and freeze the machine)
        public static void Apply()
        {
            try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High; } catch (Exception e) { Log.W("priority EXC " + e.Message); }
            try
            {
                var pt = new PowerThrottling { Version = 1, ControlMask = 1, StateMask = 0 };   // EXECUTION_SPEED throttling OFF
                SetProcessInformation(Process.GetCurrentProcess().Handle, 4, ref pt, System.Runtime.InteropServices.Marshal.SizeOf(pt));
            }
            catch { }
            try { timeBeginPeriod(1); } catch { }
            Log.W("priority: " + Process.GetCurrentProcess().PriorityClass + ", power throttling off");
        }
    }
    // ---------------------------------------------------------------- RyzenAdj wrapper
    static class RyzenAdj
    {
        public static string Exe { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"tools\ryzenadj\ryzenadj.exe"); } }
        public static bool Present { get { return File.Exists(Exe); } }

        public static string Run(string args, out int code)
        {
            code = -1;
            if (!Present) { Log.W("ryzenadj missing: " + Exe); return "Не найден " + Exe; }
            string r = Proc.RunCode(Exe, args, 20000, out code);
            Log.W("ryzenadj " + args + "  -> exit=" + code + (r.Length > 400 ? "\r\n" + r.Substring(0, 400) + "..." : "\r\n" + r));
            return r;
        }
        public static string Set(int watts)
        {
            int mw = watts * 1000, code;
            return Run("--stapm-limit=" + mw + " --fast-limit=" + mw + " --slow-limit=" + mw, out code);
        }
        // current STAPM limit in watts, NaN if unknown
        public static double ReadLimit()
        {
            int code; string info = Run("-i", out code);
            foreach (string line in info.Split('\n'))
                if (line.IndexOf("| STAPM LIMIT", StringComparison.Ordinal) >= 0)
                {
                    string[] p = line.Split('|'); double v;
                    if (p.Length > 2 && double.TryParse(p[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
                }
            return double.NaN;
        }
    }
    // ---------------------------------------------------------------- engine
    static class Engine
    {
        public static volatile Snap S = new Snap();
        static Thread th; static volatile bool run;
        static int applied = -99; static int lastC = -1, lastG = -1; static DateTime lastSet = DateTime.MinValue;
        static bool emergency; static bool biosOk;
        static PerformanceCounter pcLoad, pcPerf; static double baseMhz = 0;
        static int tick; static string[] gpuCache = null;
        static ManagementObjectSearcher zoneSearch;

        public static void Start()
        {
            Log.W("=== OmenHub start, " + Environment.OSVersion + ", " + (Environment.Is64BitProcess ? "x64" : "x86") + " ===");
            biosOk = Bios.Init();
            try { pcLoad = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total"); pcLoad.NextValue(); } catch { pcLoad = null; }
            try { pcPerf = new PerformanceCounter("Processor Information", "% Processor Performance", "_Total"); pcPerf.NextValue(); } catch { pcPerf = null; }
            try
            {
                foreach (ManagementObject o in new ManagementObjectSearcher("select MaxClockSpeed from Win32_Processor").Get())
                { baseMhz = Convert.ToDouble(o["MaxClockSpeed"]); break; }
            }
            catch { }
            try { zoneSearch = new ManagementObjectSearcher(@"root\cimv2", "select Name,Temperature from Win32_PerfFormattedData_Counters_ThermalZoneInformation"); } catch { }
            run = true;
            th = new Thread(Loop); th.IsBackground = true; th.Priority = ThreadPriority.Highest; th.Start();
        }

        public static void Stop()
        {
            run = false;
            try { if (th != null) th.Join(3000); } catch { }
            if (biosOk) Bios.SetFanMax(false);
        }

        static DateTime lastPro = DateTime.MinValue; static bool proActive;
        // "Pro auto": hold the CPU at Cfg.ProAutoW; verifies every 30 s and re-applies if something reset the limit
        static void ProAutoTick()
        {
            if (!Cfg.ProAuto) { proActive = false; return; }
            if (!RyzenAdj.Present) return;
            bool fresh = !proActive;
            if (!fresh && (DateTime.Now - lastPro).TotalSeconds < 30) return;
            proActive = true; lastPro = DateTime.Now;
            double cur = fresh ? double.NaN : RyzenAdj.ReadLimit();
            if (double.IsNaN(cur) || Math.Abs(cur - Cfg.ProAutoW) > 0.5)
            {
                RyzenAdj.Set(Cfg.ProAutoW);
                Log.W("ProAuto: set " + Cfg.ProAutoW + " W (was " + cur + ")");
            }
        }

        static void Loop()
        {
            while (run)
            {
                var s = new Snap();
                try { Poll(s); Control(s); ProAutoTick(); } catch (Exception e) { s.Status = "Ошибка: " + e.Message; }
                S = s; tick++;
                for (int i = 0; i < 15 && run; i++) Thread.Sleep(100);
            }
        }

        static void Poll(Snap s)
        {
            if (biosOk)
            {
                int c, g;
                if (Bios.GetFanRpm(out c, out g)) { s.FanCpu = c; s.FanGpu = g; }
                s.BiosTemp = Bios.GetTemp();
                s.Status = "BIOS: подключён";
            }
            else s.Status = "BIOS недоступен: " + Bios.Error;

            try
            {
                var names = new List<string>(); var vals = new List<double>();
                if (zoneSearch != null)
                    foreach (ManagementObject o in zoneSearch.Get())
                    {
                        double k = Convert.ToDouble(o["Temperature"]);
                        string n = Convert.ToString(o["Name"]); n = n.Substring(n.LastIndexOf('.') + 1);
                        names.Add(n); vals.Add(k - 273.15);
                    }
                s.ZoneNames = names.ToArray(); s.Zones = vals.ToArray();
            }
            catch { }
            double mx = s.BiosTemp;
            foreach (double v in s.Zones) if (v > mx) mx = v;
            s.SysTemp = (int)Math.Round(mx);

            if (pcLoad != null) { try { s.CpuLoad = Math.Min(100, pcLoad.NextValue()); } catch { } }
            if (pcPerf != null && baseMhz > 0) { try { s.CpuMhz = (int)(pcPerf.NextValue() / 100.0 * baseMhz); } catch { } }

            if (tick % 2 == 0 || gpuCache == null)
            {
                string o = Proc.Run("nvidia-smi", "--query-gpu=temperature.gpu,power.draw,clocks.gr,utilization.gpu --format=csv,noheader,nounits", 4000);
                string[] p = o.Split(',');
                gpuCache = p.Length >= 4 ? p : null;
            }
            if (gpuCache != null)
            {
                int i; double d;
                if (int.TryParse(gpuCache[0].Trim(), out i)) s.GpuTemp = i;
                if (double.TryParse(gpuCache[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out d)) s.GpuPower = d;
                if (int.TryParse(gpuCache[2].Trim(), out i)) s.GpuClock = i;
                if (int.TryParse(gpuCache[3].Trim(), out i)) s.GpuUtil = i;
            }
        }

        static int Eval(int[] tt, int[] rr, double x)
        {
            int[] t = tt, r = rr; int n = Math.Min(t.Length, r.Length);
            if (n == 0) return 0;
            if (x <= t[0]) return r[0];
            for (int i = 1; i < n; i++)
                if (x <= t[i])
                {
                    double f = (x - t[i - 1]) / Math.Max(1, t[i] - t[i - 1]);
                    return (int)(r[i - 1] + f * (r[i] - r[i - 1]));
                }
            return r[n - 1];
        }
        static double Src(int src, Snap s)
        {
            double sys = s.SysTemp, gpu = s.GpuTemp;
            return src == 0 ? sys : src == 1 ? (gpu >= 0 ? gpu : sys) : Math.Max(sys, gpu);
        }

        public const double AlertOn = 80, AlertOff = 74;
        static DateTime emergencySince = DateTime.MinValue;
        static int smC = -1, smG = -1;
        static int Ramp(int cur, int tgt) { return tgt > cur ? Math.Min(tgt, cur + 800) : Math.Max(tgt, cur - 200); }

        static void Control(Snap s)
        {
            if (!biosOk) return;
            int mode = Cfg.FanMode;
            double hot = Math.Max(s.SysTemp, s.GpuTemp);

            // Hard thermal alert: ignores every user setting, both fans to max.
            if (!emergency && hot >= AlertOn)
            {
                emergency = true; emergencySince = DateTime.Now; lastSet = DateTime.MinValue;
                Log.W("ALERT: " + (int)hot + "C (sys " + s.SysTemp + ", gpu " + s.GpuTemp + ") -> both fans MAX");
            }
            if (emergency)
            {
                if (hot <= AlertOff && (DateTime.Now - emergencySince).TotalSeconds >= 20)
                {
                    emergency = false; applied = -99; smC = smG = -1;
                    Log.W("ALERT cleared at " + (int)hot + "C");
                }
                else
                {
                    if ((DateTime.Now - lastSet).TotalSeconds > 8) { Bios.SetFanMax(true); lastSet = DateTime.Now; }
                    s.Alert = true; s.Status = "АВАРИЙНЫЙ ОБДУВ: " + (int)hot + " °C — оба кулера на максимум";
                    return;
                }
            }

            bool force = false;
            if (mode != applied)
            {
                if (mode == 1) Bios.SetFanMax(true); else Bios.SetFanMax(false);
                applied = mode; force = true; lastC = lastG = -1; smC = smG = -1;
                Log.W("fan mode -> " + mode);
            }
            if (mode == 1) { if ((DateTime.Now - lastSet).TotalSeconds > 20) { Bios.SetFanMax(true); lastSet = DateTime.Now; } return; }
            if (mode == 0) return;

            int c, g;
            if (mode == 2) { c = Cfg.ManCpu; g = Cfg.ManGpu; }
            else
            {
                c = Eval(Cfg.TCpu, Cfg.RCpu, Src(Cfg.SrcCpu, s)); g = Eval(Cfg.TGpu, Cfg.RGpu, Src(Cfg.SrcGpu, s));
                if (smC < 0) { smC = c; smG = g; } else { smC = Ramp(smC, c); smG = Ramp(smG, g); }
                c = smC; g = smG;
            }
            int floor = hot >= 75 ? 3000 : hot >= 70 ? 2200 : 0;   // extra safety floor below the alert
            if (c < floor) c = floor; if (g < floor) g = floor;

            if (force || c != lastC || g != lastG || (DateTime.Now - lastSet).TotalSeconds > 10)
            {
                bool okSet = Bios.SetFanLevel(c, g);
                if (!okSet || force) Log.W("SetFanLevel " + c + "/" + g + " ok=" + okSet);
                lastC = c; lastG = g; lastSet = DateTime.Now;
            }
        }
    }
}
