namespace ContextMenuCustomizer.Core;

public enum ClassicItemKind
{
    /// <summary>Статическая команда (раздел shell\&lt;имя&gt; с подразделом command).</summary>
    Command,
    /// <summary>Каскадное подменю (SubCommands / ExtendedSubCommandsKey).</summary>
    Submenu,
    /// <summary>COM-обработчик shellex\ContextMenuHandlers.</summary>
    Handler,
}

public sealed class ClassicMenuItem
{
    public required ClassicItemKind Kind { get; init; }
    public required MenuHive Hive { get; init; }
    public required string KeyName { get; init; }

    /// <summary>Путь к разделу относительно Software\Classes.</summary>
    public required string KeyPath { get; init; }

    public string DisplayName { get; init; } = "";

    /// <summary>Текст пункта не задан (ни MUIVerb, ни значение по умолчанию) — Проводник показывает имя раздела.</summary>
    public bool TextFromKeyName { get; init; }
    public string? Command { get; init; }
    public string? DelegateExecute { get; init; }
    public string? Icon { get; init; }
    public string? Position { get; init; }
    public bool Extended { get; init; }
    public bool SeparatorBefore { get; init; }
    public bool SeparatorAfter { get; init; }
    public bool LegacyDisable { get; init; }
    public bool ProgrammaticAccessOnly { get; init; }

    /// <summary>Для обработчиков: CLSID COM-объекта.</summary>
    public string? Clsid { get; init; }

    /// <summary>Для обработчиков: текущее значение по умолчанию (может начинаться с «---», если отключён).</summary>
    public string? HandlerValue { get; init; }

    /// <summary>CLSID заблокирован глобально в Shell Extensions\Blocked.</summary>
    public bool GloballyBlocked { get; init; }

    /// <summary>Для подменю: контейнер дочерних команд (…\shell) или null, если подменю берётся из CommandStore.</summary>
    public string? SubmenuShellPath { get; init; }
    public MenuHive SubmenuHive { get; init; }

    public bool Enabled => Kind == ClassicItemKind.Handler
        ? !HandlerToggle.IsDisabled(HandlerValue) && !GloballyBlocked
        : !LegacyDisable && !ProgrammaticAccessOnly;

    public bool IsEditable => Kind != ClassicItemKind.Handler;

    public string Target => Kind switch
    {
        ClassicItemKind.Handler => Clsid ?? HandlerValue ?? "",
        ClassicItemKind.Submenu => SubmenuShellPath != null ? "(подменю)" : "(подменю из CommandStore)",
        _ => Command ?? (DelegateExecute != null ? $"DelegateExecute {DelegateExecute}" : ""),
    };

    public string FullRegistryPath => RegistryUtil.FullClassesPath(Hive, KeyPath);
}
