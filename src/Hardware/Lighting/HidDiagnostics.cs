using System;
using System.Text;
using OmenHub.Core.Logging;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>Read-only look at the keyboard's vendor HID interfaces: which feature reports answer and what they contain.</summary>
    public static class HidDiagnostics
    {
        private const ushort KeyboardVendorId = 0x0D62;
        private const ushort VendorPageStart = 0xFF00;
        private const ushort LightingPage = 0x59;
        private const int MaximumReportId = 8;

        public static string ReadFeatureReports()
        {
            var summary = new StringBuilder();
            foreach (HidInterop.HidInterface hidInterface in HidInterop.FindInterfaces(KeyboardVendorId))
            {
                bool interesting = hidInterface.UsagePage >= VendorPageStart || hidInterface.UsagePage == LightingPage;
                if (!interesting || hidInterface.FeatureLength == 0) continue;
                string line = ProbeInterface(hidInterface);
                AppLog.Write("HID " + line);
                summary.Append(line).Append(" | ");
            }
            return summary.Length == 0 ? "интерфейсы не найдены" : summary.ToString();
        }

        private static string ProbeInterface(HidInterop.HidInterface hidInterface)
        {
            string title = string.Format("page=0x{0:X} feat={1}", hidInterface.UsagePage, hidInterface.FeatureLength);
            IntPtr handle = HidInterop.CreateFile(hidInterface.Path, HidInterop.ReadWriteAccess,
                HidInterop.ShareReadWrite, IntPtr.Zero, HidInterop.OpenExisting, 0, IntPtr.Zero);
            if (handle == new IntPtr(-1)) return title + " (не открыт)";
            try
            {
                var answered = new StringBuilder();
                for (int reportId = 0; reportId <= MaximumReportId; reportId++)
                {
                    byte[] buffer = new byte[hidInterface.FeatureLength];
                    buffer[0] = (byte)reportId;
                    if (HidInterop.HidD_GetFeature(handle, buffer, buffer.Length))
                        answered.Append(" id" + reportId + ":" + BitConverter.ToString(buffer, 0, Math.Min(12, buffer.Length)));
                }
                return title + (answered.Length == 0 ? " (отчёты не отвечают)" : answered.ToString());
            }
            finally { HidInterop.CloseHandle(handle); }
        }
    }
}
