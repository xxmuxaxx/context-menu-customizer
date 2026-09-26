using Microsoft.Win32;

namespace ContextMenuCustomizer.Core;

/// <summary>Доступ к разделам Software\Classes конкретного куста (без «склеенного» HKCR).</summary>
internal static class RegistryUtil
{
    private const string ClassesPrefix = @"Software\Classes\";

    public static RegistryKey Root(MenuHive hive) =>
        hive == MenuHive.LocalMachine ? Registry.LocalMachine : Registry.CurrentUser;

    public static RegistryKey? OpenClasses(MenuHive hive, string classPath, bool writable = false) =>
        Root(hive).OpenSubKey(ClassesPrefix + classPath, writable);

    public static RegistryKey CreateClasses(MenuHive hive, string classPath) =>
        Root(hive).CreateSubKey(ClassesPrefix + classPath, writable: true)
        ?? throw new InvalidOperationException($"Не удалось создать раздел {FullClassesPath(hive, classPath)}");

    public static string FullClassesPath(MenuHive hive, string classPath) =>
        $@"{(hive == MenuHive.LocalMachine ? "HKEY_LOCAL_MACHINE" : "HKEY_CURRENT_USER")}\{ClassesPrefix}{classPath}";

    public static string? GetString(RegistryKey key, string name) =>
        key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames) switch
        {
            null => null,
            string s => s,
            string[] arr => string.Join(" ", arr),
            var other => other.ToString(),
        };

    public static bool HasValue(RegistryKey key, string name) => key.GetValue(name) != null;

    /// <summary>Записывает строку, сохраняя тип REG_EXPAND_SZ, если значение уже было такого типа.</summary>
    public static void SetStringPreservingKind(RegistryKey key, string name, string value)
    {
        var kind = RegistryValueKind.String;
        if (key.GetValue(name) != null && key.GetValueKind(name) == RegistryValueKind.ExpandString)
            kind = RegistryValueKind.ExpandString;
        key.SetValue(name, value, kind);
    }

    public static void SetOrDelete(RegistryKey key, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            key.DeleteValue(name, throwOnMissingValue: false);
        else
            SetStringPreservingKind(key, name, value);
    }

    public static void SetFlag(RegistryKey key, string name, bool present)
    {
        if (present)
        {
            if (key.GetValue(name) == null)
                key.SetValue(name, "", RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(name, throwOnMissingValue: false);
        }
    }

    public static string? GetClsidName(string clsid)
    {
        using var key = Registry.ClassesRoot.OpenSubKey($@"CLSID\{clsid}");
        if (key == null) return null;
        var name = key.GetValue("LocalizedString") as string;
        if (!string.IsNullOrWhiteSpace(name))
        {
            var resolved = Native.ResolveIndirectString(name);
            if (!resolved.StartsWith('@')) return resolved;
        }
        return key.GetValue("") as string;
    }
}
