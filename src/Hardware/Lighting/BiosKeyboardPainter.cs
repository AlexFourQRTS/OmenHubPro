using OmenHub.Core.Logging;
using OmenHub.Hardware.Bios;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>
    /// Light-bar colours through the HP BIOS colour table (command 0x20009, 128 bytes): four zones x {red, green, blue}
    /// starting at byte 25. The BIOS keeps the table, so the bar colour survives closing the application.
    /// </summary>
    public static class BiosKeyboardPainter
    {
        private const uint CommandGetColorTable = 0x02;
        private const uint CommandSetColorTable = 0x03;
        private const int TableBytes = 128;
        private const int FirstZoneOffset = 25;

        /// <summary>Writes one colour per bar zone (left to right). Colours are {red, green, blue}.</summary>
        public static bool SetBarZones(byte[][] zoneColors)
        {
            byte[] table;
            if (!BiosInterface.Call(BiosInterface.KeyboardCommand, CommandGetColorTable, null, TableBytes, out table))
            {
                AppLog.Write("bar: BIOS did not return the colour table");
                return false;
            }
            for (int zone = 0; zone < zoneColors.Length; zone++)
            {
                int offset = FirstZoneOffset + zone * 3;
                table[offset] = zoneColors[zone][0];
                table[offset + 1] = zoneColors[zone][1];
                table[offset + 2] = zoneColors[zone][2];
            }
            byte[] ignored;
            return BiosInterface.Call(BiosInterface.KeyboardCommand, CommandSetColorTable, table, 4, out ignored);
        }
    }
}
