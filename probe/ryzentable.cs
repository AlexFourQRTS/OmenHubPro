using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

// Reads every libryzenadj getter and the raw PM table twice, to find the CPU temperature and power on this APU.
class RyzenTableProbe
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr LoadLibraryEx(string path, IntPtr file, uint flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    static extern IntPtr GetProcAddress(IntPtr module, string name);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Init();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int WithHandle(IntPtr handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate float FloatGetter(IntPtr handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr PointerGetter(IntPtr handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate uint UIntGetter(IntPtr handle);

    static IntPtr module;
    static T Get<T>(string name) where T : class
    {
        IntPtr address = GetProcAddress(module, name);
        return address == IntPtr.Zero ? null : (T)(object)Marshal.GetDelegateForFunctionPointer(address, typeof(T));
    }

    static void Main(string[] arguments)
    {
        var report = new StringBuilder();
        string progressPath = arguments[1] + ".progress";
        Action<string> step = delegate (string text) { File.AppendAllText(progressPath, DateTime.Now.ToString("HH:mm:ss.fff") + " " + text + "\r\n"); };
        step("start");
        module = LoadLibraryEx(arguments[0], IntPtr.Zero, 0x8);   // LOAD_WITH_ALTERED_SEARCH_PATH: find WinRing0x64.dll next to it
        if (module == IntPtr.Zero) { File.WriteAllText(arguments[1], "cannot load library, error " + Marshal.GetLastWin32Error()); return; }
        step("library loaded");
        IntPtr handle = Get<Init>("init_ryzenadj")();
        step("init_ryzenadj -> " + handle);
        report.AppendLine("handle=" + handle + "  init_table=" + Get<WithHandle>("init_table")(handle));
        report.AppendLine("table ver=0x" + Get<UIntGetter>("get_table_ver")(handle).ToString("X") + " size=" + Get<UIntGetter>("get_table_size")(handle));

        string[] getters = { "get_stapm_limit", "get_stapm_value", "get_fast_limit", "get_fast_value", "get_slow_limit", "get_slow_value",
            "get_tctl_temp", "get_tctl_temp_value", "get_socket_power", "get_soc_power", "get_core_power", "get_core_temp", "get_gfx_temp",
            "get_apu_skin_temp_value", "get_vrm_current_value", "get_cclk_busy_value" };
        for (int sample = 1; sample <= 2; sample++)
        {
            step("refresh_table " + sample);
            Get<WithHandle>("refresh_table")(handle);
            report.AppendLine("=== sample " + sample + " " + DateTime.Now.ToString("HH:mm:ss"));
            foreach (string name in getters)
            {
                var getter = Get<FloatGetter>(name);
                report.AppendLine("  " + name + " = " + (getter == null ? "(missing)" : getter(handle).ToString("0.###")));
            }
            uint bytes = Get<UIntGetter>("get_table_size")(handle);
            IntPtr values = Get<PointerGetter>("get_table_values")(handle);
            var line = new StringBuilder("  raw floats:");
            for (int index = 0; index < bytes / 4; index++)
            {
                float value = BitConverter.ToSingle(BitConverter.GetBytes(Marshal.ReadInt32(values, index * 4)), 0);
                if (!float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) > 0.01 && Math.Abs(value) < 100000)
                    line.Append(" [" + index + "]=" + value.ToString("0.###"));
            }
            report.AppendLine(line.ToString());
            Thread.Sleep(3000);
        }
        File.WriteAllText(arguments[1], report.ToString(), Encoding.UTF8);
    }
}
