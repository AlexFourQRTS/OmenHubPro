using System.Drawing;
using System.Windows.Forms;
using OmenHub.Hardware.Lighting;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>
    /// Finder tool: lights only the lamps between two indices (white) and switches all others off,
    /// to learn which of the keyboard's 120 lamp indices are keys and which belong to the light bar.
    /// </summary>
    public sealed class LampRangeCard : CardPanel
    {
        private const int LastLampIndex = 119;
        private readonly ValueSlider firstSlider;
        private readonly ValueSlider lastSlider;
        private readonly Label rangeLabel;
        private readonly Label statusLabel;
        private DirectLampArray keyboard;

        public LampRangeCard()
        {
            Left = 24; Top = 16; Width = 802; Height = 150;
            PageKit.MakeLabel(this, "Поиск ламп: показать только диапазон", 20, 14, 11, true, PageKit.RoleText);
            PageKit.MakeLabel(this, "С", 20, 52, 9.5f, false, PageKit.RoleSub);
            PageKit.MakeLabel(this, "По", 20, 82, 9.5f, false, PageKit.RoleSub);
            firstSlider = PageKit.MakeSlider(this, 60, 46, 560, 0, LastLampIndex, 1, 100);
            lastSlider = PageKit.MakeSlider(this, 60, 76, 560, 0, LastLampIndex, 1, LastLampIndex);
            rangeLabel = PageKit.MakeValueLabel(this, 630, 60, 150, 10.5f, ContentAlignment.MiddleRight);
            firstSlider.ValueChanged += delegate { ShowRangeText(); };
            lastSlider.ValueChanged += delegate { ShowRangeText(); };

            ActionButton showButton = PageKit.MakeButton(this, "Показать диапазон", 20, 108, 190, true);
            ActionButton darkButton = PageKit.MakeButton(this, "Погасить все", 222, 108, 150, false);
            ActionButton releaseButton = PageKit.MakeButton(this, "Вернуть клавиатуре", 384, 108, 190, false);
            statusLabel = PageKit.MakeLabel(this, "", 590, 116, 8.5f, false, PageKit.RoleSub);
            showButton.Click += delegate { ShowRange(); };
            darkButton.Click += delegate { PaintLamps(delegate (int lampIndex) { return false; }, "все погашены"); };
            releaseButton.Click += delegate { ReleaseKeyboard(); };
            ShowRangeText();
        }

        private void ShowRangeText()
        {
            rangeLabel.Text = firstSlider.Value + " … " + lastSlider.Value;
        }

        private void ShowRange()
        {
            int first = firstSlider.Value;
            int last = lastSlider.Value;
            PaintLamps(delegate (int lampIndex) { return lampIndex >= first && lampIndex <= last; },
                "светятся лампы " + first + "…" + last);
        }

        private void PaintLamps(System.Func<int, bool> isLit, string description)
        {
            if (keyboard == null)
            {
                keyboard = DirectLampArray.Open();
                if (keyboard == null) { statusLabel.Text = "клавиатура не открылась"; return; }
                keyboard.SetAutonomous(false);
            }
            bool accepted = keyboard.PaintEach(delegate (int lampIndex)
            {
                return isLit(lampIndex) ? new byte[] { 255, 255, 255 } : new byte[] { 0, 0, 0 };
            });
            statusLabel.Text = accepted ? description : "устройство отклонило команду";
        }

        private void ReleaseKeyboard()
        {
            if (keyboard == null) return;
            keyboard.SetAutonomous(true);
            keyboard.Dispose();
            keyboard = null;
            statusLabel.Text = "клавиатура вернулась к своим эффектам";
        }
    }
}
