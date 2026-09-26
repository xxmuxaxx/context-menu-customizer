using Microsoft.Win32;

namespace ContextMenuCustomizer.Core;

/// <summary>Новое контекстное меню Windows 11.</summary>
public sealed class ModernMenuService(RegistryBackup backup, ShellExtensionBlockList blockList)
{
    /// <summary>
    /// Пустой InprocServer32 у этого CLSID заставляет Проводник Windows 11 показывать
    /// классическое меню сразу, без «Показать дополнительные параметры».
    /// </summary>
    public const string ClassicMenuClsidPath = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}";

    public static bool IsWindows11 => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

    public bool IsClassicMenuForced
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(ClassicMenuClsidPath + @"\InprocServer32");
            return key != null;
        }
    }

    public void SetClassicMenuForced(bool forced)
    {
        if (forced)
        {
            using var key = Registry.CurrentUser.CreateSubKey(ClassicMenuClsidPath + @"\InprocServer32", writable: true);
            key.SetValue("", "", RegistryValueKind.String);
        }
        else
        {
            backup.Export(@"HKEY_CURRENT_USER\" + ClassicMenuClsidPath, "classic_menu");
            Registry.CurrentUser.DeleteSubKeyTree(ClassicMenuClsidPath, throwOnMissingSubKey: false);
        }
    }

    /// <summary>Встроенные пункты, пункты установленных пакетов и всё, что уже заблокировано.</summary>
    public IReadOnlyList<ModernMenuItem> GetItems()
    {
        var items = new Dictionary<string, ModernMenuItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var verb in PackagedMenuExtensions.Enumerate())
        {
            if (items.TryGetValue(verb.Verb.Clsid, out var existing))
            {
                var type = ManifestContextMenuParser.DescribeItemType(verb.Verb.ItemType);
                if (!existing.Targets.Split(", ").Contains(type))
                    items[verb.Verb.Clsid] = Clone(existing, targets: existing.Targets + ", " + type);
                continue;
            }

            var known = KnownModernItems.All.FirstOrDefault(k => k.Clsid.Equals(verb.Verb.Clsid, StringComparison.OrdinalIgnoreCase));
            items[verb.Verb.Clsid] = new ModernMenuItem
            {
                Title = known.Title ?? (string.IsNullOrEmpty(verb.Verb.VerbId)
                    ? verb.PackageDisplayName
                    : $"{verb.PackageDisplayName}: {verb.Verb.VerbId}"),
                Clsid = verb.Verb.Clsid,
                Source = ModernItemSource.Package,
                SourceName = verb.PackageDisplayName,
                Targets = ManifestContextMenuParser.DescribeItemType(verb.Verb.ItemType),
            };
        }

        foreach (var (clsid, title) in KnownModernItems.All)
        {
            if (items.ContainsKey(clsid)) continue;
            items[clsid] = new ModernMenuItem
            {
                Title = title,
                Clsid = clsid,
                Source = ModernItemSource.BuiltIn,
                SourceName = "Windows",
            };
        }

        foreach (var (clsid, description) in blockList.GetAll())
        {
            if (items.ContainsKey(clsid)) continue;
            items[clsid] = new ModernMenuItem
            {
                Title = string.IsNullOrWhiteSpace(description) ? RegistryUtil.GetClsidName(clsid) ?? clsid : description,
                Clsid = clsid,
                Source = ModernItemSource.BlockList,
                SourceName = "Список блокировки",
            };
        }

        var blocked = blockList.GetAll();
        foreach (var item in items.Values)
            item.IsBlocked = blocked.ContainsKey(item.Clsid);

        return items.Values
            .OrderBy(i => i.Source)
            .ThenBy(i => i.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public void SetVisible(ModernMenuItem item, bool visible)
    {
        if (visible) blockList.Unblock(item.Clsid);
        else blockList.Block(item.Clsid, item.Title);
        item.IsBlocked = !visible;
    }

    private static ModernMenuItem Clone(ModernMenuItem source, string targets) => new()
    {
        Title = source.Title,
        Clsid = source.Clsid,
        Source = source.Source,
        SourceName = source.SourceName,
        Targets = targets,
        IsBlocked = source.IsBlocked,
    };
}
