using ContextMenuCustomizer.Core;

namespace ContextMenuCustomizer;

/// <summary>Диалог создания и изменения команды или подменю классического меню.</summary>
internal sealed class VerbEditorForm : Form
{
    private readonly ClassicMenuItem? _existing;
    private readonly bool _isSubmenu;
    private readonly bool _isBackground;
    private readonly bool _commandEditable;

    private readonly TextBox _text = new() { Width = 460 };
    private readonly TextBox _keyName = new() { Width = 460 };
    private readonly TextBox _command = new() { Width = 460 };
    private readonly TextBox _icon = new() { Width = 460 };
    private readonly ComboBox _position = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly CheckBox _extended = new() { Text = "Показывать только при Shift + правый клик", AutoSize = true };
    private readonly CheckBox _separatorBefore = new() { Text = "Разделитель перед пунктом", AutoSize = true };
    private readonly CheckBox _separatorAfter = new() { Text = "Разделитель после пункта", AutoSize = true };
    private readonly RadioButton _hiveUser = new() { Text = MenuHive.CurrentUser.DisplayName(), AutoSize = true };
    private readonly RadioButton _hiveMachine = new() { Text = MenuHive.LocalMachine.DisplayName(), AutoSize = true, Checked = true };
    private bool _keyNameTouched;

    public VerbDefinition Definition { get; private set; } = null!;
    public MenuHive Hive => _hiveUser.Checked ? MenuHive.CurrentUser : MenuHive.LocalMachine;

    public VerbEditorForm(ClassicMenuItem? existing, bool isSubmenu, bool isBackground, MenuHive? fixedHive)
    {
        _existing = existing;
        _isSubmenu = isSubmenu;
        _isBackground = isBackground;
        _commandEditable = !isSubmenu && (existing == null || existing.Command != null || existing.DelegateExecute == null);

        Text = (existing == null ? "Новое " : "Изменить ") + (isSubmenu ? "подменю" : "команду");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);
        Font = new Font("Segoe UI", 9F);

        _position.Items.AddRange(["Обычно", "Вверху меню", "Внизу меню"]);
        _position.SelectedIndex = 0;

        var grid = new TableLayoutPanel { AutoSize = true, ColumnCount = 3, Dock = DockStyle.Fill };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddRow(grid, "Текст пункта:", _text);
        AddRow(grid, "Имя раздела реестра:", _keyName);

        if (!isSubmenu)
        {
            AddRow(grid, "Команда:", _command, Ui.Button("Программа…", (_, _) => BrowseProgram()));
            var hint = new Label
            {
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                MaximumSize = new Size(460, 0),
                Text = isBackground
                    ? "%V — папка, в которой щёлкнули. Пример: \"C:\\Tools\\app.exe\" \"%V\""
                    : "%1 — путь к выбранному объекту (для папок можно %V). Пример: notepad.exe \"%1\"",
            };
            var hintRow = grid.RowCount;
            grid.Controls.Add(hint, 1, hintRow);
            grid.SetColumnSpan(hint, 2);
            grid.RowCount = hintRow + 1;
        }

        AddRow(grid, "Иконка:", _icon, Ui.Button("Обзор…", (_, _) => BrowseIcon()));
        AddRow(grid, "Положение:", _position);

        var options = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown };
        options.Controls.Add(_extended);
        options.Controls.Add(_separatorBefore);
        options.Controls.Add(_separatorAfter);
        AddRow(grid, "", options);

        var hivePanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown };
        hivePanel.Controls.Add(_hiveMachine);
        hivePanel.Controls.Add(_hiveUser);
        AddRow(grid, "Для кого:", hivePanel);

        if (fixedHive != null)
        {
            _hiveUser.Checked = fixedHive == MenuHive.CurrentUser;
            _hiveMachine.Checked = fixedHive == MenuHive.LocalMachine;
            hivePanel.Enabled = false;
        }

        if (existing != null)
        {
            _text.Text = existing.DisplayName;
            _keyName.Text = existing.KeyName;
            _keyName.ReadOnly = true;
            _icon.Text = existing.Icon ?? "";
            _extended.Checked = existing.Extended;
            _separatorBefore.Checked = existing.SeparatorBefore;
            _separatorAfter.Checked = existing.SeparatorAfter;
            _position.SelectedIndex = existing.Position?.ToLowerInvariant() switch
            {
                "top" => 1,
                "bottom" => 2,
                _ => 0,
            };
            if (_commandEditable)
            {
                _command.Text = existing.Command ?? "";
            }
            else if (!isSubmenu)
            {
                _command.Text = $"(COM-команда DelegateExecute {existing.DelegateExecute} — не редактируется)";
                _command.ReadOnly = true;
            }
        }
        else
        {
            _text.TextChanged += (_, _) =>
            {
                if (!_keyNameTouched) _keyName.Text = MenuText.SuggestKeyName(_text.Text);
            };
            _keyName.KeyPress += (_, _) => _keyNameTouched = true;
        }

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, AutoSize = true };
        var ok = new Button { Text = "Сохранить", AutoSize = true };
        ok.Click += (_, _) => Accept();
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        var root = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };
        root.Controls.Add(grid);
        root.Controls.Add(buttons);
        Controls.Add(root);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static void AddRow(TableLayoutPanel grid, string label, Control control, Control? extra = null)
    {
        var row = grid.RowCount;
        grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 8, 0) }, 0, row);
        grid.Controls.Add(control, 1, row);
        if (extra != null) grid.Controls.Add(extra, 2, row);
        grid.RowCount = row + 1;
    }

    private void BrowseProgram()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Выберите программу",
            Filter = "Программы (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|Все файлы (*.*)|*.*",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _command.Text = MenuText.BuildCommand(dialog.FileName, _isBackground);
        if (string.IsNullOrWhiteSpace(_icon.Text) && dialog.FileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            _icon.Text = dialog.FileName;
        if (string.IsNullOrWhiteSpace(_text.Text))
            _text.Text = "Открыть в " + Path.GetFileNameWithoutExtension(dialog.FileName);
    }

    private void BrowseIcon()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Выберите иконку",
            Filter = "Иконки и программы (*.ico;*.exe;*.dll)|*.ico;*.exe;*.dll|Все файлы (*.*)|*.*",
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) _icon.Text = dialog.FileName;
    }

    private void Accept()
    {
        var keyError = MenuText.ValidateKeyName(_keyName.Text);
        string? error = keyError;
        if (string.IsNullOrWhiteSpace(_text.Text)) error = "Укажите текст пункта.";
        else if (!_isSubmenu && _commandEditable && string.IsNullOrWhiteSpace(_command.Text)) error = "Укажите команду.";
        if (error != null)
        {
            MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Definition = new VerbDefinition
        {
            KeyName = _keyName.Text.Trim(),
            Text = _text.Text.Trim(),
            Command = _commandEditable ? _command.Text.Trim() : null,
            Icon = _icon.Text.Trim(),
            Position = _position.SelectedIndex switch
            {
                1 => "Top",
                2 => "Bottom",
                _ => null,
            },
            Extended = _extended.Checked,
            SeparatorBefore = _separatorBefore.Checked,
            SeparatorAfter = _separatorAfter.Checked,
            IsSubmenu = _isSubmenu,
        };
        DialogResult = DialogResult.OK;
    }
}
