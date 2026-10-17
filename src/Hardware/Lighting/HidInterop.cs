using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>Minimal HID API access: enumerate interfaces of a vendor and read their capabilities.</summary>
    public static class HidInterop
    {
        public sealed class HidInterface
        {
            public string Path;
            public ushort UsagePage, Usage, InputLength, OutputLength, FeatureLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct InterfaceData { public int Size; public Guid ClassGuid; public int Flags; public IntPtr Reserved; }

        [StructLayout(LayoutKind.Sequential)]
        private struct HidAttributes { public int Size; public ushort VendorId, ProductId, VersionNumber; }

        [StructLayout(LayoutKind.Sequential)]
        private struct HidCapabilities
        {
            public ushort Usage, UsagePage, InputReportByteLength, OutputReportByteLength, FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)] public ushort[] Reserved;
            public ushort NumberLinkCollectionNodes, NumberInputButtonCaps, NumberInputValueCaps, NumberInputDataIndices,
                NumberOutputButtonCaps, NumberOutputValueCaps, NumberOutputDataIndices,
                NumberFeatureButtonCaps, NumberFeatureValueCaps, NumberFeatureDataIndices;
        }

        [DllImport("hid.dll")] private static extern void HidD_GetHidGuid(out Guid guid);
        [DllImport("hid.dll")] private static extern bool HidD_GetAttributes(IntPtr handle, ref HidAttributes attributes);
        [DllImport("hid.dll")] private static extern bool HidD_GetPreparsedData(IntPtr handle, out IntPtr data);
        [DllImport("hid.dll")] private static extern bool HidD_FreePreparsedData(IntPtr data);
        [DllImport("hid.dll")] private static extern int HidP_GetCaps(IntPtr data, out HidCapabilities capabilities);
        [DllImport("hid.dll")] public static extern bool HidD_GetFeatureReport(IntPtr handle, byte[] buffer, int length);
        [DllImport("setupapi.dll")] private static extern IntPtr SetupDiGetClassDevs(ref Guid guid, string enumerator, IntPtr parent, int flags);
        [DllImport("setupapi.dll")] private static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr info, ref Guid guid, int index, ref InterfaceData data);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode)] private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref InterfaceData data, IntPtr detail, int size, out int required, IntPtr info);
        [DllImport("setupapi.dll")] private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern IntPtr CreateFile(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll")] public static extern bool CloseHandle(IntPtr handle);

        public const uint ReadWriteAccess = 0xC0000000;
        public const uint ShareReadWrite = 3;
        public const uint OpenExisting = 3;

        public static List<HidInterface> FindInterfaces(ushort vendorId)
        {
            var found = new List<HidInterface>();
            Guid hidGuid;
            HidD_GetHidGuid(out hidGuid);
            IntPtr deviceSet = SetupDiGetClassDevs(ref hidGuid, null, IntPtr.Zero, 0x12);
            var data = new InterfaceData();
            data.Size = Marshal.SizeOf(data);
            for (int index = 0; SetupDiEnumDeviceInterfaces(deviceSet, IntPtr.Zero, ref hidGuid, index, ref data); index++)
            {
                string path = ReadPath(deviceSet, ref data);
                HidInterface entry = path == null ? null : Describe(path, vendorId);
                if (entry != null) found.Add(entry);
            }
            SetupDiDestroyDeviceInfoList(deviceSet);
            return found;
        }

        private static string ReadPath(IntPtr deviceSet, ref InterfaceData data)
        {
            int required;
            SetupDiGetDeviceInterfaceDetail(deviceSet, ref data, IntPtr.Zero, 0, out required, IntPtr.Zero);
            IntPtr detail = Marshal.AllocHGlobal(required);
            try
            {
                Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                if (!SetupDiGetDeviceInterfaceDetail(deviceSet, ref data, detail, required, out required, IntPtr.Zero)) return null;
                return Marshal.PtrToStringUni(new IntPtr(detail.ToInt64() + 4));
            }
            finally { Marshal.FreeHGlobal(detail); }
        }

        private static HidInterface Describe(string path, ushort vendorId)
        {
            IntPtr handle = CreateFile(path, 0, ShareReadWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
            if (handle == new IntPtr(-1)) return null;
            try
            {
                var attributes = new HidAttributes();
                attributes.Size = Marshal.SizeOf(attributes);
                HidD_GetAttributes(handle, ref attributes);
                if (attributes.VendorId != vendorId) return null;
                IntPtr preparsed;
                var capabilities = new HidCapabilities();
                if (!HidD_GetPreparsedData(handle, out preparsed)) return null;
                HidP_GetCaps(preparsed, out capabilities);
                HidD_FreePreparsedData(preparsed);
                return new HidInterface
                {
                    Path = path, UsagePage = capabilities.UsagePage, Usage = capabilities.Usage,
                    InputLength = capabilities.InputReportByteLength, OutputLength = capabilities.OutputReportByteLength,
                    FeatureLength = capabilities.FeatureReportByteLength
                };
            }
            finally { CloseHandle(handle); }
        }
    }
}
