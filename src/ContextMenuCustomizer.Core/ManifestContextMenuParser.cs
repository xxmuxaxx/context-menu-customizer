using System.Xml.Linq;

namespace ContextMenuCustomizer.Core;

/// <summary>Команда контекстного меню, объявленная в AppxManifest.xml (расширение windows.fileExplorerContextMenus).</summary>
public sealed record ManifestVerb(string VerbId, string Clsid, string ItemType);

public static class ManifestContextMenuParser
{
    /// <summary>
    /// Ищет элементы &lt;desktop4:FileExplorerContextMenus&gt; (и их аналоги desktop5/desktop9 и т. д.)
    /// и возвращает все &lt;Verb Id=".." Clsid=".."/&gt; вместе с типом объекта &lt;ItemType Type=".."&gt;.
    /// </summary>
    public static IReadOnlyList<ManifestVerb> Parse(XDocument manifest)
    {
        var result = new List<ManifestVerb>();
        var menus = manifest.Descendants().Where(e => e.Name.LocalName == "FileExplorerContextMenus");
        foreach (var menu in menus)
        {
            foreach (var itemType in menu.Descendants().Where(e => e.Name.LocalName == "ItemType"))
            {
                var type = (string?)itemType.Attribute("Type") ?? "";
                foreach (var verb in itemType.Elements().Where(e => e.Name.LocalName == "Verb"))
                {
                    var clsid = ClsidUtil.Normalize((string?)verb.Attribute("Clsid"));
                    if (clsid == null) continue;
                    result.Add(new ManifestVerb((string?)verb.Attribute("Id") ?? "", clsid, type));
                }
            }
        }
        return result;
    }

    public static IReadOnlyList<ManifestVerb> Parse(string xml) => Parse(XDocument.Parse(xml));

    /// <summary>Человекочитаемое описание типа объекта из манифеста.</summary>
    public static string DescribeItemType(string type) => type switch
    {
        "*" => "файлы",
        "Directory" => "папки",
        @"Directory\Background" => "фон папки",
        "Drive" => "диски",
        "Folder" => "все папки",
        _ => type,
    };
}
