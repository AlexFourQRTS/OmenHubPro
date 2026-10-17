using System;
using System.IO;
using System.Text;
using System.Threading;
using Windows.Foundation;
using Windows.Devices.Enumeration;
using Windows.Devices.Lights;

// Lists every LampArray (Windows Dynamic Lighting device): kind, lamp count, bounding box, per-lamp info. Read-only.
class LampEnum
{
    static T Await<T>(IAsyncOperation<T> op)
    {
        int waited = 0;
        while (op.Status == AsyncStatus.Started && waited < 8000) { Thread.Sleep(10); waited += 10; }
        if (op.Status == AsyncStatus.Started) throw new Exception("timeout waiting for async operation");
        if (op.Status != AsyncStatus.Completed) throw new Exception("async " + op.Status + " 0x" + op.ErrorCode.HResult.ToString("X"));
        return op.GetResults();
    }

    [STAThread]
    static void Main(string[] a)
    {
        var sb = new StringBuilder();
        try
        {
            string sel = LampArray.GetDeviceSelector();
            sb.AppendLine("selector: " + sel);
            var infos = Await(DeviceInformation.FindAllAsync(sel));
            sb.AppendLine("devices found: " + infos.Count);
            foreach (var di in infos)
            {
                sb.AppendLine("== " + di.Name + "  [" + di.Id + "]");
                try
                {
                    var arr = Await(LampArray.FromIdAsync(di.Id));
                    if (arr == null) { sb.AppendLine("   FromIdAsync -> null"); continue; }
                    sb.AppendFormat("   kind={0} lamps={1} enabled={2} always-on={3} brightness={4} bbox={5}x{6}x{7} minUpdateInterval={8}\r\n",
                        arr.LampArrayKind, arr.LampCount, arr.IsEnabled, arr.IsConnected, arr.BrightnessLevel,
                        arr.BoundingBox.X, arr.BoundingBox.Y, arr.BoundingBox.Z, arr.MinUpdateInterval);
                    int shown = 0;
                    for (int i = 0; i < arr.LampCount; i++)
                    {
                        var li = arr.GetLampInfo(i);
                        if (shown++ < 12 || i == arr.LampCount - 1)
                            sb.AppendFormat("   lamp {0,3}: pos=({1:0.###},{2:0.###},{3:0.###}) purposes={4} rgb-levels={5}\r\n", i, li.Position.X, li.Position.Y, li.Position.Z, li.Purposes, li.RedLevelCount);
                        else if (shown == 13) sb.AppendLine("   ...");
                    }
                }
                catch (Exception e) { sb.AppendLine("   ERROR: " + e.Message); }
            }
        }
        catch (Exception e) { sb.AppendLine("FATAL: " + e); }
        File.WriteAllText(a.Length > 0 ? a[0] : "lamps.txt", sb.ToString(), Encoding.UTF8);
        Environment.Exit(0);
    }
}
