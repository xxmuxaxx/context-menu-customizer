using Microsoft.Win32;

namespace ContextMenuCustomizer.Core;

/// <summary>Чтение и изменение классического контекстного меню (Windows 10 и «Показать дополнительные параметры» в Windows 11).</summary>
public sealed class ClassicMenuService(RegistryBackup backup, ShellExtensionBlockList blockList)
{
    private static readonly MenuHive[] Hives = [MenuHive.LocalMachine, MenuHive.CurrentUser];

    /// <summary>Команды и подменю из контейнера …\shell обоих кустов.</summary>
    public IReadOnlyList<ClassicMenuItem> GetCommands(string shellPath)
    {
        var result = new List<ClassicMenuItem>();
        foreach (var hive in Hives)
        {
            using var container = RegistryUtil.OpenClasses(hive, shellPath);
            if (container == null) continue;
            foreach (var name in container.GetSubKeyNames())
            {
                try
                {
                    using var key = container.OpenSubKey(name);
                    if (key != null) result.Add(ReadVerb(hive, shellPath + "\\" + name, name, key));
                }
                catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException)
                {
                    // Раздел без прав на чтение — пропускаем.
                }
            }
        }
        return result;
    }

    /// <summary>Обработчики shellex\ContextMenuHandlers обоих кустов.</summary>
    public IReadOnlyList<ClassicMenuItem> GetHandlers(string handlersPath)
    {
        var result = new List<ClassicMenuItem>();
        foreach (var hive in Hives)
        {
            using var container = RegistryUtil.OpenClasses(hive, handlersPath);
            if (container == null) continue;
            foreach (var name in container.GetSubKeyNames())
            {
                try
                {
                    using var key = container.OpenSubKey(name);
                    if (key == null) continue;
                    var value = key.GetValue("") as string;
                    var clsid = HandlerToggle.ResolveClsid(name, value);
                    var clsidName = clsid != null ? RegistryUtil.GetClsidName(clsid) : null;
                    var display = ClsidUtil.IsClsid(name) ? clsidName ?? name : name;
                    if (!string.IsNullOrWhiteSpace(clsidName) && !display.Equals(clsidName, StringComparison.OrdinalIgnoreCase))
                        display = $"{display} ({clsidName})";

                    result.Add(new ClassicMenuItem
                    {
                        Kind = ClassicItemKind.Handler,
                        Hive = hive,
                        KeyName = name,
                        KeyPath = handlersPath + "\\" + name,
                        DisplayName = display,
                        Clsid = clsid,
                        HandlerValue = value,
                        GloballyBlocked = clsid != null && blockList.IsBlocked(clsid),
                    });
                }
                catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException)
                {
                }
            }
        }
        return result;
    }

    private static ClassicMenuItem ReadVerb(MenuHive hive, string keyPath, string keyName, RegistryKey key)
    {
        var text = RegistryUtil.GetString(key, "MUIVerb");
        if (string.IsNullOrWhiteSpace(text)) text = RegistryUtil.GetString(key, "");
        text = string.IsNullOrWhiteSpace(text) ? keyName : MenuText.StripAccelerators(Native.ResolveIndirectString(text));

        string? command = null, delegateExecute = null;
        using (var commandKey = key.OpenSubKey("command"))
        {
            if (commandKey != null)
            {
                command = RegistryUtil.GetString(commandKey, "");
                delegateExecute = RegistryUtil.GetString(commandKey, "DelegateExecute");
            }
        }
        delegateExecute ??= RegistryUtil.GetString(key, "ExplorerCommandHandler");

        var kind = ClassicItemKind.Command;
        string? submenuPath = null;
        var submenuHive = hive;
        var extendedSubCommands = RegistryUtil.GetString(key, "ExtendedSubCommandsKey");
        if (!string.IsNullOrWhiteSpace(extendedSubCommands))
        {
            kind = ClassicItemKind.Submenu;
            submenuPath = extendedSubCommands.Trim('\\') + @"\shell";
            // ExtendedSubCommandsKey указывает на раздел HKCR — ищем его сначала в том же кусте.
            using var own = RegistryUtil.OpenClasses(hive, submenuPath);
            if (own == null)
                submenuHive = hive == MenuHive.LocalMachine ? MenuHive.CurrentUser : MenuHive.LocalMachine;
        }
        else if (RegistryUtil.HasValue(key, "SubCommands"))
        {
            kind = ClassicItemKind.Submenu;
            var subCommands = RegistryUtil.GetString(key, "SubCommands");
            submenuPath = string.IsNullOrEmpty(subCommands) ? keyPath + @"\shell" : null;
        }

        return new ClassicMenuItem
        {
            Kind = kind,
            Hive = hive,
            KeyName = keyName,
            KeyPath = keyPath,
            DisplayName = text,
            Command = command,
            DelegateExecute = delegateExecute,
            Icon = RegistryUtil.GetString(key, "Icon"),
            Position = RegistryUtil.GetString(key, "Position"),
            Extended = RegistryUtil.HasValue(key, "Extended"),
            SeparatorBefore = RegistryUtil.HasValue(key, "SeparatorBefore"),
            SeparatorAfter = RegistryUtil.HasValue(key, "SeparatorAfter"),
            LegacyDisable = RegistryUtil.HasValue(key, "LegacyDisable"),
            ProgrammaticAccessOnly = RegistryUtil.HasValue(key, "ProgrammaticAccessOnly"),
            SubmenuShellPath = submenuPath,
            SubmenuHive = submenuHive,
        };
    }

    /// <summary>Включает или скрывает пункт (без удаления).</summary>
    public void SetEnabled(ClassicMenuItem item, bool enabled)
    {
        using var key = RegistryUtil.OpenClasses(item.Hive, item.KeyPath, writable: true)
            ?? throw new InvalidOperationException($"Раздел не найден: {item.FullRegistryPath}");

        if (item.Kind == ClassicItemKind.Handler)
        {
            var current = key.GetValue("") as string;
            var updated = HandlerToggle.ComputeValue(item.KeyName, current, enabled);
            if (updated == null) key.DeleteValue("", throwOnMissingValue: false);
            else if (updated != current) key.SetValue("", updated, RegistryValueKind.String);

            if (enabled && item.Clsid != null && blockList.IsBlocked(item.Clsid))
                blockList.Unblock(item.Clsid);
        }
        else if (enabled)
        {
            key.DeleteValue("LegacyDisable", throwOnMissingValue: false);
            key.DeleteValue("ProgrammaticAccessOnly", throwOnMissingValue: false);
        }
        else
        {
            key.SetValue("LegacyDisable", "", RegistryValueKind.String);
        }

        Native.NotifyAssociationsChanged();
    }

    public bool Exists(MenuHive hive, string shellPath, string keyName)
    {
        using var key = RegistryUtil.OpenClasses(hive, shellPath + "\\" + keyName);
        return key != null;
    }

    /// <summary>Создаёт новую команду/подменю или обновляет существующую.</summary>
    public void Save(MenuHive hive, string shellPath, VerbDefinition def, bool isNew)
    {
        var error = MenuText.ValidateKeyName(def.KeyName);
        if (error != null) throw new ArgumentException(error);
        if (string.IsNullOrWhiteSpace(def.Text)) throw new ArgumentException("Укажите текст пункта.");
        if (isNew && Exists(hive, shellPath, def.KeyName))
            throw new InvalidOperationException($"Пункт с именем раздела «{def.KeyName}» уже существует.");
        if (isNew && !def.IsSubmenu && string.IsNullOrWhiteSpace(def.Command))
            throw new ArgumentException("Укажите команду.");

        var keyPath = shellPath + "\\" + def.KeyName;
        if (!isNew) backup.Export(RegistryUtil.FullClassesPath(hive, keyPath), "edit_" + def.KeyName);

        using var key = RegistryUtil.CreateClasses(hive, keyPath);
        RegistryUtil.SetStringPreservingKind(key, "MUIVerb", def.Text.Trim());
        RegistryUtil.SetOrDelete(key, "Icon", def.Icon?.Trim());
        RegistryUtil.SetOrDelete(key, "Position", def.Position);
        RegistryUtil.SetFlag(key, "Extended", def.Extended);
        RegistryUtil.SetFlag(key, "SeparatorBefore", def.SeparatorBefore);
        RegistryUtil.SetFlag(key, "SeparatorAfter", def.SeparatorAfter);

        if (def.IsSubmenu)
        {
            if (!RegistryUtil.HasValue(key, "ExtendedSubCommandsKey") && !RegistryUtil.HasValue(key, "SubCommands"))
                key.SetValue("SubCommands", "", RegistryValueKind.String);
            if (RegistryUtil.GetString(key, "SubCommands") == "")
                key.CreateSubKey("shell")?.Dispose();
        }
        else if (def.Command != null)
        {
            using var commandKey = key.CreateSubKey("command", writable: true);
            RegistryUtil.SetStringPreservingKind(commandKey, "", def.Command.Trim());
        }

        Native.NotifyAssociationsChanged();
    }

    /// <summary>Удаляет пункт (раздел целиком). Перед удалением делается резервная копия.</summary>
    public string? Delete(ClassicMenuItem item)
    {
        var backupFile = backup.Export(item.FullRegistryPath, "delete_" + item.KeyName);
        var parentPath = item.KeyPath[..item.KeyPath.LastIndexOf('\\')];
        using var parent = RegistryUtil.OpenClasses(item.Hive, parentPath, writable: true)
            ?? throw new InvalidOperationException($"Раздел не найден: {item.FullRegistryPath}");
        parent.DeleteSubKeyTree(item.KeyName, throwOnMissingSubKey: false);
        Native.NotifyAssociationsChanged();
        return backupFile;
    }
}
