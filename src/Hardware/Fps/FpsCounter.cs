using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using OmenHub.Core.Logging;

namespace OmenHub.Hardware.Fps
{
    /// <summary>
    /// Frames per second of any process, counted from the Microsoft-Windows-DXGI "Present" events (the source that
    /// PresentMon uses). Nothing is injected into the game. The trace session runs only while FPS is wanted.
    /// </summary>
    public static class FpsCounter
    {
        private const string SessionName = "OmenHubFps";
        private const int PresentStartEventId = 42;
        private static readonly Guid DxgiProvider = new Guid("CA11C036-0102-4A2D-A6AD-F03CFED5D3C9");
        private static readonly EtwNative.EventRecordCallback callback = OnEvent;   // kept alive for the native side

        private static readonly ConcurrentDictionary<int, int> framesThisSecond = new ConcurrentDictionary<int, int>();
        private static readonly ConcurrentDictionary<int, int> framesLastSecond = new ConcurrentDictionary<int, int>();
        private static Thread workerThread;
        private static ulong sessionHandle, traceHandle;
        private static IntPtr properties, nameMemory, logFile;
        private static volatile bool sessionActive;

        /// <summary>Set by whoever shows the FPS; the session is started and stopped accordingly.</summary>
        public static volatile bool Wanted;

        public static void Start()
        {
            if (workerThread != null) return;
            workerThread = new Thread(RunLoop);
            workerThread.IsBackground = true;
            workerThread.Start();
        }

        /// <summary>Frames per second of the process during the last full second (-1 = unknown).</summary>
        public static int FramesPerSecond(int processId)
        {
            int frames;
            return framesLastSecond.TryGetValue(processId, out frames) && frames > 0 ? frames : -1;
        }

        public static void StopNow()
        {
            Wanted = false;
            if (sessionActive) EndSession();
        }

        private static void RunLoop()
        {
            while (true)
            {
                if (Wanted && !sessionActive) BeginSession();
                if (!Wanted && sessionActive) EndSession();
                Thread.Sleep(1000);
                framesLastSecond.Clear();
                foreach (var entry in framesThisSecond) framesLastSecond[entry.Key] = entry.Value;
                framesThisSecond.Clear();
            }
        }

        private static void OnEvent(IntPtr eventRecord)
        {
            if (EtwNative.EventIdOf(eventRecord) != PresentStartEventId) return;
            framesThisSecond.AddOrUpdate(EtwNative.ProcessIdOf(eventRecord), 1, delegate (int processId, int count) { return count + 1; });
        }

        private static void BeginSession()
        {
            try
            {
                properties = EtwNative.AllocateProperties(SessionName);
                uint result = EtwNative.StartTraceW(out sessionHandle, SessionName, properties);
                if (result == EtwNative.ErrorAlreadyExists)   // a session left over from a crash: stop it and retry
                {
                    EtwNative.ControlTraceW(0, SessionName, properties, EtwNative.StopSession);
                    result = EtwNative.StartTraceW(out sessionHandle, SessionName, properties);
                }
                if (result != 0) { AppLog.Write("FPS: StartTrace failed " + result); return; }

                Guid provider = DxgiProvider;
                EtwNative.EnableTraceEx2(sessionHandle, ref provider, EtwNative.EnableProvider, 5, ulong.MaxValue, 0, 0, IntPtr.Zero);
                nameMemory = Marshal.StringToHGlobalUni(SessionName);
                logFile = EtwNative.AllocateLogFile(nameMemory, callback);
                traceHandle = EtwNative.OpenTraceW(logFile);
                var consumer = new Thread(delegate () { EtwNative.ProcessTrace(new[] { traceHandle }, 1, IntPtr.Zero, IntPtr.Zero); });
                consumer.IsBackground = true;
                consumer.Start();
                sessionActive = true;
                AppLog.Write("FPS: trace session started");
            }
            catch (Exception error) { AppLog.Write("FPS: start failed: " + error.Message); }
        }

        private static void EndSession()
        {
            try
            {
                EtwNative.ControlTraceW(sessionHandle, null, properties, EtwNative.StopSession);
                EtwNative.CloseTrace(traceHandle);
            }
            catch (Exception error) { AppLog.Write("FPS: stop failed: " + error.Message); }
            sessionActive = false;
            AppLog.Write("FPS: trace session stopped");
        }
    }
}
