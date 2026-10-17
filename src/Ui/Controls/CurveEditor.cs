using System;
using System.Collections.Generic;
using System.Windows.Forms;
using OmenHub.Core.Settings;

namespace OmenHub.Ui.Controls
{
    /// <summary>
    /// Interactive fan curve: drag a point, double-click empty space to add one, right-click a point to remove it.
    /// Painting lives in CurveEditorPainting.cs.
    /// </summary>
    public partial class CurveEditor : Control
    {
        private const int GrabRadius = 14;
        private const int RpmStep = 100;
        private int draggedIndex = -1;

        public FanCurve Curve = FanCurve.CreateDefault();
        public double CurrentTemperature = -1;
        public event EventHandler Edited;

        public CurveEditor()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        private int TemperatureAt(int mouseX)
        {
            double fraction = (mouseX - LeftMargin) / (double)(Width - LeftMargin - RightMargin);
            return (int)Math.Round(MinimumTemperature + fraction * (MaximumTemperature - MinimumTemperature));
        }

        private int RpmAtY(int mouseY)
        {
            double fraction = 1 - (mouseY - TopMargin) / (double)(Height - TopMargin - BottomMargin);
            int rpm = (int)Math.Round(fraction * MaximumRpm / RpmStep) * RpmStep;
            return Math.Max(0, Math.Min(MaximumRpm, rpm));
        }

        private int FindPointNear(int mouseX, int mouseY)
        {
            int nearest = -1;
            double nearestDistance = GrabRadius * GrabRadius;
            for (int index = 0; index < Curve.Temperatures.Length; index++)
            {
                double deltaX = XOf(Curve.Temperatures[index]) - mouseX;
                double deltaY = YOf(Curve.Rpms[index]) - mouseY;
                double distance = deltaX * deltaX + deltaY * deltaY;
                if (distance < nearestDistance) { nearestDistance = distance; nearest = index; }
            }
            return nearest;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int index = FindPointNear(e.X, e.Y);
            if (e.Button == MouseButtons.Right) { RemovePoint(index); return; }
            draggedIndex = index;
            if (draggedIndex >= 0) { Capture = true; Invalidate(); }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (draggedIndex < 0) return;
            int[] temperatures = Curve.Temperatures;
            int lowest = draggedIndex == 0 ? MinimumTemperature : temperatures[draggedIndex - 1] + 1;
            int highest = draggedIndex == temperatures.Length - 1 ? MaximumTemperature : temperatures[draggedIndex + 1] - 1;
            temperatures[draggedIndex] = Math.Max(lowest, Math.Min(highest, TemperatureAt(e.X)));
            Curve.Rpms[draggedIndex] = RpmAtY(e.Y);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (draggedIndex < 0) return;
            draggedIndex = -1;
            Capture = false;
            Invalidate();
            RaiseEdited();
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || Curve.Temperatures.Length >= FanCurve.MaximumPoints) return;
            int temperature = Math.Max(MinimumTemperature + 1, Math.Min(MaximumTemperature - 1, TemperatureAt(e.X)));
            if (Array.IndexOf(Curve.Temperatures, temperature) >= 0) return;

            var temperatures = new List<int>(Curve.Temperatures);
            var rpms = new List<int>(Curve.Rpms);
            int insertAt = 0;
            while (insertAt < temperatures.Count && temperatures[insertAt] < temperature) insertAt++;
            temperatures.Insert(insertAt, temperature);
            rpms.Insert(insertAt, RpmAtY(e.Y));
            Curve.Rpms = rpms.ToArray();
            Curve.Temperatures = temperatures.ToArray();
            RaiseEdited();
        }

        private void RemovePoint(int index)
        {
            if (index < 0 || Curve.Temperatures.Length <= FanCurve.MinimumPoints) return;
            var temperatures = new List<int>(Curve.Temperatures);
            var rpms = new List<int>(Curve.Rpms);
            temperatures.RemoveAt(index);
            rpms.RemoveAt(index);
            Curve.Rpms = rpms.ToArray();
            Curve.Temperatures = temperatures.ToArray();
            RaiseEdited();
        }

        private void RaiseEdited()
        {
            Invalidate();
            if (Edited != null) Edited(this, EventArgs.Empty);
        }
    }
}
