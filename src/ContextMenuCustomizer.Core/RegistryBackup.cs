using System.Diagnostics;

namespace ContextMenuCustomizer.Core;

/// <summary>Сохраняет разделы реестра в .reg-файлы (через reg.exe export) перед изменениями.</summary>
public sealed class RegistryBackup
{
    public RegistryBackup(string? directory = null)
    {
        Directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ContextMenuCustomizer", "Backups");
    }

    public string Directory { get; }

    /// <summary>
    /// Экспортирует раздел (полный путь вида HKEY_LOCAL_MACHINE\...) в файл.
    /// Возвращает путь к файлу или null, если раздела нет либо экспорт не удался.
    /// </summary>
    public string? Export(string fullKeyPath, string reason)
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            var file = Path.Combine(Directory, $"{DateTime.Now:yyyyMMdd-HHmmss-fff}_{MakeSafe(reason)}.reg");
            var psi = new ProcessStartInfo("reg.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.ArgumentList.Add("export");
            psi.ArgumentList.Add(fullKeyPath);
            psi.ArgumentList.Add(file);
            psi.ArgumentList.Add("/y");
            using var process = Process.Start(psi);
            if (process == null) return null;
            process.WaitForExit(15000);
            return process.HasExited && process.ExitCode == 0 && File.Exists(file) ? file : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal static string MakeSafe(string text)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = text.Select(c => invalid.Contains(c) || c is '\\' or '/' or ' ' or '*' ? '_' : c).ToArray();
        var result = new string(chars).Trim('_');
        if (result.Length > 60) result = result[..60];
        return result.Length == 0 ? "backup" : result;
    }
}
