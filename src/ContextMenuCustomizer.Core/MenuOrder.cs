using System.Text.RegularExpressions;

namespace ContextMenuCustomizer.Core;

/// <summary>
/// Порядок пунктов классического меню. Проводник выводит статические команды одного раздела shell
/// в порядке имён разделов (по алфавиту), с учётом групп Position=Top / обычные / Position=Bottom.
/// Поэтому перестановка делается переименованием разделов: к имени добавляется префикс «010_», «020_», …
/// </summary>
public static partial class MenuOrder
{
    /// <summary>Имена глаголов, которые Windows и программы вызывают по имени, — их переименовывать нельзя.</summary>
    private static readonly HashSet<string> ProtectedVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "open", "opennew", "edit", "print", "printto", "find", "runas", "runasuser",
        "explore", "openas", "properties", "play", "preview",
    };

    [GeneratedRegex(@"^\d{3}_")]
    private static partial Regex OrderPrefix();

    /// <summary>0 — Position=Top, 1 — обычные, 2 — Position=Bottom.</summary>
    public static int PositionGroup(string? position) => position?.Trim().ToLowerInvariant() switch
    {
        "top" => 0,
        "bottom" => 2,
        _ => 1,
    };

    public static string StripPrefix(string keyName) => OrderPrefix().Replace(keyName, "");

    public static string WithPrefix(string keyName, int index) => $"{(index + 1) * 10:D3}_{StripPrefix(keyName)}";

    /// <summary>Сортирует пункты так, как их покажет Проводник: команды и подменю, затем обработчики.</summary>
    public static IEnumerable<ClassicMenuItem> Sort(IEnumerable<ClassicMenuItem> items) => items
        .OrderBy(i => i.Kind == ClassicItemKind.Handler)
        .ThenBy(i => i.Kind == ClassicItemKind.Handler ? 1 : PositionGroup(i.Position))
        .ThenBy(i => i.KeyName, StringComparer.OrdinalIgnoreCase);

    /// <summary>Можно ли менять позицию пункта переименованием раздела.</summary>
    public static string? WhyNotReorderable(ClassicMenuItem item, string? shellDefaultVerb)
    {
        if (item.Kind == ClassicItemKind.Handler)
            return "Порядок пунктов обработчиков (shellex) определяет сама программа-расширение.";
        if (ProtectedVerbs.Contains(item.KeyName))
            return $"«{item.KeyName}» — стандартный глагол Windows, его раздел нельзя переименовывать.";
        if (shellDefaultVerb != null && shellDefaultVerb
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Contains(item.KeyName, StringComparer.OrdinalIgnoreCase))
            return $"«{item.KeyName}» указан как действие по умолчанию, его раздел нельзя переименовывать.";
        return null;
    }

    /// <summary>
    /// Для нового порядка пунктов (в пределах одной группы) возвращает переименования «старое имя → новое».
    /// Пункты, имя которых уже правильное, не попадают в результат.
    /// </summary>
    public static IReadOnlyList<(ClassicMenuItem Item, string NewName)> PlanRenames(IReadOnlyList<ClassicMenuItem> ordered) =>
        ordered
            .Select((item, index) => (Item: item, NewName: WithPrefix(item.KeyName, index)))
            .Where(r => !r.NewName.Equals(r.Item.KeyName, StringComparison.Ordinal))
            .ToList();
}
