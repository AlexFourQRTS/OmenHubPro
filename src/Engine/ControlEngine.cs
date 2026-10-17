using System;
using System.Threading;
using OmenHub.Core.Logging;
using OmenHub.Hardware.Bios;
using OmenHub.Hardware.Fans;
using OmenHub.Hardware.Power;
using OmenHub.Hardware.Sensors;

namespace OmenHub.Engine
{
    /// <summary>Background loop: read sensors, apply fan mode, apply power profile. Runs at highest thread priority.</summary>
    public static class ControlEngine
    {
        private const int CycleMilliseconds = 1500;
        private const int SleepSliceMilliseconds = 100;
        private const int StopTimeoutMilliseconds = 3000;

        private static Thread workerThread;
        private static volatile bool running;
        private static bool biosAvailable;
        private static SensorReader sensorReader;
        private static FanController fanController;
        private static PowerProfileController powerController;

        public static volatile SensorSnapshot Latest = new SensorSnapshot();

        public static void Start()
        {
            AppLog.Write("=== OmenHub start, " + Environment.OSVersion + " ===");
            biosAvailable = BiosInterface.Initialize();
            sensorReader = new SensorReader(biosAvailable);
            fanController = new FanController();
            powerController = new PowerProfileController();

            running = true;
            workerThread = new Thread(RunLoop);
            workerThread.IsBackground = true;
            workerThread.Priority = ThreadPriority.Highest;
            workerThread.Start();
        }

        /// <summary>Stops the loop and hands the fans back to the BIOS.</summary>
        public static void Stop()
        {
            running = false;
            try { if (workerThread != null) workerThread.Join(StopTimeoutMilliseconds); }
            catch { }
            if (biosAvailable) BiosFanCommands.SetFanMax(false);
        }

        private static void RunLoop()
        {
            while (running)
            {
                SensorSnapshot snapshot = new SensorSnapshot();
                try
                {
                    snapshot = sensorReader.Read();
                    if (biosAvailable) fanController.Apply(snapshot);
                    powerController.Apply(snapshot);
                }
                catch (Exception error) { snapshot.StatusText = "Ошибка: " + error.Message; }
                Latest = snapshot;
                for (int waited = 0; waited < CycleMilliseconds && running; waited += SleepSliceMilliseconds)
                    Thread.Sleep(SleepSliceMilliseconds);
            }
        }
    }
}
