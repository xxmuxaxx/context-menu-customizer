namespace ContextMenuCustomizer.Core;

public enum ModernItemSource
{
    /// <summary>Встроенный пункт Windows с известным CLSID.</summary>
    BuiltIn,
    /// <summary>Пункт из пакетного приложения (MSIX / sparse package), найден в AppxManifest.xml.</summary>
    Package,
    /// <summary>Неизвестный CLSID, уже присутствующий в списке блокировки.</summary>
    BlockList,
}

public sealed class ModernMenuItem
{
    public required string Title { get; init; }
    public required string Clsid { get; init; }
    public required ModernItemSource Source { get; init; }
    public string SourceName { get; init; } = "";
    public string Targets { get; init; } = "";
    public bool IsBlocked { get; set; }
}
