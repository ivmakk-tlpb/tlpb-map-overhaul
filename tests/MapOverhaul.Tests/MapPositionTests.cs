using MapOverhaul;
using Xunit;

public class MapPositionTests
{
    // A region of the chunks -2 to 1 in each direction: the world from -64 to 64.
    private static readonly MapPosition Region = new MapPosition(-2, -2, 1, 1, 32);

    [Theory]
    [InlineData(-64f, -0.5f)]
    [InlineData(0f, 0f)]
    [InlineData(32f, 0.25f)]
    [InlineData(64f, 0.5f)]
    [InlineData(500f, 0.5f)]
    [InlineData(-500f, -0.5f)]
    public void A_world_position_maps_to_the_region_fraction_around_the_center(float world, float relative)
    {
        var (x, y) = Region.Relative(world, world);

        Assert.Equal(relative, x, 4);
        Assert.Equal(relative, y, 4);
    }

    [Fact]
    public void A_region_of_one_chunk_maps_to_its_center()
    {
        var single = new MapPosition(3, 3, 2, 2, 32);

        Assert.Equal((0f, 0f), single.Relative(100f, 100f));
    }

    [Theory]
    [InlineData(31.9f, 0)]
    [InlineData(32f, 1)]
    [InlineData(-0.1f, -1)]
    [InlineData(-32f, -1)]
    [InlineData(-32.1f, -2)]
    public void A_world_position_lies_in_the_chunk_of_its_floor(float world, int chunk)
    {
        Assert.Equal(new ChunkCoord(chunk, chunk), Region.ChunkOf(world, world));
    }
}
