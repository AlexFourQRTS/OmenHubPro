using System;
using System.IO;
using System.Text;
using System.Threading;
using OmenHub.Core.Processes;
using OmenHub.Hardware.Bios;

// Restores the GPU power mode and watches how the driver's enforced limit reacts over time.
class GpuModeProbe
{
    const uint Default = 0x20008;
    static StringBuilder report = new StringBuilder();

    static string Limits()
    {
        return ProcessRunner.Run("nvidia-smi", "--query-gpu=power.draw,enforced.power.limit --format=csv,noheader", 5000);
    }

    static string ReadMode()
    {
        byte[] data;
        return BiosInterface.Call(Default, 0x21, null, 4, out data) ? BitConverter.ToString(data) : "read failed";
    }

    static bool Write(byte customTgp, byte ppab, byte dState, byte temp)
    {
        byte[] ignored;
        return BiosInterface.Call(Default, 0x22, new byte[] { customTgp, ppab, dState, temp }, 4, out ignored);
    }

    static void Watch(string label, int seconds)
    {
        for (int step = 0; step < seconds / 2; step++)
        {
            Thread.Sleep(2000);
            report.AppendLine("  " + label + " +" + (step + 1) * 2 + "s  mode=" + ReadMode() + "  driver: " + Limits());
        }
    }

    static void Main(string[] arguments)
    {
        BiosInterface.Initialize();
        report.AppendLine("now: mode=" + ReadMode() + "  driver: " + Limits());

        report.AppendLine("A) write 01 01 01 00 again");
        Write(1, 1, 1, 0); Watch("A", 10);

        report.AppendLine("B) write 00 00 01 00 then 01 01 01 00 (full cycle)");
        Write(0, 0, 1, 0); Thread.Sleep(2000); Write(1, 1, 1, 0); Watch("B", 10);

        report.AppendLine("C) write 01 01 02 00 (dState 2)");
        Write(1, 1, 2, 0); Watch("C", 6);
        report.AppendLine("D) back to 01 01 01 00");
        Write(1, 1, 1, 0); Watch("D", 6);
        File.WriteAllText(arguments[0], report.ToString(), Encoding.UTF8);
    }
}
