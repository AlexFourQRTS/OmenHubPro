using System.Drawing;
using System.Windows.Forms;
using OmenHub.Hardware.Sensors;

namespace OmenHub.Ui.Pages
{
    /// <summary>A content page. The shell calls LoadFromSettings on every tab switch (settings are the source of truth).</summary>
    public abstract class PageBase : Panel
    {
        public const int PageWidth = 850;
        public const int PageHeight = 636;

        protected PageBase()
        {
            Tag = Theme.ThemeApplier.RolePage;
            Size = new Size(PageWidth, PageHeight);
            Visible = false;
        }

        /// <summary>Re-reads every value from AppSettings into the controls.</summary>
        public virtual void LoadFromSettings() { }

        /// <summary>Called once per second with the latest sensor snapshot.</summary>
        public virtual void ShowSnapshot(SensorSnapshot snapshot) { }
    }
}
