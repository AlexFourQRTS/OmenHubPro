using System;
using System.Threading;
using System.Windows.Forms;
using OmenHub.Core.Logging;
using OmenHub.Core.Processes;
using OmenHub.Core.Settings;
using OmenHub.Engine;
using OmenHub.Hardware.Bios;
using OmenHub.Ui.Shell;
using OmenHub.Ui.Theme;

namespace OmenHub.App
{
    /// <summary>Entry point: single instance, "/quit" to stop a running copy, "/autostart" to begin in the tray.</summary>
    public static class Program
    {
        private const string InstanceMutexName = "OmenHub.SingleInstance";
        private const string QuitEventName = "OmenHub.Quit";

        [STAThread]
        public static void Main(string[] arguments)
        {
            if (Array.IndexOf(arguments, "/quit") >= 0) { RequestQuitOfRunningInstance(); return; }

            bool isFirstInstance;
            using (new Mutex(true, InstanceMutexName, out isFirstInstance))
            {
                if (!isFirstInstance) return;
                SettingsStorage.Load();
                Palette.Apply(AppSettings.DarkTheme);
                ProcessPriority.RaiseToHigh();
                HookCrashLogging();

                ControlEngine.Start();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var window = new MainForm(Array.IndexOf(arguments, "/autostart") >= 0);
                ListenForQuitRequest(window);
                Application.Run(window);
                ControlEngine.Stop();
                SettingsStorage.Save();
            }
        }

        private static void RequestQuitOfRunningInstance()
        {
            try { EventWaitHandle.OpenExisting(QuitEventName).Set(); }
            catch { }
        }

        private static void ListenForQuitRequest(MainForm window)
        {
            var quitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, QuitEventName);
            var listener = new Thread(delegate ()
            {
                quitEvent.WaitOne();
                try { window.BeginInvoke(new Action(window.ExitApplication)); }
                catch { }
            });
            listener.IsBackground = true;
            listener.Start();
        }

        private static void HookCrashLogging()
        {
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e)
            {
                AppLog.Write("UNHANDLED: " + e.ExceptionObject);
                try { BiosFanCommands.SetFanMax(false); }   // never leave the fans stuck in manual mode
                catch { }
            };
            Application.ThreadException += delegate (object sender, ThreadExceptionEventArgs e)
            {
                AppLog.Write("UI EXCEPTION: " + e.Exception);
            };
        }
    }
}
