namespace ContextMenuCustomizer.Core;

/// <summary>Тип объекта, для которого настраивается меню (раздел в Software\Classes).</summary>
public sealed record MenuLocation(string Title, string ClassPath, string Description, bool IsBackground = false)
{
    /// <summary>Контейнер статических команд: &lt;класс&gt;\shell.</summary>
    public string ShellPath => ClassPath + @"\shell";

    /// <summary>Контейнер обработчиков-расширений: &lt;класс&gt;\shellex\ContextMenuHandlers.</summary>
    public string HandlersPath => ClassPath + @"\shellex\ContextMenuHandlers";

    public override string ToString() => Title;

    public static IReadOnlyList<MenuLocation> Standard { get; } =
    [
        new("Все файлы", "*", "Меню любого файла (HKCR\\*)"),
        new("Папки", "Directory", "Меню папки в файловой системе"),
        new("Фон папки", @"Directory\Background", "Правый клик по пустому месту внутри папки", IsBackground: true),
        new("Рабочий стол (фон)", "DesktopBackground", "Правый клик по пустому месту рабочего стола", IsBackground: true),
        new("Диски", "Drive", "Меню дисков в «Этом компьютере»"),
        new("Файлы и папки", "AllFilesystemObjects", "Любые файлы и папки"),
        new("Все папки (включая виртуальные)", "Folder", "Папки, библиотеки, «Этот компьютер» и т. п."),
    ];

    /// <summary>
    /// Создаёт расположение по вводу пользователя: «.txt» → SystemFileAssociations\.txt,
    /// иначе строка трактуется как путь к классу (ProgID), например «txtfile».
    /// </summary>
    public static MenuLocation FromUserInput(string input)
    {
        var value = input.Trim().Trim('\\');
        if (value.Length == 0)
            throw new ArgumentException("Укажите расширение (например, .txt) или имя класса.");
        if (value.IndexOfAny(['/', '"', '*', '?', '<', '>', '|']) >= 0 && value != "*")
            throw new ArgumentException("Недопустимые символы в имени.");

        if (value.StartsWith('.'))
        {
            if (value.Contains('\\'))
                throw new ArgumentException("Расширение не должно содержать «\\».");
            var ext = value.ToLowerInvariant();
            return new MenuLocation($"Файлы {ext}", $@"SystemFileAssociations\{ext}", $"Только файлы с расширением {ext}");
        }

        return new MenuLocation(value, value, $"Класс HKCR\\{value}");
    }
}
