using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using OmenHub.Core.Settings;
using OmenHub.Hardware.Fans;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Controls
{
    public partial class CurveEditor
    {
        private const int LeftMargin = 56;
        private const int TopMargin = 12;
        private const int RightMargin = 16;
        private const int BottomMargin = 30;
        private const int MinimumTemperature = 30;
        private const int MaximumTemperature = 100;
        private const int MaximumRpm = AppSettings.MaximumFanRpm;
        private const int RpmGridStep = 1500;
        private const int TemperatureGridStep = 10;

        private float XOf(double temperature)
        {
            return (float)(LeftMargin + (temperature - MinimumTemperature) / (MaximumTemperature - MinimumTemperature)
                * (Width - LeftMargin - RightMargin));
        }

        private float YOf(double rpm)
        {
            return (float)(TopMargin + (1 - rpm / MaximumRpm) * (Height - TopMargin - BottomMargin));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            UiDrawing.SetHighQuality(e.Graphics);
            if (Parent != null) e.Graphics.Clear(Parent.BackColor);
            PaintGrid(e.Graphics);
            PaintAlertZone(e.Graphics);
            PaintCurrentTemperature(e.Graphics);
            PaintCurve(e.Graphics);
        }

        private void PaintGrid(Graphics graphics)
        {
            using (var gridPen = new Pen(Palette.Border))
            {
                for (int rpm = 0; rpm <= MaximumRpm; rpm += RpmGridStep)
                {
                    graphics.DrawLine(gridPen, LeftMargin, YOf(rpm), Width - RightMargin, YOf(rpm));
                    TextRenderer.DrawText(graphics, rpm.ToString(), UiDrawing.GetFont(8, false),
                        new Rectangle(0, (int)YOf(rpm) - 8, LeftMargin - 6, 16), Palette.SubText,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                }
                for (int temperature = 30; temperature <= MaximumTemperature; temperature += TemperatureGridStep)
                {
                    graphics.DrawLine(gridPen, XOf(temperature), TopMargin, XOf(temperature), Height - BottomMargin);
                    TextRenderer.DrawText(graphics, temperature + "°", UiDrawing.GetFont(8, false),
                        new Rectangle((int)XOf(temperature) - 20, Height - 26, 40, 16), Palette.SubText, TextFormatFlags.HorizontalCenter);
                }
            }
        }

        private void PaintAlertZone(Graphics graphics)
        {
            float alertLeft = XOf(ThermalAlert.ActivationCelsius);
            using (var zoneBrush = new SolidBrush(Color.FromArgb(34, Palette.Bad)))
                graphics.FillRectangle(zoneBrush, alertLeft, TopMargin, Width - RightMargin - alertLeft, Height - BottomMargin - TopMargin);
            TextRenderer.DrawText(graphics, "аварийный обдув ≥ 80°", UiDrawing.GetFont(8, true),
                new Rectangle((int)alertLeft + 4, TopMargin + 2, 150, 16), Palette.Bad, TextFormatFlags.Left);
        }

        private void PaintCurrentTemperature(Graphics graphics)
        {
            if (CurrentTemperature < MinimumTemperature) return;
            float x = XOf(Math.Min(CurrentTemperature, MaximumTemperature));
            using (var marker = new Pen(Palette.AccentSecondary, 1.5f) { DashStyle = DashStyle.Dash })
                graphics.DrawLine(marker, x, TopMargin, x, Height - BottomMargin);
        }

        private void PaintCurve(Graphics graphics)
        {
            int pointCount = Math.Min(Curve.Temperatures.Length, Curve.Rpms.Length);
            if (pointCount < 2) return;
            var points = new PointF[pointCount];
            for (int index = 0; index < pointCount; index++)
                points[index] = new PointF(XOf(Curve.Temperatures[index]), YOf(Curve.Rpms[index]));

            using (var linePen = new Pen(Palette.Accent, 3) { LineJoin = LineJoin.Round })
                graphics.DrawLines(linePen, points);
            for (int index = 0; index < pointCount; index++)
            {
                using (var fill = new SolidBrush(index == draggedIndex ? Palette.AccentSecondary : Color.White))
                    graphics.FillEllipse(fill, points[index].X - 7, points[index].Y - 7, 14, 14);
                using (var outline = new Pen(Palette.Accent, 3))
                    graphics.DrawEllipse(outline, points[index].X - 6.5f, points[index].Y - 6.5f, 13, 13);
            }
            if (draggedIndex >= 0)
                TextRenderer.DrawText(graphics, Curve.Temperatures[draggedIndex] + "° → " + Curve.Rpms[draggedIndex] + " об/мин",
                    UiDrawing.GetFont(9, true), new Rectangle((int)points[draggedIndex].X - 70, (int)points[draggedIndex].Y - 30, 140, 18),
                    Palette.Text, TextFormatFlags.HorizontalCenter);
        }
    }
}
