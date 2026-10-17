using System;
using System.IO;
using System.Text;
using System.Threading;
using OmenHub.Hardware.Bios;
using OmenHub.Hardware.Lighting;

// Finds out who wins on the light bar: the BIOS colour table or HID lamp updates of the whole keyboard.
class BarPriorityTest
{
    const int StageMilliseconds = 5000;

    static void Main(string[] arguments)
    {
        var report = new StringBuilder();
        BiosInterface.Initialize();
        DirectLampArray keyboard = DirectLampArray.Open();
        if (keyboard == null) { File.WriteAllText(arguments[0], "keyboard did not open"); return; }
        byte[] white = { 255, 255, 255 };
        byte[][] black = { new byte[] { 0, 0, 0 }, new byte[] { 0, 0, 0 }, new byte[] { 0, 0, 0 }, new byte[] { 0, 0, 0 } };
        byte[][] purple = { new byte[] { 155, 107, 255 }, new byte[] { 155, 107, 255 }, new byte[] { 155, 107, 255 }, new byte[] { 155, 107, 255 } };

        keyboard.SetAutonomous(false);
        Stage(report, "1: all 120 lamps white", keyboard.PaintAll(255, 255, 255));
        Thread.Sleep(StageMilliseconds);
        Stage(report, "2: BIOS bar black", BiosKeyboardPainter.SetBarZones(black));
        Thread.Sleep(StageMilliseconds);
        Stage(report, "3: all 120 lamps white again", keyboard.PaintEach(delegate (int lamp) { return white; }));
        Thread.Sleep(StageMilliseconds);
        Stage(report, "4: BIOS bar purple", BiosKeyboardPainter.SetBarZones(purple));
        Thread.Sleep(StageMilliseconds);
        Stage(report, "5: all 120 lamps white again", keyboard.PaintEach(delegate (int lamp) { return white; }));
        Thread.Sleep(StageMilliseconds);
        keyboard.SetAutonomous(true);
        keyboard.Dispose();
        report.AppendLine("done, keyboard handed back");
        File.WriteAllText(arguments[0], report.ToString(), Encoding.UTF8);
    }

    static void Stage(StringBuilder report, string name, bool accepted)
    {
        report.AppendLine(DateTime.Now.ToString("HH:mm:ss") + "  stage " + name + "  accepted=" + accepted);
    }
}
