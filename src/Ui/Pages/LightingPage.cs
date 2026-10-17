using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using OmenHub.Hardware.Lighting;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>
    /// Lighting laboratory: numbered variants that flash yellow / blue (or toggle the backlight) by different methods.
    /// The user reports which numbers visibly worked; every run is also written to the log.
    /// </summary>
    public sealed class LightingPage : PageBase
    {
        private const int ColumnWidth = 384;
        private const int RowHeight = 42;

        private readonly List<ActionButton> variantButtons = new List<ActionButton>();
        private readonly TextBox resultBox;
        private volatile bool isRunning;

        public LightingPage()
        {
            CardPanel card = PageKit.MakeCard(this, 24, 16, 802, 604, "Подсветка: лаборатория вариантов");
            PageKit.MakeLabel(card, "Жми вариант и смотри на ноутбук (клавиатура, планка, логотип). Скажи мне номера, которые сработали.",
                20, 42, 9, false, PageKit.RoleSub);

            List<LightingVariant> variants = LightingVariantCatalog.Build();
            for (int index = 0; index < variants.Count; index++)
                AddVariantButton(card, variants[index], index);

            int rows = (variants.Count + 1) / 2;
            resultBox = new TextBox
            {
                Left = 20, Top = 76 + rows * RowHeight + 10, Width = 762, Height = 604 - (76 + rows * RowHeight + 10) - 20,
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9), BorderStyle = BorderStyle.None
            };
            card.Controls.Add(resultBox);
        }

        private void AddVariantButton(CardPanel card, LightingVariant variant, int index)
        {
            int column = index % 2;
            int row = index / 2;
            ActionButton button = PageKit.MakeButton(card, variant.Number + ". " + variant.Title,
                20 + column * (ColumnWidth - 6), 76 + row * RowHeight, ColumnWidth - 12, false);
            button.Click += delegate { RunVariant(variant); };
            variantButtons.Add(button);
        }

        private void RunVariant(LightingVariant variant)
        {
            if (isRunning) return;
            isRunning = true;
            SetButtonsEnabled(false);
            AppendResult("[" + variant.Number + "] запуск: " + variant.Title);
            var worker = new Thread(delegate ()
            {
                string result = variant.Execute();
                BeginInvoke(new Action(delegate
                {
                    AppendResult("[" + variant.Number + "] итог: " + result);
                    SetButtonsEnabled(true);
                    isRunning = false;
                }));
            });
            worker.IsBackground = true;
            worker.SetApartmentState(ApartmentState.STA);
            worker.Start();
        }

        private void SetButtonsEnabled(bool enabled)
        {
            foreach (ActionButton button in variantButtons) button.Enabled = enabled;
        }

        private void AppendResult(string line)
        {
            resultBox.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + line + "\r\n");
        }
    }
}
