using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

// Listens for HP BIOS WMI events AND low-level key presses for N seconds; logs everything.
// Usage: keywatch.exe <seconds> <out.txt>
class KeyWatch
{
    static StringBuilder log = new StringBuilder();
    static readonly object lk = new object();
    static void P(string s) { lock (lk) { log.AppendLine(DateTime.Now.ToString("HH:mm:ss.fff") + "  " + s); } }

    delegate IntPtr HookProc(int code, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int id, HookProc cb, IntPtr mod, uint tid);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr h, int code, IntPtr w, IntPtr l);
    [DllImport("kernel32.dll")] static extern IntPtr GetModuleHandle(string n);
    static HookProc hp;

    static IntPtr Hook(int code, IntPtr w, IntPtr l)
    {
        if (code >= 0)
        {
            int vk = Marshal.ReadInt32(l, 0), sc = Marshal.ReadInt32(l, 4), fl = Marshal.ReadInt32(l, 8);
            P(string.Format("KEY {0} vk=0x{1:X2} scan=0x{2:X2} flags=0x{3:X2}", (int)w == 0x100 || (int)w == 0x104 ? "down" : "up", vk, sc, fl));
        }
        return CallNextHookEx(IntPtr.Zero, code, w, l);
    }

    static void Watch(string wql, string name)
    {
        try
        {
            var w = new ManagementEventWatcher(new ManagementScope(@"root\wmi"), new WqlEventQuery(wql));
            w.EventArrived += delegate (object s, EventArrivedEventArgs e)
            {
                var sb = new StringBuilder("WMI " + name + ":");
                foreach (PropertyData pd in e.NewEvent.Properties)
                {
                    string v = pd.Value == null ? "null" : pd.Value is Array ? string.Join(",", Array.ConvertAll((object[])ToObjArray(pd.Value), x => x.ToString())) : pd.Value.ToString();
                    sb.Append(" " + pd.Name + "=" + v);
                }
                P(sb.ToString());
            };
            w.Start(); P("watching " + name);
        }
        catch (Exception e) { P("cannot watch " + name + ": " + e.Message); }
    }
    static object[] ToObjArray(object a) { var l = new List<object>(); foreach (object o in (Array)a) l.Add(o); return l.ToArray(); }

    [STAThread]
    static void Main(string[] a)
    {
        int secs = int.Parse(a[0]);
        Watch("SELECT * FROM hpqBEvnt", "hpqBEvnt");
        Watch("SELECT * FROM HP_BIOSEvent", "HP_BIOSEvent");
        Watch("SELECT * FROM HPBIOS_BIOSEvent", "HPBIOS_BIOSEvent");
        hp = Hook;
        SetWindowsHookEx(13, hp, GetModuleHandle(null), 0);
        P("READY - press the OMEN key now");
        File.WriteAllText(a[1] + ".ready", "1");
        var end = DateTime.Now.AddSeconds(secs);
        while (DateTime.Now < end) { Application.DoEvents(); Thread.Sleep(20); }
        lock (lk) File.WriteAllText(a[1], log.ToString(), Encoding.UTF8);
    }
}
