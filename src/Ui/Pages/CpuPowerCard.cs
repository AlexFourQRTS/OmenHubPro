using System;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Engine;
using OmenHub.Hardware.Power;
using OmenHub.Ui.Controls;

namespace OmenHub.Ui.Pages
{
    /// <summary>CPU power limit (20-75 W, step 5) through RyzenAdj, plus its read-out and a load benchmark.</summary>
    public sealed class CpuPowerCard : CardPanel
    {
        private readonly ValueSlider wattsSlider;
        private readonly Label wattsLabel;
        private readonly TextBox outputBox;
        private readonly ActionButton benchmarkButton;

        public CpuPowerCard()
        {
            Left = 24; Top = 294; Width = 802; Height = 334;
            PageKit.MakeLabel(this, "CPU: лимит мощности (RyzenAdj)", 20, 14, 11, true, PageKit.RoleText);
            PageKit.MakeLabel(this, "Диапазон 20–75 Вт, шаг 5. Использует tools\\ryzenadj\\ryzenadj.exe (драйвер WinRing0) — запускается только по кнопкам ниже.",
                20, 42, 8.5f, false, PageKit.RoleSub);
            wattsLabel = PageKit.MakeValueLabel(this, 660, 74, 120, 10.5f, ContentAlignment.MiddleRight);
            wattsSlider = PageKit.MakeSlider(this, 20, 74, 630, AppSettings.MinimumCpuWatts, AppSettings.MaximumCpuWatts,
                AppSettings.CpuWattsStep, AppSettings.CpuWatts);
            wattsSlider.ValueChanged += delegate { OnSliderMoved(); };

            ActionButton applyButton = PageKit.MakeButton(this, "Применить", 20, 118, 140, true);
            ActionButton readButton = PageKit.MakeButton(this, "Прочитать (ryzenadj -i)", 172, 118, 220, false);
            benchmarkButton = PageKit.MakeButton(this, "Автотест 35 → 75 Вт", 404, 118, 230, false);
            outputBox = new TextBox
            {
                Left = 20, Top = 166, Width = 762, Height = 150, Multiline = true, ReadOnly = true,
                ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9), BorderStyle = BorderStyle.None
            };
            Controls.Add(outputBox);

            applyButton.Click += delegate { ApplyAndVerify(); };
            readButton.Click += delegate { outputBox.Text = RunRyzenAdj("-i"); };
            benchmarkButton.Click += delegate { StartBenchmark(); };
            LoadFromSettings();
        }

        public void LoadFromSettings()
        {
            wattsSlider.Value = AppSettings.ProAutoEnabled ? AppSettings.ProAutoWatts : AppSettings.CpuWatts;
            wattsLabel.Text = wattsSlider.Value + " Вт";
        }

        private void OnSliderMoved()
        {
            if (AppSettings.ProAutoEnabled) { LoadFromSettings(); return; }   // locked while the profile is active
            AppSettings.CpuWatts = wattsSlider.Value;
            wattsLabel.Text = wattsSlider.Value + " Вт";
            AppSettings.MarkChanged();
        }

        private void ApplyAndVerify()
        {
            string setResult = RyzenAdjTool.SetPowerLimit(wattsSlider.Value);
            int exitCode;
            string table = RyzenAdjTool.Run("-i", out exitCode);
            var report = new StringBuilder();
            report.AppendLine("Запрошено: " + wattsSlider.Value + " Вт");
            report.AppendLine("Ответ установки: " + setResult.Replace("\r\n", " | "));
            report.AppendLine("--- реальные лимиты после установки ---");
            foreach (string line in table.Split('\n'))
                if (IsInteresting(line)) report.AppendLine(line.TrimEnd('\r'));
            outputBox.Text = report.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        private static bool IsInteresting(string line)
        {
            string[] keywords = { "LIMIT", "VALUE", "TEMP", "error", "fail", "unsupport" };
            foreach (string keyword in keywords)
                if (line.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static string RunRyzenAdj(string arguments)
        {
            int exitCode;
            string output = RyzenAdjTool.Run(arguments, out exitCode);
            if (exitCode != 0) output = "[код выхода " + exitCode + "] " + output;
            return output.Length == 0 ? "(готово, вывода нет)" : output.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        private void StartBenchmark()
        {
            benchmarkButton.Enabled = false;
            outputBox.Text = "Тест идёт (~40 с): нагрузка на все ядра, замеры в logs\\omenhub.log ...";
            var worker = new Thread(delegate ()
            {
                string summary = new PowerBenchmark(() => ControlEngine.Latest).Run();
                BeginInvoke(new Action(delegate
                {
                    outputBox.Text = summary.Replace("\r\n", "\n").Replace("\n", "\r\n");
                    benchmarkButton.Enabled = true;
                }));
            });
            worker.IsBackground = true;
            worker.Start();
        }
    }
}
