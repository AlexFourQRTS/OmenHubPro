using System;
using System.IO;
using System.Text;
using OmenHub.Hardware.Lighting;

// Dumps position and purpose of every keyboard lamp, to separate the key lamps from the light-bar lamps.
class LampDump
{
    static void Main(string[] arguments)
    {
        var report = new StringBuilder();
        using (DirectLampArray keyboard = DirectLampArray.Open())
        {
            if (keyboard == null) report.AppendLine("cannot open keyboard lamp array");
            else
            {
                DirectLampArray.LampInfo[] infos = keyboard.ReadLampInfos();
                report.AppendLine("lamps=" + keyboard.LampCount);
                foreach (DirectLampArray.LampInfo info in infos)
                {
                    if (info == null) continue;
                    report.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0};{1:0.0};{2:0.0};{3:0.0};{4:X}\r\n",
                        info.Id, info.XMicrometers / 1000.0, info.YMicrometers / 1000.0, info.ZMicrometers / 1000.0, info.Purposes);
                }
            }
        }
        File.WriteAllText(arguments[0], report.ToString(), Encoding.UTF8);
    }
}
