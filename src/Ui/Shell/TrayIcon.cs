using System;
using System.Drawing;
using System.Windows.Forms;

namespace OmenHub.Ui.Shell
{
    /// <summary>Notification-area icon with a small menu. The window hides here instead of closing.</summary>
    public sealed class TrayIcon : IDisposable
    {
        private readonly NotifyIcon notifyIcon;

        public event EventHandler OpenRequested;
        public event EventHandler FansAutoRequested;
        public event EventHandler FansMaxRequested;
        public event EventHandler ExitRequested;

        public TrayIcon()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Открыть OmenHub", null, delegate { Raise(OpenRequested); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Вентиляторы: авто (BIOS)", null, delegate { Raise(FansAutoRequested); });
            menu.Items.Add("Вентиляторы: максимум", null, delegate { Raise(FansMaxRequested); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Выход (кулеры вернутся в авто)", null, delegate { Raise(ExitRequested); });

            notifyIcon = new NotifyIcon { ContextMenuStrip = menu, Text = "OmenHub", Visible = true };
            try { notifyIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { notifyIcon.Icon = SystemIcons.Application; }
            notifyIcon.MouseClick += delegate (object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) Raise(OpenRequested);
            };
        }

        public void SetTooltip(string text)
        {
            notifyIcon.Text = text.Length > 63 ? text.Substring(0, 63) : text;
        }

        public void ShowHint(string title, string message)
        {
            notifyIcon.ShowBalloonTip(2500, title, message, ToolTipIcon.Info);
        }

        public void Dispose()
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }

        private void Raise(EventHandler handler)
        {
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
