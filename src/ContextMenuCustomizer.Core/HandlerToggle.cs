namespace ContextMenuCustomizer.Core;

/// <summary>
/// Отключение обработчика shellex делается так же, как в ShellExView/ShellMenuView:
/// к CLSID в значении по умолчанию дописывается префикс «---», и Проводник не может создать объект.
/// </summary>
public static class HandlerToggle
{
    public const string DisabledPrefix = "---";

    public static bool IsDisabled(string? value) => value != null && value.StartsWith(DisabledPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Вычисляет новое значение по умолчанию раздела обработчика.
    /// null означает «значения по умолчанию быть не должно» (Проводник возьмёт CLSID из имени раздела).
    /// </summary>
    public static string? ComputeValue(string keyName, string? current, bool enable)
    {
        if (enable)
        {
            if (!IsDisabled(current)) return string.IsNullOrEmpty(current) ? null : current;
            var original = current![DisabledPrefix.Length..];
            return original.Length == 0 || original.Equals(keyName, StringComparison.OrdinalIgnoreCase) ? null : original;
        }

        if (IsDisabled(current)) return current;
        return DisabledPrefix + (string.IsNullOrEmpty(current) ? keyName : current);
    }

    /// <summary>CLSID обработчика: из значения по умолчанию или из имени раздела.</summary>
    public static string? ResolveClsid(string keyName, string? value)
    {
        var fromValue = value == null ? null : ClsidUtil.Normalize(value.TrimStart('-'));
        return fromValue ?? ClsidUtil.Normalize(keyName);
    }
}
