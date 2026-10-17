using System;
using System.IO;
using System.Text;
using OmenHub.Hardware.Lighting;

// Standalone run of the keyboard experiments: strobe, then per-key rainbow.
class DirectLampRunner
{
    static void Main(string[] arguments)
    {
        var report = new StringBuilder();
        try
        {
            report.AppendLine("strobe: " + DirectLampExperiments.StrobeWholeKeyboard());
            report.AppendLine("rainbow: " + DirectLampExperiments.RainbowPerKey());
        }
        catch (Exception error) { report.AppendLine("ERROR: " + error); }
        File.WriteAllText(arguments[0], report.ToString(), Encoding.UTF8);
    }
}
