using ContextMenuCustomizer.Core;

namespace ContextMenuCustomizer;

/// <summary>Вкладка классического меню: список расположений слева, пункты выбранного расположения справа.</summary>
internal sealed class ClassicMenuPage : UserControl
{
    private sealed record Level(string Title, string ShellPath, string? HandlersPath, MenuHive? FixedHive, bool IsBackground);

    private readonly ClassicMenuService _service;
    private readonly Action<string> _status;
    private readonly ListBox _locations = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly ListView _list = new()
    {
        Dock = DockStyle.Fill,
        View = View.Details,
        CheckBoxes = true,
        FullRowSelect = true,
        MultiSelect = false,
        HideSelection = false,
        ShowItemToolTips = true,
    };
    private readonly Label _path = new() { AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
    private readonly Button _back;
    private readonly CheckBox _showHandlers = new() { Text = "Показывать обработчики (shellex)", AutoSize = true, Checked = true };
    private readonly Stack<Level> _levels = new();
    private bool _loading;

    public ClassicMenuPage(ClassicMenuService service, Action<string> status)
    {
        _service = service;
        _status = status;

        foreach (var location in MenuLocation.Standard) _locations.Items.Add(location);
        _locations.SelectedIndexChanged += (_, _) => OpenLocation();

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(4) };
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        left.Controls.Add(new Label { Text = "Где показывать меню:", AutoSize = true, Padding = new Padding(0, 4, 0, 4) });
        left.Controls.Add(_locations);
        left.Controls.Add(Ui.Button("Добавить тип файла…", (_, _) => AddLocation()));

        _list.Columns.Add("Пункт", 260);
        _list.Columns.Add("Тип", 90);
        _list.Columns.Add("Хранится", 70);
        _list.Columns.Add("Команда / CLSID", 380);
        _list.Columns.Add("Параметры", 200);
        _list.ItemChecked += OnItemChecked;
        _list.DoubleClick += (_, _) => OpenOrEdit();
        _list.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete) DeleteSelected();
            else if (e.KeyCode == Keys.Enter) OpenOrEdit();
            else if (e.KeyCode == Keys.Back) GoBack();
        };

        _back = Ui.Button("← Назад", (_, _) => GoBack());
        _back.Enabled = false;
        _showHandlers.CheckedChanged += (_, _) => Reload();

        var top = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false };
        top.Controls.Add(_back);
        top.Controls.Add(_path);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        buttons.Controls.Add(Ui.Button("Добавить команду…", (_, _) => Create(submenu: false)));
        buttons.Controls.Add(Ui.Button("Добавить подменю…", (_, _) => Create(submenu: true)));
        buttons.Controls.Add(Ui.Button("Изменить…", (_, _) => EditSelected()));
        buttons.Controls.Add(Ui.Button("Удалить", (_, _) => DeleteSelected()));
        buttons.Controls.Add(Ui.Button("Обновить", (_, _) => Reload()));
        buttons.Controls.Add(_showHandlers);

        var hint = new Label
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Text = "Флажок — пункт виден в меню. Снятый флажок скрывает пункт без удаления (LegacyDisable для команд, «---» перед CLSID для обработчиков). " +
                   "Двойной щелчок по подменю открывает его содержимое.",
            MaximumSize = new Size(900, 0),
            Padding = new Padding(0, 4, 0, 0),
        };

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(4) };
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.Controls.Add(top);
        right.Controls.Add(_list);
        right.Controls.Add(buttons);
        right.Controls.Add(hint);

        var split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1 };
        split.Panel1.Controls.Add(left);
        split.Panel2.Controls.Add(right);
        Controls.Add(split);

        Load += (_, _) =>
        {
            split.SplitterDistance = 230;
            _locations.SelectedIndex = 0;
        };
    }

    private Level? Current => _levels.Count > 0 ? _levels.Peek() : null;

    private void OpenLocation()
    {
        if (_locations.SelectedItem is not MenuLocation location) return;
        _levels.Clear();
        _levels.Push(new Level(location.Title, location.ShellPath, location.HandlersPath, null, location.IsBackground));
        Reload();
    }

    private void AddLocation()
    {
        var input = Ui.Prompt(FindForm()!, "Добавить тип файла",
            "Введите расширение (например, .txt или .png) — пункты будут показываться только для таких файлов.\n" +
            "Также можно ввести имя класса из HKEY_CLASSES_ROOT, например txtfile или Directory.");
        if (string.IsNullOrEmpty(input)) return;
        try
        {
            var location = MenuLocation.FromUserInput(input);
            var existing = _locations.Items.Cast<MenuLocation>()
                .FirstOrDefault(l => l.ClassPath.Equals(location.ClassPath, StringComparison.OrdinalIgnoreCase));
            if (existing == null) _locations.Items.Add(existing = location);
            _locations.SelectedItem = existing;
        }
        catch (ArgumentException e)
        {
            Ui.ShowError(FindForm(), e);
        }
    }

    private void Reload()
    {
        var level = Current;
        if (level == null) return;

        _back.Enabled = _levels.Count > 1;
        _path.Text = string.Join("  ›  ", _levels.Reverse().Select(l => l.Title)) + $"      (HKCR\\{level.ShellPath})";

        var items = new List<ClassicMenuItem>();
        try
        {
            items.AddRange(_service.GetCommands(level.ShellPath));
            if (_showHandlers.Checked && level.HandlersPath != null)
                items.AddRange(_service.GetHandlers(level.HandlersPath));
        }
        catch (Exception e)
        {
            Ui.ShowError(FindForm(), e);
        }

        _loading = true;
        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (var item in items
                         .OrderBy(i => i.Kind == ClassicItemKind.Handler)
                         .ThenBy(i => i.DisplayName, StringComparer.CurrentCultureIgnoreCase))
            {
                _list.Items.Add(CreateRow(item));
            }
        }
        finally
        {
            _list.EndUpdate();
            _loading = false;
        }
        _status($"{items.Count} пунктов в HKCR\\{level.ShellPath}");
    }

    private static ListViewItem CreateRow(ClassicMenuItem item)
    {
        var kind = item.Kind switch
        {
            ClassicItemKind.Command => "Команда",
            ClassicItemKind.Submenu => "Подменю",
            _ => "Обработчик",
        };

        var flags = new List<string>();
        if (item.Extended) flags.Add("только с Shift");
        if (item.Position is { Length: > 0 } position)
            flags.Add(position.Equals("Top", StringComparison.OrdinalIgnoreCase) ? "вверху" : position.Equals("Bottom", StringComparison.OrdinalIgnoreCase) ? "внизу" : position);
        if (item.ProgrammaticAccessOnly) flags.Add("скрыт приложением");
        if (item.GloballyBlocked) flags.Add("CLSID заблокирован");
        if (item.Icon is { Length: > 0 }) flags.Add("иконка");

        var row = new ListViewItem([item.DisplayName, kind, item.Hive.ShortName(), item.Target, string.Join(", ", flags)])
        {
            Tag = item,
            Checked = item.Enabled,
            ToolTipText = item.FullRegistryPath,
        };
        if (!item.Enabled) row.ForeColor = SystemColors.GrayText;
        if (item.Kind == ClassicItemKind.Submenu) row.Font = new Font(row.Font ?? SystemFonts.DefaultFont, FontStyle.Bold);
        return row;
    }

    private ClassicMenuItem? Selected => _list.SelectedItems.Count > 0 ? _list.SelectedItems[0].Tag as ClassicMenuItem : null;

    private void OnItemChecked(object? sender, ItemCheckedEventArgs e)
    {
        if (_loading || e.Item.Tag is not ClassicMenuItem item) return;
        try
        {
            _service.SetEnabled(item, e.Item.Checked);
            _status($"«{item.DisplayName}» {(e.Item.Checked ? "показан" : "скрыт")}.");
            BeginInvoke(Reload);
        }
        catch (Exception ex)
        {
            _loading = true;
            e.Item.Checked = !e.Item.Checked;
            _loading = false;
            Ui.ShowError(FindForm(), ex);
        }
    }

    private void OpenOrEdit()
    {
        var item = Selected;
        if (item == null) return;
        if (item.Kind == ClassicItemKind.Submenu && item.SubmenuShellPath != null)
        {
            var parent = Current!;
            _levels.Push(new Level(item.DisplayName, item.SubmenuShellPath, null, item.SubmenuHive, parent.IsBackground));
            Reload();
        }
        else
        {
            EditSelected();
        }
    }

    private void GoBack()
    {
        if (_levels.Count <= 1) return;
        _levels.Pop();
        Reload();
    }

    private void Create(bool submenu)
    {
        var level = Current;
        if (level == null) return;
        using var editor = new VerbEditorForm(null, submenu, level.IsBackground, level.FixedHive);
        if (editor.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            _service.Save(editor.Hive, level.ShellPath, editor.Definition, isNew: true);
            _status($"Пункт «{editor.Definition.Text}» добавлен в {editor.Hive.ShortName()}\\Software\\Classes\\{level.ShellPath}.");
            Reload();
        }
        catch (Exception e)
        {
            Ui.ShowError(FindForm(), e);
        }
    }

    private void EditSelected()
    {
        var item = Selected;
        var level = Current;
        if (item == null || level == null) return;
        if (!item.IsEditable)
        {
            MessageBox.Show(FindForm(),
                "Обработчики shellex — это COM-объекты сторонних программ, их текст задаёт сама программа.\n" +
                "Их можно только скрыть (снять флажок) или удалить.",
                "Изменение недоступно", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var editor = new VerbEditorForm(item, item.Kind == ClassicItemKind.Submenu, level.IsBackground, item.Hive);
        if (editor.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            var shellPath = item.KeyPath[..item.KeyPath.LastIndexOf('\\')];
            _service.Save(item.Hive, shellPath, editor.Definition, isNew: false);
            _status($"Пункт «{editor.Definition.Text}» сохранён.");
            Reload();
        }
        catch (Exception e)
        {
            Ui.ShowError(FindForm(), e);
        }
    }

    private void DeleteSelected()
    {
        var item = Selected;
        if (item == null) return;
        var question = $"Удалить «{item.DisplayName}»?\n\n{item.FullRegistryPath}\n\n" +
                       "Перед удалением будет создана резервная копия (.reg). Если нужно лишь убрать пункт из меню, лучше снять флажок.";
        if (MessageBox.Show(FindForm(), question, "Удаление", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        try
        {
            var backupFile = _service.Delete(item);
            _status(backupFile != null ? $"Удалено. Резервная копия: {backupFile}" : "Удалено (резервную копию создать не удалось).");
            Reload();
        }
        catch (Exception e)
        {
            Ui.ShowError(FindForm(), e);
        }
    }
}
