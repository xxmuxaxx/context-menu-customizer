using System.Diagnostics;
using ContextMenuCustomizer.Core;

namespace ContextMenuCustomizer;

internal sealed class MainForm : Form
{
    private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly RegistryBackup _backup = new();

    public MainForm()
    {
        Text = "Настройка контекстного меню Windows";
        Size = new Size(1100, 700);
        MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        var blockList = new ShellExtensionBlockList(_backup);
        var classic = new ClassicMenuService(_backup, blockList);
        var modern = new ModernMenuService(_backup, blockList);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var classicTab = new TabPage("Классическое меню (Windows 10 / «Показать дополнительные параметры»)");
        classicTab.Controls.Add(new ClassicMenuPage(classic, SetStatus) { Dock = DockStyle.Fill });
        var modernTab = new TabPage("Новое меню Windows 11");
        modernTab.Controls.Add(new ModernMenuPage(modern, SetStatus) { Dock = DockStyle.Fill });
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

        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(_status);

        Controls.Add(tabs);
        Controls.Add(statusStrip);
        Controls.Add(menu);
        MainMenuStrip = menu;

        SetStatus($"Резервные копии изменяемых разделов сохраняются в {_backup.Directory}");
    }

    private void SetStatus(string text) => _status.Text = text;

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
