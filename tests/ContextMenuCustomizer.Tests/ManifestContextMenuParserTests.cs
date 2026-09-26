using ContextMenuCustomizer.Core;

namespace ContextMenuCustomizer.Tests;

public class ManifestContextMenuParserTests
{
    // Сокращённый фрагмент AppxManifest.xml Windows Terminal.
    private const string TerminalManifest = """
        <Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
                 xmlns:uap3="http://schemas.microsoft.com/appx/manifest/uap/windows10/3"
                 xmlns:desktop4="http://schemas.microsoft.com/appx/manifest/desktop/windows10/4"
                 xmlns:com="http://schemas.microsoft.com/appx/manifest/com/windows10">
          <Applications>
            <Application Id="App">
              <Extensions>
                <desktop4:Extension Category="windows.fileExplorerContextMenus">
                  <desktop4:FileExplorerContextMenus>
                    <desktop5:ItemType xmlns:desktop5="http://schemas.microsoft.com/appx/manifest/desktop/windows10/5" Type="Directory">
                      <desktop5:Verb Id="OpenTerminalHere" Clsid="9f156763-7844-4dc4-b2b1-901f640f5155" />
                    </desktop5:ItemType>
                    <desktop5:ItemType xmlns:desktop5="http://schemas.microsoft.com/appx/manifest/desktop/windows10/5" Type="Directory\Background">
                      <desktop5:Verb Id="OpenTerminalHere" Clsid="{9F156763-7844-4DC4-B2B1-901F640F5155}" />
                    </desktop5:ItemType>
                    <desktop5:ItemType xmlns:desktop5="http://schemas.microsoft.com/appx/manifest/desktop/windows10/5" Type="*">
                      <desktop5:Verb Id="Broken" Clsid="not-a-guid" />
                    </desktop5:ItemType>
                  </desktop4:FileExplorerContextMenus>
                </desktop4:Extension>
              </Extensions>
            </Application>
          </Applications>
        </Package>
        """;

    [Fact]
    public void Parse_FindsVerbsWithNormalizedClsid()
    {
        var verbs = ManifestContextMenuParser.Parse(TerminalManifest);

        Assert.Equal(2, verbs.Count);
        Assert.All(verbs, v => Assert.Equal("{9F156763-7844-4DC4-B2B1-901F640F5155}", v.Clsid));
        Assert.All(verbs, v => Assert.Equal("OpenTerminalHere", v.VerbId));
        Assert.Equal(["Directory", @"Directory\Background"], verbs.Select(v => v.ItemType));
    }

    [Fact]
    public void Parse_ReturnsEmpty_WithoutContextMenus()
    {
        Assert.Empty(ManifestContextMenuParser.Parse("<Package><Applications /></Package>"));
    }

    [Fact]
    public void KnownItems_HaveValidUniqueClsids()
    {
        Assert.All(KnownModernItems.All, i => Assert.Equal(i.Clsid, ClsidUtil.Normalize(i.Clsid)));
        Assert.Equal(KnownModernItems.All.Count, KnownModernItems.All.Select(i => i.Clsid).Distinct().Count());
    }
}
