using System;
using System.IO;
using System.Text;
using System.Threading;
using Windows.Foundation;
using Windows.Devices.Enumeration;
using Windows.Devices.Lights;
using Windows.UI;

// Strobe test: whole array yellow -> blue -> white every 0.5 s. Usage: lampset.exe <vhf|kbd> <seconds> <out.txt>
class LampSet
{
    static T Await<T>(IAsyncOperation<T> operation, int timeoutMilliseconds)
    {
        int waitedMilliseconds = 0;
        while (operation.Status == AsyncStatus.Started && waitedMilliseconds < timeoutMilliseconds) { Thread.Sleep(10); waitedMilliseconds += 10; }
        if (operation.Status != AsyncStatus.Completed) throw new Exception("async " + operation.Status + " after " + waitedMilliseconds + " ms");
        return operation.GetResults();
    }

    [STAThread]
    static void Main(string[] args)
    {
        var report = new StringBuilder();
        string target = args[0];
        int durationSeconds = int.Parse(args[1]);
        try
        {
            var devices = Await(DeviceInformation.FindAllAsync(LampArray.GetDeviceSelector()), 8000);
            foreach (var device in devices)
            {
                bool isVirtual = device.Id.IndexOf("VHF", StringComparison.OrdinalIgnoreCase) >= 0;
                if ((target == "vhf") != isVirtual) continue;
                report.AppendLine("opening " + device.Id);
                var lampArray = Await(LampArray.FromIdAsync(device.Id), 20000);
                lampArray.IsEnabled = true;
                report.AppendLine("opened, lamps=" + lampArray.LampCount + ", strobing " + durationSeconds + " s");
                Color[] sequence = { ColorHelper.FromArgb(255, 255, 220, 0), ColorHelper.FromArgb(255, 0, 80, 255), ColorHelper.FromArgb(255, 255, 255, 255) };
                DateTime endTime = DateTime.Now.AddSeconds(durationSeconds);
                for (int step = 0; DateTime.Now < endTime; step++)
                {
                    lampArray.SetColor(sequence[step % sequence.Length]);
                    Thread.Sleep(500);
                }
                report.AppendLine("done");
            }
        }
        catch (Exception error) { report.AppendLine("ERROR: " + error.Message); }
        File.WriteAllText(args[2], report.ToString(), Encoding.UTF8);
        Environment.Exit(0);
    }
}
