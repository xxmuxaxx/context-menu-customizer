using ContextMenuCustomizer.Core;

namespace ContextMenuCustomizer.Tests;

public class MenuOrderTests
{
    private static ClassicMenuItem Verb(string name, string? position = null,
        ClassicItemKind kind = ClassicItemKind.Command, MenuHive hive = MenuHive.LocalMachine) => new()
    {
        Kind = kind,
        Hive = hive,
        KeyName = name,
        KeyPath = @"Directory\Background\shell\" + name,
        DisplayName = name,
        Position = position,
    };

    [Fact]
    public void Sort_GroupsByPositionThenKeyName_HandlersLast()
    {
        var items = new[]
        {
            Verb("zeta"),
            Verb("handler", kind: ClassicItemKind.Handler),
            Verb("bottom", "Bottom"),
            Verb("Alpha"),
            Verb("top", "top"),
            Verb("020_b"),
            Verb("010_a", hive: MenuHive.CurrentUser),
        };

        Assert.Equal(
            ["top", "010_a", "020_b", "Alpha", "zeta", "bottom", "handler"],
            MenuOrder.Sort(items).Select(i => i.KeyName));
    }

    [Theory]
    [InlineData("Foo", 0, "010_Foo")]
    [InlineData("030_Foo", 1, "020_Foo")]
    [InlineData("12_Foo", 9, "100_12_Foo")]
    public void WithPrefix_ReplacesOwnPrefixOnly(string name, int index, string expected) =>
        Assert.Equal(expected, MenuOrder.WithPrefix(name, index));

    [Fact]
    public void PlanRenames_SkipsItemsAlreadyInPlace()
    {
        var ordered = new[] { Verb("010_a"), Verb("c"), Verb("030_b") };

        var plan = MenuOrder.PlanRenames(ordered);

        Assert.Equal([("c", "020_c")], plan.Select(p => (p.Item.KeyName, p.NewName)));
    }

    [Fact]
    public void PlanRenames_SwapProducesUniqueNames()
    {
        var ordered = new[] { Verb("020_b"), Verb("010_a") };

        var plan = MenuOrder.PlanRenames(ordered);

        Assert.Equal([("020_b", "010_b"), ("010_a", "020_a")], plan.Select(p => (p.Item.KeyName, p.NewName)));
    }

    [Theory]
    [InlineData("runas", null, false)]
    [InlineData("Open", null, false)]
    [InlineData("VSCode", "open,VSCode", false)]
    [InlineData("VSCode", "none", true)]
    [InlineData("VSCode", null, true)]
    public void WhyNotReorderable_ProtectsSystemAndDefaultVerbs(string name, string? shellDefault, bool allowed) =>
        Assert.Equal(allowed, MenuOrder.WhyNotReorderable(Verb(name), shellDefault) == null);

    [Fact]
    public void WhyNotReorderable_RejectsHandlers() =>
        Assert.NotNull(MenuOrder.WhyNotReorderable(Verb("7-Zip", kind: ClassicItemKind.Handler), null));
}
