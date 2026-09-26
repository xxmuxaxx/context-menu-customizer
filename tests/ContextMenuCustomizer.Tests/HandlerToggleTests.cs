using ContextMenuCustomizer.Core;

namespace ContextMenuCustomizer.Tests;

public class HandlerToggleTests
{
    private const string Clsid = "{B41DB860-64E4-11D2-9906-E49FADC173CA}";

    [Fact]
    public void Disable_PrefixesExistingClsid()
    {
        Assert.Equal("---" + Clsid, HandlerToggle.ComputeValue("WinRAR", Clsid, enable: false));
    }

    [Fact]
    public void Disable_UsesKeyName_WhenValueIsEmpty()
    {
        Assert.Equal("---" + Clsid, HandlerToggle.ComputeValue(Clsid, null, enable: false));
        Assert.Equal("---" + Clsid, HandlerToggle.ComputeValue(Clsid, "", enable: false));
    }

    [Fact]
    public void Disable_IsIdempotent()
    {
        Assert.Equal("---" + Clsid, HandlerToggle.ComputeValue("WinRAR", "---" + Clsid, enable: false));
    }

    [Fact]
    public void Enable_RestoresOriginalValue()
    {
        Assert.Equal(Clsid, HandlerToggle.ComputeValue("WinRAR", "---" + Clsid, enable: true));
    }

    [Fact]
    public void Enable_RemovesValue_WhenItDuplicatesKeyName()
    {
        Assert.Null(HandlerToggle.ComputeValue(Clsid, "---" + Clsid, enable: true));
    }

    [Fact]
    public void Enable_LeavesEnabledValueUntouched()
    {
        Assert.Equal(Clsid, HandlerToggle.ComputeValue("WinRAR", Clsid, enable: true));
        Assert.Null(HandlerToggle.ComputeValue(Clsid, null, enable: true));
    }

    [Theory]
    [InlineData("WinRAR", "{b41db860-64e4-11d2-9906-e49fadc173ca}", Clsid)]
    [InlineData("WinRAR", "---{B41DB860-64E4-11D2-9906-E49FADC173CA}", Clsid)]
    [InlineData("{B41DB860-64E4-11D2-9906-E49FADC173CA}", null, Clsid)]
    [InlineData("Something", "not a guid", null)]
    public void ResolveClsid(string keyName, string? value, string? expected)
    {
        Assert.Equal(expected, HandlerToggle.ResolveClsid(keyName, value));
    }
}
