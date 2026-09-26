using ContextMenuCustomizer.Core;

namespace ContextMenuCustomizer.Tests;

public class MenuTextTests
{
    [Theory]
    [InlineData("&Открыть", "Открыть")]
    [InlineData("Tom && Jerry", "Tom & Jerry")]
    [InlineData("Без ускорителя", "Без ускорителя")]
    public void StripAccelerators(string input, string expected) =>
        Assert.Equal(expected, MenuText.StripAccelerators(input));

    [Theory]
    [InlineData("Открыть в VS Code", "OtkrytVVSCode")]
    [InlineData("Open with Notepad++", "OpenWithNotepad")]
    [InlineData("!!!", "CustomCommand")]
    public void SuggestKeyName(string text, string expected) =>
        Assert.Equal(expected, MenuText.SuggestKeyName(text));

    [Theory]
    [InlineData("OpenHere", true)]
    [InlineData("", false)]
    [InlineData("a\\b", false)]
    [InlineData(" padded", false)]
    public void ValidateKeyName(string name, bool valid) =>
        Assert.Equal(valid, MenuText.ValidateKeyName(name) == null);

    [Fact]
    public void BuildCommand_UsesPlaceholderForContext()
    {
        Assert.Equal("\"C:\\app.exe\" \"%1\"", MenuText.BuildCommand(@"C:\app.exe", isBackground: false));
        Assert.Equal("\"C:\\app.exe\" \"%V\"", MenuText.BuildCommand(@"C:\app.exe", isBackground: true));
    }
}
