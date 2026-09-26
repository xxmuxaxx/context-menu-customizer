namespace ContextMenuCustomizer.Core;

/// <summary>Где хранится запись: для всех пользователей (HKLM) или только для текущего (HKCU).</summary>
public enum MenuHive
{
    LocalMachine,
    CurrentUser,
}

public static class MenuHiveExtensions
{
    public static string ShortName(this MenuHive hive) => hive == MenuHive.LocalMachine ? "HKLM" : "HKCU";

    public static string DisplayName(this MenuHive hive) =>
        hive == MenuHive.LocalMachine ? "Все пользователи (HKLM)" : "Текущий пользователь (HKCU)";
}
