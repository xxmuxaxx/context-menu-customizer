using System.Diagnostics;

namespace ContextMenuCustomizer.Core;

public static class ExplorerRestarter
{
    /// <summary>Завершает все процессы explorer.exe и запускает оболочку заново.</summary>
    public static void Restart()
    {
        foreach (var process in Process.GetProcessesByName("explorer"))
        {
            using (process)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch (Exception)
                {
                    // Процесс мог уже завершиться.
                }
            }
        }

        // Winlogon обычно сам перезапускает оболочку (AutoRestartShell). Если нет — запускаем вручную.
        var deadline = DateTime.UtcNow.AddSeconds(4);
        while (DateTime.UtcNow < deadline)
        {
            if (Process.GetProcessesByName("explorer").Length > 0) return;
            Thread.Sleep(250);
        }

        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        Process.Start(new ProcessStartInfo(Path.Combine(windows, "explorer.exe")) { UseShellExecute = true });
    }
}
