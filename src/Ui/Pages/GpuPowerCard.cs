using System.Windows.Forms;
using OmenHub.Hardware.Bios;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>GPU power information. Wattage control for the GPU is the next step (clock-ceiling governor).</summary>
    public sealed class GpuPowerCard : CardPanel
    {
        private readonly Label modeLabel;

        public GpuPowerCard()
        {
            Left = 24; Top = 168; Width = 802; Height = 110;
            PageKit.MakeLabel(this, "GPU", 20, 14, 11, true, PageKit.RoleText);
            modeLabel = PageKit.MakeLabel(this, "", 20, 46, 9.5f, false, PageKit.RoleSub);
            PageKit.MakeLabel(this, "Лимит ватт видеокарты задаёт BIOS (режимы Custom TGP / Dynamic Boost). Управление ваттами GPU — следующий этап.",
                20, 78, 8.5f, false, PageKit.RoleSub);
        }

        public void RefreshBiosMode()
        {
            modeLabel.Text = "Режим BIOS:  " + BiosSystemCommands.DescribeGpuPowerMode();
        }
    }
}
