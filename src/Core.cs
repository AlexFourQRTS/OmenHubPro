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
        public static int[] TCpu = { 40, 50, 60, 70, 80, 90 };
        public static int[] RCpu = { 0, 1800, 2400, 3200, 4000, 4400 };
        public static int[] TGpu = { 40, 50, 60, 70, 80, 90 };
        public static int[] RGpu = { 0, 1800, 2400, 3200, 4000, 4400 };
        public static int SrcCpu = 0, SrcGpu = 1; // 0 system, 1 gpu, 2 max of all
        public static int CpuW = 45;

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
                sb.AppendLine("TCpu=" + J(TCpu)); sb.AppendLine("RCpu=" + J(RCpu));
                sb.AppendLine("TGpu=" + J(TGpu)); sb.AppendLine("RGpu=" + J(RGpu));
                sb.AppendLine("SrcCpu=" + SrcCpu); sb.AppendLine("SrcGpu=" + SrcGpu);
                sb.AppendLine("CpuW=" + CpuW);
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
                if (d.TryGetValue("TCpu", out v)) P(v, TCpu);
                if (d.TryGetValue("RCpu", out v)) P(v, RCpu);
                if (d.TryGetValue("TGpu", out v)) P(v, TGpu);
                if (d.TryGetValue("RGpu", out v)) P(v, RGpu);
                SrcCpu = G(d, "SrcCpu", SrcCpu); SrcGpu = G(d, "SrcGpu", SrcGpu);
                CpuW = G(d, "CpuW", CpuW);
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
                if (inst == null) { Error = "hpqBIntM не найден"; return false; }
                return true;
            }
            catch (Exception e) { Error = e.Message; return false; }
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
                    return Convert.ToUInt32(o["rwReturnCode"]) == 0;
                }
                catch (Exception e) { Error = e.Message; return false; }
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
            return Call(D, 0x2E, new byte[] { (byte)Math.Max(0, Math.Min(44, cpuRpm / 100)), (byte)Math.Max(0, Math.Min(44, gpuRpm / 100)), 0, 0 }, 4, out d);
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
    static class Proc
    {
        public static string Run(string file, string args, int timeoutMs)
        {
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
            th = new Thread(Loop); th.IsBackground = true; th.Start();
        }

        public static void Stop()
        {
            run = false;
            try { if (th != null) th.Join(3000); } catch { }
            if (biosOk) Bios.SetFanMax(false);
        }

        static void Loop()
        {
            while (run)
            {
                var s = new Snap();
                try { Poll(s); Control(s); } catch (Exception e) { s.Status = "Ошибка: " + e.Message; }
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

        static int Eval(int[] t, int[] r, double x)
        {
            if (x <= t[0]) return r[0];
            for (int i = 1; i < t.Length; i++)
                if (x <= t[i])
                {
                    double f = (x - t[i - 1]) / Math.Max(1, t[i] - t[i - 1]);
                    return (int)(r[i - 1] + f * (r[i] - r[i - 1]));
                }
            return r[r.Length - 1];
        }
        static double Src(int src, Snap s)
        {
            double sys = s.SysTemp, gpu = s.GpuTemp;
            return src == 0 ? sys : src == 1 ? (gpu >= 0 ? gpu : sys) : Math.Max(sys, gpu);
        }

        static void Control(Snap s)
        {
            if (!biosOk) return;
            int mode = Cfg.FanMode;
            double hot = Math.Max(s.SysTemp, s.GpuTemp);

            if (hot >= 95)   // failsafe: never let it cook
            {
                if (!emergency) { Bios.SetFanMax(true); emergency = true; s.Status = "АВАРИЙНЫЙ ОБДУВ: " + (int)hot + " °C"; }
                return;
            }
            if (emergency) { emergency = false; applied = -99; }

            bool force = false;
            if (mode != applied)
            {
                if (mode == 0) Bios.SetFanMax(false);
                else if (mode == 1) Bios.SetFanMax(true);
                else Bios.SetFanMax(false);
                applied = mode; force = true; lastC = lastG = -1;
            }
            if (mode == 1) { if ((DateTime.Now - lastSet).TotalSeconds > 20) { Bios.SetFanMax(true); lastSet = DateTime.Now; } return; }
            if (mode == 0) return;

            int c, g;
            if (mode == 2) { c = Cfg.ManCpu; g = Cfg.ManGpu; }
            else { c = Eval(Cfg.TCpu, Cfg.RCpu, Src(Cfg.SrcCpu, s)); g = Eval(Cfg.TGpu, Cfg.RGpu, Src(Cfg.SrcGpu, s)); }
            int floor = hot >= 85 ? 3600 : hot >= 75 ? 2600 : 0;   // safety floor
            if (c < floor) c = floor; if (g < floor) g = floor;

            if (force || c != lastC || g != lastG || (DateTime.Now - lastSet).TotalSeconds > 10)
            {
                Bios.SetFanLevel(c, g);
                lastC = c; lastG = g; lastSet = DateTime.Now;
            }
        }
    }
}
