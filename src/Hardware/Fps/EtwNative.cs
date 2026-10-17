using System;
using System.Runtime.InteropServices;

namespace OmenHub.Hardware.Fps
{
    /// <summary>
    /// Minimal Event Tracing for Windows access (real-time consumer). Structure offsets are for 64-bit Windows:
    /// EVENT_TRACE_PROPERTIES is 120 bytes plus the logger name, EVENT_TRACE_LOGFILEW is 448 bytes.
    /// </summary>
    internal static class EtwNative
    {
        public const int PropertiesSize = 120;
        public const int LogFileSize = 448;
        public const uint RealTimeMode = 0x100;
        public const uint WnodeTracedGuid = 0x20000;
        public const uint ProcessModeRealTime = 0x100, ProcessModeEventRecord = 0x10000000;
        public const uint EnableProvider = 1, StopSession = 1;
        public const uint ErrorAlreadyExists = 183;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void EventRecordCallback(IntPtr eventRecord);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern uint StartTraceW(out ulong handle, string name, IntPtr properties);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern uint ControlTraceW(ulong handle, string name, IntPtr properties, uint code);

        [DllImport("advapi32.dll")]
        public static extern uint EnableTraceEx2(ulong handle, ref Guid provider, uint code, byte level, ulong anyKeyword, ulong allKeyword, uint timeout, IntPtr parameters);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern ulong OpenTraceW(IntPtr logfile);

        [DllImport("advapi32.dll")]
        public static extern uint ProcessTrace(ulong[] handles, uint count, IntPtr startTime, IntPtr endTime);

        [DllImport("advapi32.dll")]
        public static extern uint CloseTrace(ulong handle);

        /// <summary>Allocates the properties block for a real-time session with the given logger name (free with FreeHGlobal).</summary>
        public static IntPtr AllocateProperties(string loggerName)
        {
            int total = PropertiesSize + (loggerName.Length + 1) * 2;
            IntPtr block = Marshal.AllocHGlobal(total);
            for (int offset = 0; offset < total; offset += 2) Marshal.WriteInt16(block, offset, 0);
            Marshal.WriteInt32(block, 0, total);                         // Wnode.BufferSize
            Marshal.WriteInt32(block, 44, (int)WnodeTracedGuid);         // Wnode.Flags
            Marshal.WriteInt32(block, 64, (int)RealTimeMode);            // LogFileMode
            Marshal.WriteInt32(block, 116, PropertiesSize);              // LoggerNameOffset
            Marshal.Copy(loggerName.ToCharArray(), 0, new IntPtr(block.ToInt64() + PropertiesSize), loggerName.Length);
            return block;
        }

        /// <summary>Allocates an EVENT_TRACE_LOGFILEW configured for real-time record callbacks.</summary>
        public static IntPtr AllocateLogFile(IntPtr loggerNameMemory, EventRecordCallback callback)
        {
            IntPtr logFile = Marshal.AllocHGlobal(LogFileSize);
            for (int offset = 0; offset < LogFileSize; offset += 4) Marshal.WriteInt32(logFile, offset, 0);
            Marshal.WriteIntPtr(logFile, 8, loggerNameMemory);                                         // LoggerName
            Marshal.WriteInt32(logFile, 28, (int)(ProcessModeRealTime | ProcessModeEventRecord));      // ProcessTraceMode
            Marshal.WriteIntPtr(logFile, 424, Marshal.GetFunctionPointerForDelegate(callback));        // EventRecordCallback
            return logFile;
        }

        public static int ProcessIdOf(IntPtr eventRecord) { return Marshal.ReadInt32(eventRecord, 12); }

        public static int EventIdOf(IntPtr eventRecord) { return Marshal.ReadInt16(eventRecord, 40); }
    }
}
