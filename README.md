# Context Menu Customizer

Инструмент для Windows 10 и 11, который настраивает пункты контекстного меню Проводника — и в **классическом** виде
(Windows 10 и «Показать дополнительные параметры» в Windows 11), и в **новом** меню Windows 11.

## Возможности

### Классическое меню
- Пункты меню для **всех файлов**, **папок**, **фона папки**, **фона рабочего стола**, **дисков**, «файлов и папок»,
  всех папок, а также для **конкретного расширения** (`.txt`, `.png`, …) или любого класса HKCR (`txtfile`, …).
- **Добавление команды**: текст, командная строка (`%1` — выбранный объект, `%V` — текущая папка), иконка,
  положение (вверху/внизу), «только с Shift», разделители; для всех пользователей (HKLM) или только для себя (HKCU).
- **Каскадные подменю**: создание подменю и команд внутри него (двойной щелчок открывает подменю).
- **Скрытие без удаления** — снимите флажок:
  - для команд ставится `LegacyDisable`;
  - для COM-обработчиков `shellex\ContextMenuHandlers` (7-Zip, WinRAR, антивирусы и т. п.) к CLSID дописывается
    префикс `---` (тот же приём, что у ShellExView).
- Изменение и удаление существующих пунктов (включая пункты сторонних программ).
- **Порядок пунктов**: кнопки «▲ Выше» / «▼ Ниже» (или Alt+↑/↓). Проводник сортирует статические команды по имени
  раздела реестра внутри групп «Вверху» / обычные / «Внизу», поэтому программа переименовывает разделы, добавляя номер
  (`010_`, `020_`, …); текст пунктов не меняется. Стандартные глаголы (`open`, `runas`, `edit`, …) и действие по умолчанию
  не переименовываются. Порядок пунктов COM-обработчиков (shellex) задают сами расширения.

### Новое меню Windows 11
- **Включение/выключение нового меню** одной кнопкой на панели главного окна (видна на любой вкладке)
  или из командной строки — удобно для ярлыка:
  ```
  ContextMenuCustomizer.exe --classic     классическое меню, как в Windows 10
  ContextMenuCustomizer.exe --modern      новое меню Windows 11
  ContextMenuCustomizer.exe --toggle      переключить
  ContextMenuCustomizer.exe --classic --no-restart   без перезапуска Проводника
  ```
  (`HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32`).
- **Скрытие пунктов нового меню**: «Открыть в Терминале», «Спросить Copilot», «Изменить в Paint/Блокноте/Фотографиях»,
  «Поделиться», «Проверить с помощью Microsoft Defender» и т. д., а также пункты **любых установленных
  MSIX-приложений** — программа читает их `AppxManifest.xml` (`windows.fileExplorerContextMenus`).
  Скрытие делается через `Shell Extensions\Blocked` и полностью обратимо.
- Скрытие произвольного расширения по CLSID.

### Безопасность
- Перед удалением и изменением разделов реестра создаётся `.reg`-копия в
  `%LOCALAPPDATA%\ContextMenuCustomizer\Backups` (меню «Файл → Открыть папку резервных копий»).
  Чтобы откатить изменение, дважды щёлкните по нужному `.reg`-файлу.
- Кнопка «Перезапустить Проводник», чтобы изменения применились сразу.

## Ограничение Windows 11

Новое меню Windows 11 показывает только команды приложений, которые установлены **пакетом (MSIX / sparse package)**
и реализуют COM-интерфейс `IExplorerCommand`. Обычные команды из реестра туда не попадают — это ограничение самой Windows.
Поэтому команды, добавленные в программе, в Windows 11 видны:
- в «Показать дополнительные параметры» (или Shift+F10 / Shift + правый клик), либо
- сразу, если на вкладке «Новое меню Windows 11» выбрать **классический вид** меню.

## Запуск

Программа требует прав администратора (большинство пунктов меню хранится в `HKLM`).

Скачайте `ContextMenuCustomizer.exe` (один файл, .NET встроен) со страницы
[Releases](https://github.com/xxmuxaxx/context-menu-customizer/releases/latest).

Новый релиз выпускается так: Actions → **build** → *Run workflow* → указать версию (например, `1.0.1`),
либо запушить тег `v1.0.1`. Workflow соберёт exe, создаст тег и GitHub Release с файлом.

## Сборка из исходников

Нужен [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
dotnet test tests/ContextMenuCustomizer.Tests
dotnet run --project src/ContextMenuCustomizer

# один exe-файл без зависимостей
dotnet publish src/ContextMenuCustomizer -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

## Структура

| Путь | Назначение |
|---|---|
| `src/ContextMenuCustomizer.Core` | Работа с реестром: классическое меню (`ClassicMenuService`), новое меню Windows 11 (`ModernMenuService`), список блокировки расширений, резервные копии, разбор `AppxManifest.xml` |
| `src/ContextMenuCustomizer` | Приложение WinForms |
| `tests/ContextMenuCustomizer.Tests` | Модульные тесты (xUnit) |

## Где что хранится в реестре

| Что | Раздел |
|---|---|
| Команды | `HKLM\|HKCU\Software\Classes\<класс>\shell\<имя>` (+ `command`) |
| Подменю | `…\shell\<имя>` со значением `SubCommands=""` и вложенным `shell` |
| COM-обработчики | `…\<класс>\shellex\ContextMenuHandlers\<имя>` |
| Блокировка расширений | `HKLM\|HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked` |
| Классическое меню в Windows 11 | `HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32` |

`<класс>`: `*` — все файлы, `Directory` — папки, `Directory\Background` — фон папки, `DesktopBackground` — рабочий стол,
`Drive` — диски, `SystemFileAssociations\.ext` — файлы с расширением `.ext`.
