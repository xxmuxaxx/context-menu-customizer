namespace ContextMenuCustomizer;

internal static class Ui
{
    public static Button Button(string text, EventHandler onClick)
    {
        var button = new Button { Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(6, 2, 6, 2) };
        button.Click += onClick;
        return button;
    }

    public static void ShowError(IWin32Window? owner, Exception e)
    {
        var message = e switch
        {
            UnauthorizedAccessException or System.Security.SecurityException =>
                "Нет прав на изменение этого раздела реестра.\n\n" +
                "Некоторые системные разделы принадлежат TrustedInstaller и защищены даже от администратора.\n\n" + e.Message,
            _ => e.Message,
        };
        MessageBox.Show(owner, message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public static string? Prompt(IWin32Window owner, string title, string label, string initial = "")
    {
        using var form = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12),
        };
        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };
        layout.Controls.Add(new Label { Text = label, AutoSize = true, MaximumSize = new Size(420, 0) });
        var input = new TextBox { Text = initial, Width = 420 };
        layout.Controls.Add(input);
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, AutoSize = true };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        layout.Controls.Add(buttons);
        form.Controls.Add(layout);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        return form.ShowDialog(owner) == DialogResult.OK ? input.Text.Trim() : null;
    }
}
