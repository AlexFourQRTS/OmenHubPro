using System;
using System.Windows.Forms;
using OmenHub.Core.Logging;
using OmenHub.Core.Settings;
using OmenHub.Engine;
using OmenHub.Hardware.Sensors;
using OmenHub.Ui.Overlay;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Shell
{
    /// <summary>
    /// The application window. Closing or minimizing hides it to the tray; the engine keeps running.
    /// A real exit is requested through ExitApplication (tray menu or "/quit").
    /// </summary>
    public sealed partial class MainForm : Form
    {
        private readonly System.Windows.Forms.Timer refreshTimer = new System.Windows.Forms.Timer();
        private readonly TrayIcon trayIcon = new TrayIcon();
        private readonly OmenKeyListener omenKeyListener = new OmenKeyListener();
        private readonly OverlayController overlay = new OverlayController();
        private bool exitRequested;
        private bool startHidden;
        private bool trayHintShown;
        private int currentPage;

        public MainForm(bool startHidden)
        {
            this.startHidden = startHidden;
            BuildFrame();
            BuildPages();
            ThemeApplier.Apply(this);
            ApplyLabVisibility();
            ShowPage(0);
            WireShellEvents();

            refreshTimer.Interval = 1000;
            refreshTimer.Tick += delegate { RefreshLiveData(); };
            refreshTimer.Start();
            omenKeyListener.Start();
        }

        protected override void SetVisibleCore(bool value)
        {
            if (startHidden)   // autostart: begin in the tray, window stays hidden until requested
            {
                startHidden = false;
                if (!IsHandleCreated) CreateHandle();
                value = false;
            }
            base.SetVisibleCore(value);
        }

        private void WireShellEvents()
        {
            trayIcon.OpenRequested += delegate { ShowWindow(); };
            trayIcon.ExitRequested += delegate { ExitApplication(); };
            trayIcon.FansAutoRequested += delegate { ChooseFanMode(FanMode.Auto); };
            trayIcon.FansMaxRequested += delegate { ChooseFanMode(FanMode.Max); };
            trayIcon.BarToggleRequested += delegate { AppSettings.BarEnabled = !AppSettings.BarEnabled; AppSettings.MarkChanged(); ShowPage(currentPage); };
            omenKeyListener.Pressed += delegate { BeginInvoke(new Action(ToggleWindow)); };
            Resize += delegate { if (WindowState == FormWindowState.Minimized) Hide(); };
            FormClosing += OnFormClosing;
        }

        private void ChooseFanMode(FanMode mode)
        {
            AppSettings.Mode = mode;
            AppSettings.MarkChanged();
            ShowPage(currentPage);
        }

        private void ShowPage(int index)
        {
            currentPage = index;
            for (int page = 0; page < pages.Length; page++)
            {
                pages[page].Visible = page == index;
                navButtons[page].IsActive = page == index;
                navButtons[page].Invalidate();
            }
            titleLabel.Text = pageNames[index];
            pages[index].LoadFromSettings();   // settings are the single source of truth
        }

        public void ShowWindow()
        {
            Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            TopMost = true; TopMost = false;
            Activate();
            BringToFront();
        }

        private void ToggleWindow()
        {
            if (Visible && WindowState != FormWindowState.Minimized && ContainsFocus) WindowState = FormWindowState.Minimized;
            else ShowWindow();
        }

        public void ExitApplication()
        {
            exitRequested = true;
            Close();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!exitRequested && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                if (!trayHintShown)
                {
                    trayHintShown = true;
                    trayIcon.ShowHint("OmenHub работает в трее", "Полный выход: правый клик по значку → Выход.");
                }
                return;
            }
            SettingsStorage.Save();
            omenKeyListener.Dispose();
            overlay.Dispose();
            trayIcon.Dispose();
        }

        private void RefreshLiveData()
        {
            if (AppSettings.TakeUnsavedFlag()) PersistSettings();
            SensorSnapshot snapshot = ControlEngine.Latest;
            statusLabel.Text = snapshot.StatusText + "    |    режим: " + AppSettings.Mode + "    |    питание: " + (snapshot.OnBattery ? "батарея" : "сеть");
            statusLabel.ForeColor = snapshot.ThermalAlertActive ? Palette.Bad : Palette.SubText;
            cpuFanChip.Value = snapshot.CpuFanRpm < 0 ? "—" : snapshot.CpuFanRpm + " об/мин";
            gpuFanChip.Value = snapshot.GpuFanRpm < 0 ? "—" : snapshot.GpuFanRpm + " об/мин";
            cpuFanChip.IsAlert = gpuFanChip.IsAlert = snapshot.ThermalAlertActive;
            cpuFanChip.Invalidate();
            gpuFanChip.Invalidate();
            trayIcon.SetTooltip("OmenHub  CPU " + cpuFanChip.Value + " / GPU " + gpuFanChip.Value + (snapshot.ThermalAlertActive ? "  ТРЕВОГА" : ""));
            pages[currentPage].ShowSnapshot(snapshot);
        }

        private static void PersistSettings()
        {
            SettingsStorage.Save();
            AppLog.Write("CONFIG mode=" + AppSettings.Mode + " manual=" + AppSettings.ManualCpuFanRpm + "/" + AppSettings.ManualGpuFanRpm
                + " cpuCurve=" + AppSettings.CpuCurve.Serialize() + " gpuCurve=" + AppSettings.GpuCurve.Serialize()
                + " cpuW=" + AppSettings.CpuWatts + " proAuto=" + AppSettings.ProAutoEnabled + " batteryEco=" + AppSettings.BatteryEcoEnabled);
        }
    }
}
