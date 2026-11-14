using MapOverhaul;
using Xunit;

public class FogTextureTests
{
    [Fact]
    public void The_texture_has_one_pixel_for_each_chunk_of_the_region()
    {
        var fog = new FogTexture(-32, -16, 31, 15);

        Assert.Equal((64, 32), (fog.Width, fog.Height));
        Assert.Equal(64 * 32, fog.Alpha.Length);
    }

    [Fact]
    public void Each_chunk_starts_under_the_fog_with_alpha_0_7()
    {
        var fog = new FogTexture(0, 0, 1, 1);

        Assert.All(fog.Alpha, alpha => Assert.Equal(0.7f, alpha));
    }

    [Fact]
    public void A_generated_chunk_is_clear()
    {
        var fog = new FogTexture(10, 20, 12, 22);

        Assert.True(fog.SetGenerated(11, 21));

        Assert.Equal(0f, fog.AlphaAt(1, 1));
        Assert.Equal(0.7f, fog.AlphaAt(0, 1));
        Assert.Equal(0.7f, fog.AlphaAt(1, 2));
    }

    [Fact]
    public void The_first_row_is_the_south_edge_of_the_region_and_the_first_column_its_west_edge()
    {
        var fog = new FogTexture(-2, -3, 1, 2);

        fog.SetGenerated(-2, -3);
        fog.SetGenerated(1, 2);

        Assert.Equal(0f, fog.Alpha[0]);
        Assert.Equal(0f, fog.Alpha[fog.Alpha.Length - 1]);
        Assert.Equal(0.7f, fog.Alpha[1]);
    }

    [Fact]
    public void A_chunk_that_is_already_clear_changes_nothing()
    {
        var fog = new FogTexture(0, 0, 0, 0);
        fog.SetGenerated(0, 0);

        Assert.False(fog.SetGenerated(0, 0));
    }

    [Fact]
    public void A_chunk_outside_the_region_changes_nothing()
    {
        var fog = new FogTexture(0, 0, 0, 0);

        Assert.False(fog.SetGenerated(1, 0));
        Assert.False(fog.SetGenerated(0, -1));
        Assert.All(fog.Alpha, alpha => Assert.Equal(0.7f, alpha));
    }
}
