using Microsoft.Win32;

namespace ContextMenuCustomizer.Core;

/// <summary>
/// Список Shell Extensions\Blocked: Проводник не загружает перечисленные CLSID нигде —
/// ни в классическом меню, ни в новом меню Windows 11.
/// </summary>
public sealed class ShellExtensionBlockList(RegistryBackup backup)
{
    public const string KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";

    private static IEnumerable<(MenuHive Hive, RegistryKey Root)> Roots()
    {
        yield return (MenuHive.LocalMachine, Registry.LocalMachine);
        yield return (MenuHive.CurrentUser, Registry.CurrentUser);
    }

    public bool IsBlocked(string? clsid)
    {
        var normalized = ClsidUtil.Normalize(clsid);
        if (normalized == null) return false;
        foreach (var (_, root) in Roots())
        {
            using var key = root.OpenSubKey(KeyPath);
            if (key?.GetValue(normalized) != null) return true;
        }
        return false;
    }

    /// <summary>Все заблокированные CLSID с описанием (значение параметра).</summary>
    public IReadOnlyDictionary<string, string> GetAll()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, root) in Roots())
        {
            using var key = root.OpenSubKey(KeyPath);
            if (key == null) continue;
            foreach (var name in key.GetValueNames())
            {
                var normalized = ClsidUtil.Normalize(name);
                if (normalized != null && !result.ContainsKey(normalized))
                    result[normalized] = key.GetValue(name) as string ?? "";
            }
        }
        return result;
    }

    /// <summary>Блокирует CLSID. Пишет в HKLM, при нехватке прав — в HKCU.</summary>
    public void Block(string clsid, string? description)
    {
        var normalized = ClsidUtil.Normalize(clsid) ?? throw new ArgumentException($"«{clsid}» не похож на CLSID.");
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(KeyPath, writable: true);
            key.SetValue(normalized, description ?? "", RegistryValueKind.String);
        }
        catch (Exception e) when (e is UnauthorizedAccessException or System.Security.SecurityException)
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);
            key.SetValue(normalized, description ?? "", RegistryValueKind.String);
        }
    }

    /// <summary>Снимает блокировку в обоих кустах.</summary>
    public void Unblock(string clsid)
    {
        var normalized = ClsidUtil.Normalize(clsid) ?? throw new ArgumentException($"«{clsid}» не похож на CLSID.");
        foreach (var (hive, root) in Roots())
        {
            using var key = root.OpenSubKey(KeyPath, writable: false);
            if (key == null) continue;
            var names = key.GetValueNames().Where(n => ClsidUtil.Normalize(n) == normalized).ToList();
            if (names.Count == 0) continue;

            backup.Export($@"{(hive == MenuHive.LocalMachine ? "HKEY_LOCAL_MACHINE" : "HKEY_CURRENT_USER")}\{KeyPath}", "Blocked");
            using var writable = root.OpenSubKey(KeyPath, writable: true)
                ?? throw new UnauthorizedAccessException($"Нет доступа к {hive.ShortName()}\\{KeyPath}");
            foreach (var name in names)
                writable.DeleteValue(name, throwOnMissingValue: false);
        }
    }
}
