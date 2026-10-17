using System;
using System.Text;
using System.Threading;
using OmenHub.Core.Logging;
using OmenHub.Hardware.Bios;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>Keyboard lighting through the HP BIOS (command 0x20009): 4-zone colour table and backlight switch.</summary>
    public static class BiosKeyboardPainter
    {
        private const uint CommandGetColorTable = 0x02;
        private const uint CommandSetColorTable = 0x03;
        private const uint CommandSetBacklight = 0x05;
        private const int TableBytes = 128;
        private const int FirstZoneOffset = 25;
        private const int ZoneCount = 4;
        private const int StrobeStepMilliseconds = 1500;
        private const byte BacklightOn = 0xE4;
        private const byte BacklightOff = 0x64;

        public static string DumpColorTable()
        {
            byte[] table;
            if (!BiosInterface.Call(BiosInterface.KeyboardCommand, CommandGetColorTable, null, TableBytes, out table))
                return "BIOS не отдал таблицу цветов";
            string hex = ToHex(table, 48);
            AppLog.Write("keyboard colour table: " + hex);
            return "таблица: " + hex;
        }

        public static string StrobeZones(bool blueGreenRedOrder, int rounds)
        {
            byte[] original;
            if (!BiosInterface.Call(BiosInterface.KeyboardCommand, CommandGetColorTable, null, TableBytes, out original))
                return "BIOS не отдал таблицу цветов";

            string result = "";
            for (int round = 0; round < rounds; round++)
            {
                result += PaintAllZones(original, 255, 220, 0, blueGreenRedOrder) ? "жёлтый ок; " : "жёлтый отказ; ";
                Thread.Sleep(StrobeStepMilliseconds);
                result += PaintAllZones(original, 0, 80, 255, blueGreenRedOrder) ? "синий ок; " : "синий отказ; ";
                Thread.Sleep(StrobeStepMilliseconds);
            }
            WriteTable(original);
            return result + "исходные цвета возвращены";
        }

        public static string BlinkBacklight(int rounds)
        {
            string result = "";
            for (int round = 0; round < rounds; round++)
            {
                result += SetBacklight(false) ? "выкл ок; " : "выкл отказ; ";
                Thread.Sleep(StrobeStepMilliseconds);
                result += SetBacklight(true) ? "вкл ок; " : "вкл отказ; ";
                Thread.Sleep(StrobeStepMilliseconds);
            }
            return result;
        }

        private static bool PaintAllZones(byte[] baseTable, byte red, byte green, byte blue, bool blueGreenRedOrder)
        {
            byte[] table = (byte[])baseTable.Clone();
            for (int zone = 0; zone < ZoneCount; zone++)
            {
                int offset = FirstZoneOffset + zone * 3;
                table[offset] = blueGreenRedOrder ? blue : red;
                table[offset + 1] = green;
                table[offset + 2] = blueGreenRedOrder ? red : blue;
            }
            return WriteTable(table);
        }

        private static bool WriteTable(byte[] table)
        {
            byte[] ignored;
            return BiosInterface.Call(BiosInterface.KeyboardCommand, CommandSetColorTable, table, 4, out ignored);
        }

        private static bool SetBacklight(bool enabled)
        {
            byte[] ignored;
            byte[] payload = { enabled ? BacklightOn : BacklightOff, 0, 0, 0 };
            return BiosInterface.Call(BiosInterface.KeyboardCommand, CommandSetBacklight, payload, 4, out ignored);
        }

        private static string ToHex(byte[] data, int count)
        {
            var text = new StringBuilder();
            for (int index = 0; index < count && index < data.Length; index++) text.Append(data[index].ToString("X2")).Append(' ');
            return text.ToString().Trim();
        }
    }
}
