using System;
using System.IO;
using System.Text;
using OmenHub.Hardware.Lighting;

// Standalone run of the app's HID diagnostics: lists keyboard HID interfaces and which feature reports answer.
class HidDiagRunner
{
    static void Main(string[] arguments)
    {
        var report = new StringBuilder();
        foreach (HidInterop.HidInterface hidInterface in HidInterop.FindInterfaces(0x0D62))
            report.AppendFormat("page=0x{0:X} usage=0x{1:X} in={2} out={3} feat={4}\r\n   {5}\r\n",
                hidInterface.UsagePage, hidInterface.Usage, hidInterface.InputLength, hidInterface.OutputLength, hidInterface.FeatureLength, hidInterface.Path);
        report.AppendLine();
        report.AppendLine(HidDiagnostics.ReadFeatureReports().Replace(" | ", "\r\n"));
        File.WriteAllText(arguments[0], report.ToString(), Encoding.UTF8);
    }
}
