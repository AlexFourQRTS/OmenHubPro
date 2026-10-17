using OmenHub.Core.Settings;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>Colour chooser for one zone of the light bar, with its own "switch off" button.</summary>
    public sealed class BarZoneColumn
    {
        private readonly int zoneIndex;
        private readonly ColorPad colorPad;

        public BarZoneColumn(System.Windows.Forms.Control parent, int zoneIndex, int left)
        {
            this.zoneIndex = zoneIndex;
            PageKit.MakeLabel(parent, "Зона " + (zoneIndex + 1), left, 62, 9.5f, true, PageKit.RoleText);
            colorPad = new ColorPad { Left = left, Top = 88, Width = 184, Height = 120 };
            parent.Controls.Add(colorPad);
            ActionButton offButton = PageKit.MakeButton(parent, "Выключить зону", left, 216, 184, false);
            colorPad.ColorChanged += delegate { Store(colorPad.SelectedRgb[0], colorPad.SelectedRgb[1], colorPad.SelectedRgb[2]); };
            offButton.Click += delegate { Store(0, 0, 0); LoadFromSettings(); };
        }

        public void LoadFromSettings()
        {
            byte[] color = AppSettings.BarZoneColors[zoneIndex];
            colorPad.SetColor(color[0], color[1], color[2]);
        }

        private void Store(byte red, byte green, byte blue)
        {
            AppSettings.BarZoneColors[zoneIndex] = new[] { red, green, blue };
            AppSettings.MarkChanged();
        }
    }
}
