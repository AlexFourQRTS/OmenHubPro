using System;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>
    /// Talks to the keyboard's HID LampArray interface (usage page 0x59) directly with feature reports,
    /// following the HID "Lighting and Illumination" specification. The device exposes updates as feature reports.
    /// Report ids: 1 = array attributes, 4 = multi-lamp update, 5 = range update, 6 = autonomous-mode control.
    /// </summary>
    public sealed class DirectLampArray : IDisposable
    {
        private const ushort KeyboardVendorId = 0x0D62;
        private const ushort LightingUsagePage = 0x59;
        private const byte ReportAttributes = 1;
        private const byte ReportLampRequest = 2;
        private const byte ReportLampResponse = 3;
        private const byte ReportMultiUpdate = 4;
        private const byte ReportRangeUpdate = 5;
        private const byte ReportControl = 6;
        private const byte UpdateComplete = 1;
        private const int LampsPerMultiUpdate = 8;

        private readonly IntPtr handle;
        private readonly int reportLength;

        public int LampCount { get; private set; }

        private DirectLampArray(IntPtr handle, int reportLength)
        {
            this.handle = handle;
            this.reportLength = reportLength;
        }

        public static DirectLampArray Open()
        {
            foreach (HidInterop.HidInterface candidate in HidInterop.FindInterfaces(KeyboardVendorId))
            {
                if (candidate.UsagePage != LightingUsagePage) continue;
                IntPtr handle = HidInterop.CreateFile(candidate.Path, HidInterop.ReadWriteAccess,
                    HidInterop.ShareReadWrite, IntPtr.Zero, HidInterop.OpenExisting, 0, IntPtr.Zero);
                if (handle == new IntPtr(-1)) return null;
                var lampArray = new DirectLampArray(handle, candidate.FeatureLength);
                return lampArray.ReadAttributes() ? lampArray : null;
            }
            return null;
        }

        private bool ReadAttributes()
        {
            byte[] report = new byte[reportLength];
            report[0] = ReportAttributes;
            if (!HidInterop.HidD_GetFeature(handle, report, report.Length)) return false;
            LampCount = report[1] | (report[2] << 8);
            return LampCount > 0;
        }

        public sealed class LampInfo
        {
            public int Id, XMicrometers, YMicrometers, ZMicrometers;
            public uint Purposes;
        }

        /// <summary>Reads position and purpose of every lamp (report 2 selects the lamp, report 3 returns its attributes).</summary>
        public LampInfo[] ReadLampInfos()
        {
            var infos = new LampInfo[LampCount];
            for (int lampId = 0; lampId < LampCount; lampId++)
            {
                byte[] request = NewReport(ReportLampRequest);
                WriteUInt16(request, 1, lampId);
                byte[] response = NewReport(ReportLampResponse);
                if (!Send(request) || !HidInterop.HidD_GetFeature(handle, response, response.Length)) return infos;
                infos[lampId] = new LampInfo
                {
                    Id = response[1] | (response[2] << 8),
                    XMicrometers = BitConverter.ToInt32(response, 3),
                    YMicrometers = BitConverter.ToInt32(response, 7),
                    ZMicrometers = BitConverter.ToInt32(response, 11),
                    Purposes = BitConverter.ToUInt32(response, 15)
                };
            }
            return infos;
        }

        /// <summary>false = the host controls every lamp; true = the keyboard runs its own effect again.</summary>
        public bool SetAutonomous(bool autonomous)
        {
            byte[] report = NewReport(ReportControl);
            report[1] = (byte)(autonomous ? 1 : 0);
            return Send(report);
        }

        public bool PaintAll(byte red, byte green, byte blue)
        {
            byte[] report = NewReport(ReportRangeUpdate);
            report[1] = UpdateComplete;
            WriteUInt16(report, 2, 0);
            WriteUInt16(report, 4, LampCount - 1);
            report[6] = red; report[7] = green; report[8] = blue; report[9] = 255;
            return Send(report);
        }

        /// <summary>Paints lamps one by one in groups of eight; colorOf returns {red, green, blue} for a lamp index.</summary>
        public bool PaintEach(Func<int, byte[]> colorOf)
        {
            bool allAccepted = true;
            for (int first = 0; first < LampCount; first += LampsPerMultiUpdate)
            {
                int count = Math.Min(LampsPerMultiUpdate, LampCount - first);
                byte[] report = NewReport(ReportMultiUpdate);
                report[1] = (byte)count;
                report[2] = first + count >= LampCount ? UpdateComplete : (byte)0;
                for (int slot = 0; slot < count; slot++)
                {
                    WriteUInt16(report, 3 + slot * 2, first + slot);
                    byte[] color = colorOf(first + slot);
                    int colorOffset = 3 + LampsPerMultiUpdate * 2 + slot * 4;
                    report[colorOffset] = color[0]; report[colorOffset + 1] = color[1];
                    report[colorOffset + 2] = color[2]; report[colorOffset + 3] = 255;
                }
                allAccepted &= Send(report);
            }
            return allAccepted;
        }

        private byte[] NewReport(byte reportId)
        {
            byte[] report = new byte[reportLength];
            report[0] = reportId;
            return report;
        }

        private bool Send(byte[] report)
        {
            return HidInterop.HidD_SetFeature(handle, report, report.Length);
        }

        private static void WriteUInt16(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)(value & 255);
            buffer[offset + 1] = (byte)(value >> 8);
        }

        public void Dispose()
        {
            HidInterop.CloseHandle(handle);
        }
    }
}
