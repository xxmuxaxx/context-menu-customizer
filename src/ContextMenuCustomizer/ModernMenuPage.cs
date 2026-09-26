using ContextMenuCustomizer.Core;

namespace ContextMenuCustomizer;

/// <summary>Вкладка нового меню Windows 11: выбор вида меню и скрытие пунктов.</summary>
internal sealed class ModernMenuPage : UserControl
{
    private readonly ModernMenuService _service;
    private readonly Action<string> _status;
    private readonly RadioButton _modern = new() { Text = "Новое меню Windows 11 (по умолчанию)", AutoSize = true };
    private readonly RadioButton _classic = new() { Text = "Классическое меню, как в Windows 10 (без «Показать дополнительные параметры»)", AutoSize = true };
    private readonly ListView _list = new()
    {
        Dock = DockStyle.Fill,
        View = View.Details,
        CheckBoxes = true,
        FullRowSelect = true,
        MultiSelect = false,
        HideSelection = false,
    };
    private bool _loading;

    public ModernMenuPage(ModernMenuService service, Action<string> status)
    {
        _service = service;
        _status = status;

        var styleBox = new GroupBox { Text = "Вид контекстного меню", Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(8) };
        var styleLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown };
        styleLayout.Controls.Add(_modern);
        styleLayout.Controls.Add(_classic);
        styleLayout.Controls.Add(Ui.Button("Применить и перезапустить Проводник", (_, _) => ApplyStyle()));
        styleBox.Controls.Add(styleLayout);

        _list.Columns.Add("Пункт нового меню", 330);
        _list.Columns.Add("Источник", 200);
        _list.Columns.Add("Для объектов", 170);
        _list.Columns.Add("CLSID", 290);
        _list.ItemChecked += OnItemChecked;

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        buttons.Controls.Add(Ui.Button("Обновить", (_, _) => Reload()));
        buttons.Controls.Add(Ui.Button("Скрыть по CLSID…", (_, _) => BlockCustom()));
        buttons.Controls.Add(Ui.Button("Перезапустить Проводник", (_, _) => MainForm.RestartExplorer(FindForm()!, _status)));

        var note = new Label
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            MaximumSize = new Size(1000, 0),
            Padding = new Padding(0, 4, 0, 0),
            Text =
                "Снятый флажок скрывает пункт: его CLSID добавляется в «Shell Extensions\\Blocked», и Проводник перестаёт загружать это расширение " +
                "(в новом и в классическом меню). Изменения вступают в силу после перезапуска Проводника.\n" +
                "Новое меню Windows 11 показывает только пункты приложений, зарегистрированных через пакет (MSIX) с интерфейсом IExplorerCommand, — " +
                "обычные команды из реестра туда не попадают. Команды, добавленные на вкладке «Классическое меню», видны в «Показать дополнительные параметры» " +
                "(Shift+F10) или сразу, если выбрать классический вид меню выше.",
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(8) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(styleBox);
        layout.Controls.Add(_list);
        layout.Controls.Add(buttons);
        layout.Controls.Add(note);
        Controls.Add(layout);

        if (!ModernMenuService.IsWindows11)
        {
            layout.Enabled = false;
            Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 32,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = SystemColors.Info,
                ForeColor = SystemColors.InfoText,
                Text = "Эта система — не Windows 11: нового меню здесь нет, используйте вкладку «Классическое меню».",
            });
        }

        Load += (_, _) =>
        {
            if (ModernMenuService.IsWindows11) Reload();
        };
    }

    private void Reload()
    {
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            var classic = _service.IsClassicMenuForced;
            _classic.Checked = classic;
            _modern.Checked = !classic;

            var items = _service.GetItems();
            _loading = true;
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var item in items)
            {
                var row = new ListViewItem([item.Title, item.SourceName, item.Targets, item.Clsid])
                {
                    Tag = item,
                    Checked = !item.IsBlocked,
                };
                if (item.IsBlocked) row.ForeColor = SystemColors.GrayText;
                _list.Items.Add(row);
            }
            _status($"Найдено пунктов нового меню: {items.Count}, скрыто: {items.Count(i => i.IsBlocked)}.");
        }
        catch (Exception e)
        {
            Ui.ShowError(FindForm(), e);
        }
        finally
        {
            _list.EndUpdate();
            _loading = false;
            Cursor.Current = Cursors.Default;
        }
    }

    private void OnItemChecked(object? sender, ItemCheckedEventArgs e)
    {
        if (_loading || e.Item.Tag is not ModernMenuItem item) return;
        try
        {
            _service.SetVisible(item, e.Item.Checked);
            e.Item.ForeColor = e.Item.Checked ? SystemColors.WindowText : SystemColors.GrayText;
            _status($"«{item.Title}» {(e.Item.Checked ? "будет показан" : "будет скрыт")} после перезапуска Проводника.");
        }
        catch (Exception ex)
        {
            _loading = true;
            e.Item.Checked = !e.Item.Checked;
            _loading = false;
            Ui.ShowError(FindForm(), ex);
        }
    }

    private void ApplyStyle()
    {
        try
        {
            _service.SetClassicMenuForced(_classic.Checked);
            _status(_classic.Checked ? "Включено классическое меню." : "Включено новое меню Windows 11.");
            MainForm.RestartExplorer(FindForm()!, _status);
        }
        catch (Exception e)
        {
            Ui.ShowError(FindForm(), e);
        }
    }

    private void BlockCustom()
    {
        var clsid = Ui.Prompt(FindForm()!, "Скрыть расширение по CLSID",
            "Введите CLSID расширения, например {9F156763-7844-4DC4-B2B1-901F640F5155}.\n" +
            "Его можно найти в AppxManifest.xml приложения или в ShellExView.");
        if (string.IsNullOrEmpty(clsid)) return;
        var normalized = ClsidUtil.Normalize(clsid);
        if (normalized == null)
        {
            MessageBox.Show(FindForm(), $"«{clsid}» не похож на CLSID.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            var item = new ModernMenuItem { Title = normalized, Clsid = normalized, Source = ModernItemSource.BlockList };
            _service.SetVisible(item, visible: false);
            Reload();
        }
        catch (Exception e)
        {
            Ui.ShowError(FindForm(), e);
        }
    }
}
