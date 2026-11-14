using MapOverhaul;
using Xunit;

public class PoiTableTests
{
    [Theory]
    [InlineData("cabin1")]
    [InlineData("cabin2")]
    public void A_cabin_identifier_gives_a_cabin(string identifier)
    {
        Assert.True(PoiTable.TryGet(identifier, out var kind, out _));
        Assert.Equal(FindKind.Cabin, kind);
    }

    [Theory]
    [InlineData("cave_entrance_2")]
    [InlineData("cave_entrance_tunnel_4")]
    public void A_cave_entrance_identifier_gives_a_cave_entrance(string identifier)
    {
        Assert.True(PoiTable.TryGet(identifier, out var kind, out _));
        Assert.Equal(FindKind.CaveEntrance, kind);
    }

    [Theory]
    [InlineData("tin_rock_resource_1", Metal.Tin)]
    [InlineData("copper_rock_resource_1", Metal.Copper)]
    [InlineData("copper_rock_resource_2", Metal.Copper)]
    [InlineData("iron_rock_resource_1", Metal.Iron)]
    [InlineData("iron_rock_resource_2", Metal.Iron)]
    [InlineData("silver_rock_resource_1", Metal.Silver)]
    public void An_ore_rock_identifier_gives_the_ore_rock_of_its_metal(string identifier, Metal metal)
    {
        Assert.True(PoiTable.TryGet(identifier, out var kind, out var found));
        Assert.Equal(FindKind.OreRock, kind);
        Assert.Equal(metal, found);
    }

    [Theory]
    [InlineData("cabin_roof_large")]
    [InlineData("cabin_chair")]
    [InlineData("rock_resource_1")]
    [InlineData("limestone_rock_resource_1")]
    [InlineData("iron_ore")]
    [InlineData("tin_ore")]
    [InlineData("")]
    [InlineData(null)]
    public void Another_identifier_is_no_poi(string identifier)
    {
        Assert.False(PoiTable.TryGet(identifier, out _, out _));
    }
}
