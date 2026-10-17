using System;
using System.Drawing;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Sensors;
using OmenHub.Ui.Controls;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Pages
{
    /// <summary>Live dashboard: fan speeds, temperatures and load.</summary>
    public sealed class OverviewPage : PageBase
    {
        private const int RowSpacing = 56;
        private const int TemperatureScale = 100;
        private const int HeatWarning = 75;
        private const int HeatDanger = 85;

        private Label cpuFanValue, gpuFanValue;
        private MeterBar cpuFanBar, gpuFanBar;
        private MeterBar systemBar, biosBar, gpuTempBar, gpuPowerBar, gpuClockBar, gpuLoadBar, cpuLoadBar, cpuClockBar;
        private Label systemValue, biosValue, gpuTempValue, gpuPowerValue, gpuClockValue, gpuLoadValue, cpuLoadValue, cpuClockValue;

        public OverviewPage()
        {
            BuildFanCards();
            BuildTemperatureCard();
            BuildLoadCard();
        }

        private void BuildFanCards()
        {
            CardPanel cpuCard = PageKit.MakeCard(this, 24, 16, 390, 118, "Кулер CPU");
            cpuFanValue = MakeBigValue(cpuCard);
            cpuFanBar = MakeBar(cpuCard, 94);
            CardPanel gpuCard = PageKit.MakeCard(this, 436, 16, 390, 118, "Кулер GPU");
            gpuFanValue = MakeBigValue(gpuCard);
            gpuFanBar = MakeBar(gpuCard, 94);
        }

        private void BuildTemperatureCard()
        {
            CardPanel card = PageKit.MakeCard(this, 24, 150, 390, 230, "Температуры");
            systemValue = MakeMeterRow(card, 52, "Система (макс. ACPI/BIOS)", out systemBar);
            biosValue = MakeMeterRow(card, 52 + RowSpacing, "BIOS-датчик", out biosBar);
            gpuTempValue = MakeMeterRow(card, 52 + 2 * RowSpacing, "GPU", out gpuTempBar);
        }

        private void BuildLoadCard()
        {
            CardPanel card = PageKit.MakeCard(this, 436, 150, 390, 330, "Нагрузка");
            gpuPowerValue = MakeMeterRow(card, 52, "GPU: мощность", out gpuPowerBar);
            gpuClockValue = MakeMeterRow(card, 52 + RowSpacing, "GPU: частота ядра", out gpuClockBar);
            gpuLoadValue = MakeMeterRow(card, 52 + 2 * RowSpacing, "GPU: загрузка", out gpuLoadBar);
            cpuLoadValue = MakeMeterRow(card, 52 + 3 * RowSpacing, "CPU: загрузка", out cpuLoadBar);
            cpuClockValue = MakeMeterRow(card, 52 + 4 * RowSpacing, "CPU: частота", out cpuClockBar);
        }

        private static Label MakeBigValue(CardPanel card)
        {
            var label = new Label
            {
                Left = 20, Top = 44, Width = 350, Height = 44, Font = UiDrawing.GetFont(26, true),
                Tag = PageKit.RoleText, BackColor = Color.Transparent, Text = "—"
            };
            card.Controls.Add(label);
            return label;
        }

        private static MeterBar MakeBar(CardPanel card, int top)
        {
            var bar = new MeterBar { Left = 20, Top = top, Width = card.Width - 40 };
            card.Controls.Add(bar);
            return bar;
        }

        private static Label MakeMeterRow(CardPanel card, int top, string name, out MeterBar bar)
        {
            PageKit.MakeLabel(card, name, 20, top, 9.5f, false, PageKit.RoleSub);
            Label value = PageKit.MakeValueLabel(card, card.Width - 150, top - 3, 130, 10.5f, ContentAlignment.MiddleRight);
            bar = MakeBar(card, top + 24);
            return value;
        }

        public override void ShowSnapshot(SensorSnapshot snapshot)
        {
            ShowFan(cpuFanValue, cpuFanBar, snapshot.CpuFanRpm);
            ShowFan(gpuFanValue, gpuFanBar, snapshot.GpuFanRpm);
            ShowTemperature(systemValue, systemBar, snapshot.SystemTemperature);
            ShowTemperature(biosValue, biosBar, snapshot.BiosTemperature);
            ShowTemperature(gpuTempValue, gpuTempBar, snapshot.GpuTemperature);
            Show(gpuPowerValue, gpuPowerBar, snapshot.GpuPowerWatts < 0, snapshot.GpuPowerWatts.ToString("0.0") + " Вт", snapshot.GpuPowerWatts / 140.0);
            Show(gpuClockValue, gpuClockBar, snapshot.GpuClockMhz < 0, snapshot.GpuClockMhz + " МГц", snapshot.GpuClockMhz / 3000.0);
            Show(gpuLoadValue, gpuLoadBar, snapshot.GpuUtilizationPercent < 0, snapshot.GpuUtilizationPercent + " %", snapshot.GpuUtilizationPercent / 100.0);
            Show(cpuLoadValue, cpuLoadBar, snapshot.CpuLoadPercent < 0, snapshot.CpuLoadPercent.ToString("0") + " %", snapshot.CpuLoadPercent / 100.0);
            Show(cpuClockValue, cpuClockBar, snapshot.CpuFrequencyMhz < 0, snapshot.CpuFrequencyMhz + " МГц", snapshot.CpuFrequencyMhz / 5100.0);
        }

        private static void ShowFan(Label value, MeterBar bar, int rpm)
        {
            value.Text = rpm < 0 ? "—" : rpm + " об/мин";
            bar.Fraction = Math.Max(0, rpm) / (double)AppSettings.MaximumFanRpm;
            bar.Invalidate();
        }

        private static void ShowTemperature(Label value, MeterBar bar, int celsius)
        {
            Color? warning = celsius >= HeatDanger ? Palette.Bad : celsius >= HeatWarning ? Palette.AccentSecondary : (Color?)null;
            Show(value, bar, celsius < 0, celsius + " °C", celsius / (double)TemperatureScale);
            bar.WarningColor = warning;
        }

        private static void Show(Label value, MeterBar bar, bool unavailable, string text, double fraction)
        {
            value.Text = unavailable ? "—" : text;
            bar.Fraction = unavailable ? 0 : fraction;
            bar.WarningColor = null;
            bar.Invalidate();
        }
    }
}
