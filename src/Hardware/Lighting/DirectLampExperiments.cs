using System;
using System.Threading;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>Visible experiments on the keyboard through DirectLampArray (per-key HID lighting).</summary>
    public static class DirectLampExperiments
    {
        private const int StepMilliseconds = 700;
        private const int RoundCount = 3;

        public static string StrobeWholeKeyboard()
        {
            using (DirectLampArray keyboard = DirectLampArray.Open())
            {
                if (keyboard == null) return "интерфейс ламп клавиатуры не открылся";
                string result = "ламп: " + keyboard.LampCount + "; управление хостом: " + (keyboard.SetAutonomous(false) ? "ок" : "отказ") + "; ";
                for (int round = 0; round < RoundCount; round++)
                {
                    result += keyboard.PaintAll(255, 220, 0) ? "жёлтый ок; " : "жёлтый отказ; ";
                    Thread.Sleep(StepMilliseconds);
                    result += keyboard.PaintAll(0, 80, 255) ? "синий ок; " : "синий отказ; ";
                    Thread.Sleep(StepMilliseconds);
                    result += keyboard.PaintAll(255, 255, 255) ? "белый ок; " : "белый отказ; ";
                    Thread.Sleep(StepMilliseconds);
                }
                return result + "возврат клавиатуре: " + (keyboard.SetAutonomous(true) ? "ок" : "отказ");
            }
        }

        public static string RainbowPerKey()
        {
            using (DirectLampArray keyboard = DirectLampArray.Open())
            {
                if (keyboard == null) return "интерфейс ламп клавиатуры не открылся";
                keyboard.SetAutonomous(false);
                bool accepted = keyboard.PaintEach(delegate (int lampIndex) { return RainbowColor(lampIndex, keyboard.LampCount); });
                Thread.Sleep(StepMilliseconds * 6);
                keyboard.SetAutonomous(true);
                return "радуга по клавишам (" + keyboard.LampCount + " ламп): " + (accepted ? "принято" : "отказ");
            }
        }

        private static byte[] RainbowColor(int lampIndex, int lampCount)
        {
            double hue = 6.0 * lampIndex / lampCount;
            int sector = (int)hue;
            byte rising = (byte)(255 * (hue - sector));
            byte falling = (byte)(255 - rising);
            switch (sector % 6)
            {
                case 0: return new byte[] { 255, rising, 0 };
                case 1: return new byte[] { falling, 255, 0 };
                case 2: return new byte[] { 0, 255, rising };
                case 3: return new byte[] { 0, falling, 255 };
                case 4: return new byte[] { rising, 0, 255 };
                default: return new byte[] { 255, 0, falling };
            }
        }
    }
}
