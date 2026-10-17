using System.Windows.Forms;
using OmenHub.Ui.Controls;


namespace OmenHub.Ui.Theme
{
    /// <summary>Walks the control tree and recolours everything for the current Palette.</summary>
    public static class ThemeApplier
    {
        public const string RoleSidebar = "side";
        public const string RoleHeader = "head";
        public const string RolePage = "page";
        public const string RoleText = "text";
        public const string RoleSub = "sub";

        public static void Apply(Control root)
        {
            Recolor(root);
            root.Invalidate(true);
        }

        private static void Recolor(Control control)
        {
            string role = control.Tag as string;

            if (control is Form) control.BackColor = Palette.Background;
            else if (control is CardPanel) control.BackColor = Palette.CardSurface;
            else if (role == RoleSidebar) control.BackColor = Palette.Sidebar;
            else if (role == RoleHeader || role == RolePage) control.BackColor = Palette.Background;
            else if (control is TextBox) { control.BackColor = Palette.Track; control.ForeColor = Palette.Text; }

            var label = control as Label;
            if (label != null)
                label.ForeColor = role == RoleSub ? Palette.SubText : Palette.Text;
            else if (IsCustomDrawn(control))
            {
                if (control.Parent != null) control.BackColor = control.Parent.BackColor;
                control.Invalidate();
            }

            foreach (Control child in control.Controls) Recolor(child);
        }

        private static bool IsCustomDrawn(Control control)
        {
            return control is NavButton || control is ActionButton || control is SegmentedControl
                || control is ValueSlider || control is MeterBar || control is CurveEditor || control is StatChip;
        }
    }
}
