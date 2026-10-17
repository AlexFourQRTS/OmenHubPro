using System.Drawing;
using System.Windows.Forms;
using OmenHub.Ui.Controls;
using OmenHub.Ui.Theme;

namespace OmenHub.Ui.Pages
{
    /// <summary>Factory helpers so pages stay short. Label roles ("text" / "sub") are coloured by ThemeApplier.</summary>
    public static class PageKit
    {
        public const string RoleText = ThemeApplier.RoleText;
        public const string RoleSub = ThemeApplier.RoleSub;

        public static Label MakeLabel(Control parent, string text, int left, int top, float fontSize, bool bold, string role)
        {
            var label = new Label
            {
                Text = text, Left = left, Top = top, AutoSize = true,
                Font = UiDrawing.GetFont(fontSize, bold), Tag = role, BackColor = Color.Transparent
            };
            parent.Controls.Add(label);
            return label;
        }

        public static Label MakeValueLabel(Control parent, int left, int top, int width, float fontSize, ContentAlignment alignment)
        {
            var label = new Label
            {
                Left = left, Top = top, Width = width, Height = 24, TextAlign = alignment,
                Font = UiDrawing.GetFont(fontSize, true), Tag = RoleText, BackColor = Color.Transparent, Text = "—"
            };
            parent.Controls.Add(label);
            return label;
        }

        public static CardPanel MakeCard(Control page, int left, int top, int width, int height, string heading)
        {
            var card = new CardPanel { Left = left, Top = top, Width = width, Height = height };
            page.Controls.Add(card);
            if (heading != null) MakeLabel(card, heading, 20, 14, 11, true, RoleText);
            return card;
        }

        public static ActionButton MakeButton(Control parent, string text, int left, int top, int width, bool accent)
        {
            var button = new ActionButton { Text = text, Left = left, Top = top, Width = width, IsAccent = accent };
            parent.Controls.Add(button);
            return button;
        }

        public static SegmentedControl MakeSegments(Control parent, int left, int top, int width, string[] items, int selected)
        {
            var segments = new SegmentedControl { Left = left, Top = top, Width = width, Items = items, SelectedIndex = selected };
            parent.Controls.Add(segments);
            return segments;
        }

        public static ValueSlider MakeSlider(Control parent, int left, int top, int width, int minimum, int maximum, int step, int value)
        {
            var slider = new ValueSlider { Left = left, Top = top, Width = width, Minimum = minimum, Maximum = maximum, Step = step };
            slider.Value = value;
            parent.Controls.Add(slider);
            return slider;
        }

        public static string Shorten(string text, int maximumLength)
        {
            string flat = text.Replace("\r", " ").Replace("\n", " ");
            return flat.Length > maximumLength ? flat.Substring(0, maximumLength) + "…" : flat;
        }
    }
}
