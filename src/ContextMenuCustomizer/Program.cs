using ContextMenuCustomizer.Core;

namespace ContextMenuCustomizer;

internal static class Program
{
    private const string Usage =
        "Ключи командной строки:\n\n" +
        "  --modern    включить новое меню Windows 11\n" +
        "  --classic   включить классическое меню (как в Windows 10)\n" +
        "  --toggle    переключить вид меню\n" +
        "  --no-restart  не перезапускать Проводник\n\n" +
        "Без ключей открывается окно настройки.";

    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (args.Length > 0)
            return RunCommandLine(args);

        Application.Run(new MainForm());
        return 0;
    }

    private static int RunCommandLine(string[] args)
    {
        var options = args.Select(a => a.Trim().TrimStart('-', '/').ToLowerInvariant()).ToHashSet();
        var noRestart = options.Remove("no-restart");
        if (options.Count != 1 || options.Single() is not ("modern" or "classic" or "toggle"))
        {
            MessageBox.Show(Usage, "Context Menu Customizer", MessageBoxButtons.OK,
                options.Contains("help") || options.Contains("?") ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            return options.Contains("help") || options.Contains("?") ? 0 : 2;
        }

        if (!ModernMenuService.IsWindows11)
        {
            MessageBox.Show("Переключение вида меню доступно только в Windows 11.", "Context Menu Customizer",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return 1;
        }

        try
        {
            var service = new ModernMenuService(new RegistryBackup(), new ShellExtensionBlockList(new RegistryBackup()));
            var classic = options.Single() switch
            {
                "classic" => true,
                "modern" => false,
                _ => !service.IsClassicMenuForced,
            };
            service.SetClassicMenuForced(classic);
            if (!noRestart) ExplorerRestarter.Restart();
            return 0;
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Context Menu Customizer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
