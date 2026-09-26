using System.Runtime.InteropServices;
using System.Text;

namespace ContextMenuCustomizer.Core;

internal static partial class Native
{
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegRenameKey(Microsoft.Win32.SafeHandles.SafeRegistryHandle hKey, string lpSubKeyName, string lpNewKeyName);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    private const int SHCNE_ASSOCCHANGED = 0x08000000;
    private const uint SHCNF_IDLIST = 0x0000;

    /// <summary>Разворачивает строки вида «@shell32.dll,-8506» в текст на языке системы.</summary>
    public static string ResolveIndirectString(string value)
    {
        if (!value.StartsWith('@') || !OperatingSystem.IsWindows())
            return value;
        try
        {
            var buffer = new StringBuilder(1024);
            return SHLoadIndirectString(value, buffer, buffer.Capacity, IntPtr.Zero) == 0 ? buffer.ToString() : value;
        }
        catch (Exception)
        {
            return value;
        }
    }

    /// <summary>Сообщает Проводнику, что ассоциации (и меню) изменились.</summary>
    public static void NotifyAssociationsChanged()
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }
        catch (Exception)
        {
            // Не критично: изменения подхватятся после перезапуска Проводника.
        }
    }

    /// <summary>Переименовывает подраздел, сохраняя его содержимое и права доступа.</summary>
    public static void RenameRegistryKey(Microsoft.Win32.RegistryKey parent, string oldName, string newName)
    {
        var error = RegRenameKey(parent.Handle, oldName, newName);
        if (error == 5) throw new UnauthorizedAccessException($"Нет прав на переименование раздела «{oldName}».");
        if (error != 0) throw new System.ComponentModel.Win32Exception(error, $"Не удалось переименовать раздел «{oldName}» в «{newName}».");
    }
}
