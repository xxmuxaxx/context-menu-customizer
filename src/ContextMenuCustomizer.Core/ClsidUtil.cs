namespace ContextMenuCustomizer.Core;

public static class ClsidUtil
{
    /// <summary>Приводит CLSID к виду {XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX} или возвращает null.</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Guid.TryParse(value.Trim(), out var guid) ? guid.ToString("B").ToUpperInvariant() : null;
    }

    public static bool IsClsid(string? value) => Normalize(value) != null;
}
