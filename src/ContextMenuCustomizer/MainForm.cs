using System.Diagnostics;
using ContextMenuCustomizer.Core;

namespace ContextMenuCustomizer;

internal sealed class MainForm : Form
{
    private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly RegistryBackup _backup = new();
    private readonly ModernMenuService _modern;
    private readonly ModernMenuPage _modernPage;
    private readonly ToolStripButton _modernToggle = new()
    {
        CheckOnClick = false,
        DisplayStyle = ToolStripItemDisplayStyle.Text,
        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
    };

    public MainForm()
    {
        Text = "Настройка контекстного меню Windows";
        Size = new Size(1100, 700);
        MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        var blockList = new ShellExtensionBlockList(_backup);
        var classic = new ClassicMenuService(_backup, blockList);
        var modern = _modern = new ModernMenuService(_backup, blockList);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var classicTab = new TabPage("Классическое меню (Windows 10 / «Показать дополнительные параметры»)");
        classicTab.Controls.Add(new ClassicMenuPage(classic, SetStatus) { Dock = DockStyle.Fill });
        var modernTab = new TabPage("Новое меню Windows 11");
        _modernPage = new ModernMenuPage(modern, SetStatus) { Dock = DockStyle.Fill };
        _modernPage.StyleChanged += _ => UpdateModernToggle();
        modernTab.Controls.Add(_modernPage);
        tabs.TabPages.Add(classicTab);
        tabs.TabPages.Add(modernTab);

        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("Файл");
        file.DropDownItems.Add("Перезапустить Проводник", null, (_, _) => RestartExplorer(this, SetStatus));
        file.DropDownItems.Add("Открыть папку резервных копий", null, (_, _) => OpenBackups());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("Выход", null, (_, _) => Close());
        var help = new ToolStripMenuItem("Справка");
        help.DropDownItems.Add("О программе", null, (_, _) => ShowAbout());
        menu.Items.Add(file);
        menu.Items.Add(help);

        var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(6, 2, 6, 2) };
        toolbar.Items.Add(new ToolStripLabel("Вид контекстного меню Windows 11:"));
        toolbar.Items.Add(_modernToggle);
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(new ToolStripButton("Перезапустить Проводник", null, (_, _) => RestartExplorer(this, SetStatus)));
        _modernToggle.Click += (_, _) => ToggleModernMenu();
        UpdateModernToggle();

        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(_status);

        Controls.Add(tabs);
        Controls.Add(statusStrip);
        Controls.Add(toolbar);
        Controls.Add(menu);
        MainMenuStrip = menu;

        SetStatus($"Резервные копии изменяемых разделов сохраняются в {_backup.Directory}");
    }

    private void SetStatus(string text) => _status.Text = text;

    private void UpdateModernToggle()
    {
        if (!ModernMenuService.IsWindows11)
        {
            _modernToggle.Text = "недоступно (не Windows 11)";
            _modernToggle.Enabled = false;
            return;
        }
        var classic = _modern.IsClassicMenuForced;
        _modernToggle.Checked = !classic;
        _modernToggle.Text = classic ? "○ Классическое  —  включить новое" : "● Новое (Windows 11)  —  включить классическое";
        _modernToggle.ToolTipText = classic
            ? "Сейчас меню как в Windows 10. Нажмите, чтобы вернуть новое меню Windows 11."
            : "Сейчас новое меню Windows 11. Нажмите, чтобы сразу показывать классическое меню.";
        _modernPage.ShowStyle(classic);
    }

    private void ToggleModernMenu()
    {
        try
        {
            var classic = !_modern.IsClassicMenuForced;
            _modern.SetClassicMenuForced(classic);
            UpdateModernToggle();
            SetStatus(classic ? "Включено классическое меню." : "Включено новое меню Windows 11.");
            RestartExplorer(this, SetStatus);
        }
        catch (Exception e)
        {
            Ui.ShowError(this, e);
        }
    }

    public static void RestartExplorer(IWin32Window owner, Action<string> status)
    {
        if (MessageBox.Show(owner, "Перезапустить Проводник? Открытые окна папок будут закрыты.", "Перезапуск Проводника",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
            return;
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            ExplorerRestarter.Restart();
            status("Проводник перезапущен.");
        }
        catch (Exception e)
        {
            Ui.ShowError(owner, e);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private void OpenBackups()
    {
        Directory.CreateDirectory(_backup.Directory);
        Process.Start(new ProcessStartInfo(_backup.Directory) { UseShellExecute = true });
    }

    private void ShowAbout() => MessageBox.Show(this,
        "Context Menu Customizer 1.0\n\n" +
        "• Классическое меню: добавление, изменение, скрытие и удаление команд, подменю и обработчиков shellex.\n" +
        "• Windows 11: переключение между новым и классическим меню, скрытие пунктов нового меню.\n\n" +
        "Перед удалением и изменением разделов создаются .reg-файлы резервных копий — их можно вернуть двойным щелчком.",
        "О программе", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
