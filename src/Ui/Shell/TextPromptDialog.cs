using System.Drawing;
using System.Windows.Forms;

namespace OmenHub.Ui.Shell
{
    /// <summary>Tiny modal dialog asking for one line of text (used to name a preset).</summary>
    public static class TextPromptDialog
    {
        /// <summary>Returns the typed text, or null when cancelled.</summary>
        public static string Ask(string title, string question)
        {
            using (var dialog = new Form())
            {
                dialog.Text = title;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.MinimizeBox = false; dialog.MaximizeBox = false;
                dialog.ClientSize = new Size(360, 110);
                var label = new Label { Text = question, Left = 12, Top = 12, Width = 336 };
                var input = new TextBox { Left = 12, Top = 38, Width = 336 };
                var ok = new Button { Text = "Сохранить", Left = 168, Top = 72, Width = 90, DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Отмена", Left = 266, Top = 72, Width = 82, DialogResult = DialogResult.Cancel };
                dialog.Controls.AddRange(new Control[] { label, input, ok, cancel });
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;
                return dialog.ShowDialog() == DialogResult.OK ? input.Text.Trim() : null;
            }
        }
    }
}
