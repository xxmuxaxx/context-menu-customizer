using ContextMenuCustomizer.Core;

namespace ContextMenuCustomizer.Tests;

public class MenuLocationTests
{
    [Fact]
    public void Extension_MapsToSystemFileAssociations()
    {
        var location = MenuLocation.FromUserInput(" .TXT ");
        Assert.Equal(@"SystemFileAssociations\.txt", location.ClassPath);
        Assert.Equal(@"SystemFileAssociations\.txt\shell", location.ShellPath);
        Assert.Equal(@"SystemFileAssociations\.txt\shellex\ContextMenuHandlers", location.HandlersPath);
    }

    [Fact]
    public void ProgId_IsUsedAsIs()
    {
        Assert.Equal("txtfile", MenuLocation.FromUserInput("txtfile").ClassPath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".t\\xt")]
    [InlineData("bad|name")]
    public void InvalidInput_Throws(string input) =>
        Assert.Throws<ArgumentException>(() => MenuLocation.FromUserInput(input));

    [Fact]
    public void BackgroundLocations_AreMarked()
    {
        Assert.Contains(MenuLocation.Standard, l => l.ClassPath == @"Directory\Background" && l.IsBackground);
        Assert.Contains(MenuLocation.Standard, l => l.ClassPath == "*" && !l.IsBackground);
    }
}
