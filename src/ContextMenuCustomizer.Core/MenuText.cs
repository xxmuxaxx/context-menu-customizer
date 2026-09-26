using System.Text;

namespace ContextMenuCustomizer.Core;

public static class MenuText
{
    /// <summary>Убирает маркеры клавиш-ускорителей: «&amp;Открыть» → «Открыть», «&amp;&amp;» → «&amp;».</summary>
    public static string StripAccelerators(string text)
    {
        var sb = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '&')
            {
                if (i + 1 < text.Length && text[i + 1] == '&')
                {
                    sb.Append('&');
                    i++;
                }
                continue;
            }
            sb.Append(text[i]);
        }
        return sb.ToString();
    }

    /// <summary>Проверяет имя раздела реестра для новой команды. Возвращает текст ошибки или null.</summary>
    public static string? ValidateKeyName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Имя раздела не может быть пустым.";
        if (name.Length > 200) return "Имя раздела слишком длинное.";
        if (name.Contains('\\')) return "Имя раздела не может содержать «\\».";
        if (name != name.Trim()) return "Имя раздела не должно начинаться или заканчиваться пробелом.";
        return null;
    }

    private static readonly Dictionary<char, string> Translit = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['е'] = "e", ['ё'] = "e", ['ж'] = "zh",
        ['з'] = "z", ['и'] = "i", ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n", ['о'] = "o",
        ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u", ['ф'] = "f", ['х'] = "h", ['ц'] = "ts",
        ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "sch", ['ъ'] = "", ['ы'] = "y", ['ь'] = "", ['э'] = "e", ['ю'] = "yu",
        ['я'] = "ya",
    };

    /// <summary>Предлагает латинское имя раздела по тексту пункта: «Открыть в VS Code» → «OtkrytVVSCode».</summary>
    public static string SuggestKeyName(string text)
    {
        var sb = new StringBuilder();
        var upperNext = true;
        foreach (var raw in StripAccelerators(text))
        {
            var lower = char.ToLowerInvariant(raw);
            string? part = null;
            if (Translit.TryGetValue(lower, out var t)) part = t;
            else if (lower is >= 'a' and <= 'z' || char.IsAsciiDigit(lower)) part = raw.ToString();

            if (string.IsNullOrEmpty(part))
            {
                upperNext = true;
                continue;
            }
            if (upperNext)
            {
                part = char.ToUpperInvariant(part[0]) + part[1..];
                upperNext = false;
            }
            sb.Append(part);
            if (sb.Length >= 48) break;
        }
        return sb.Length == 0 ? "CustomCommand" : sb.ToString();
    }

    /// <summary>
    /// Формирует командную строку для программы: для фона папки передаётся %V (текущая папка),
    /// для файлов и папок — %1 (выбранный объект).
    /// </summary>
    public static string BuildCommand(string exePath, bool isBackground) =>
        $"\"{exePath}\" \"{(isBackground ? "%V" : "%1")}\"";
}
