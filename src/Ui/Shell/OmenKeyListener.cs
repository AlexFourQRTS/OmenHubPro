using System;
using System.Management;
using OmenHub.Core.Logging;

namespace OmenHub.Ui.Shell
{
    /// <summary>Listens for the physical OMEN key: the BIOS raises WMI event 29 (hpqBEvnt) when it is pressed.</summary>
    public sealed class OmenKeyListener : IDisposable
    {
        private const int OmenKeyEventId = 29;
        private const int DebounceMilliseconds = 800;

        private ManagementEventWatcher watcher;
        private DateTime lastPress = DateTime.MinValue;

        /// <summary>Raised on a background thread; the subscriber must marshal to the UI thread.</summary>
        public event EventHandler Pressed;

        public void Start()
        {
            try
            {
                watcher = new ManagementEventWatcher(new ManagementScope(@"root\wmi"), new WqlEventQuery("SELECT * FROM hpqBEvnt"));
                watcher.EventArrived += OnBiosEvent;
                watcher.Start();
                AppLog.Write("OMEN key listener started");
            }
            catch (Exception error) { AppLog.Write("OMEN key listener FAILED: " + error.Message); }
        }

        private void OnBiosEvent(object sender, EventArrivedEventArgs eventArgs)
        {
            if (Convert.ToInt32(eventArgs.NewEvent["EventID"]) != OmenKeyEventId) return;
            if ((DateTime.Now - lastPress).TotalMilliseconds < DebounceMilliseconds) return;   // key held or bouncing
            lastPress = DateTime.Now;
            AppLog.Write("OMEN key pressed (data=" + eventArgs.NewEvent["EventData"] + ")");
            if (Pressed != null) Pressed(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            try { if (watcher != null) watcher.Stop(); }
            catch { }
        }
    }
}
