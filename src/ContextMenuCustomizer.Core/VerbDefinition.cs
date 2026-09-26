namespace ContextMenuCustomizer.Core;

/// <summary>Параметры создаваемой или редактируемой команды классического меню.</summary>
public sealed class VerbDefinition
{
    public required string KeyName { get; set; }
    public required string Text { get; set; }

    /// <summary>Командная строка. null — не трогать подраздел command (например, у DelegateExecute).</summary>
    public string? Command { get; set; }

    public string? Icon { get; set; }

    /// <summary>null, "Top" или "Bottom".</summary>
    public string? Position { get; set; }

    /// <summary>Показывать только при Shift+правый клик.</summary>
    public bool Extended { get; set; }

    public bool SeparatorBefore { get; set; }
    public bool SeparatorAfter { get; set; }

    /// <summary>Создать каскадное подменю вместо команды.</summary>
    public bool IsSubmenu { get; set; }

    public static VerbDefinition FromItem(ClassicMenuItem item) => new()
    {
        KeyName = item.KeyName,
        Text = item.DisplayName,
        Command = item.Command,
        Icon = item.Icon,
        Position = item.Position,
        Extended = item.Extended,
        SeparatorBefore = item.SeparatorBefore,
        SeparatorAfter = item.SeparatorAfter,
        IsSubmenu = item.Kind == ClassicItemKind.Submenu,
    };
}
