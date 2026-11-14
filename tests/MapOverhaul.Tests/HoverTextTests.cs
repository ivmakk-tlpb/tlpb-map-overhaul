using MapOverhaul;
using Xunit;

public class HoverTextTests
{
    // Names that differ from the English names, so a test sees that the text takes the names given in.
    private static readonly string[] Names = { "Zinn", "Kupfer", "Eisenerz", "Silbererz" };

    [Fact]
    public void A_cabin_is_cabin()
    {
        Assert.Equal("Cabin", HoverText.Of(new Poi(PoiKind.Cabin, 0, 0), Names));
    }

    [Theory]
    [InlineData(PoiKind.Tin, 1, "Tin deposit (1)")]
    [InlineData(PoiKind.Copper, 2, "Copper deposit (2)")]
    [InlineData(PoiKind.Iron, 4, "Iron deposit (4)")]
    [InlineData(PoiKind.Silver, 3, "Silver deposit (3)")]
    public void A_deposit_gives_its_metal_and_count(PoiKind kind, int count, string text)
    {
        Assert.Equal(text, HoverText.Of(new Poi(kind, 0, 0, count), Names));
    }

    [Fact]
    public void A_cave_lists_its_ores_with_the_names_given_in()
    {
        var cave = new Poi(PoiKind.Cave, 0, 0, caveOres: new[] { 0, 0, 4, 1 });

        Assert.Equal("Cave\nEisenerz (4)\nSilbererz (1)", HoverText.Of(cave, Names));
    }

    [Fact]
    public void A_cave_with_no_ores_is_cave()
    {
        Assert.Equal("Cave", HoverText.Of(new Poi(PoiKind.Cave, 0, 0, caveOres: new int[4]), Names));
    }

    [Fact]
    public void The_ore_lines_keep_the_order_tin_copper_iron_silver()
    {
        var cave = new Poi(PoiKind.Cave, 0, 0, caveOres: new[] { 2, 3, 1, 5 });

        Assert.Equal("Cave\nZinn (2)\nKupfer (3)\nEisenerz (1)\nSilbererz (5)", HoverText.Of(cave, Names));
    }
}
