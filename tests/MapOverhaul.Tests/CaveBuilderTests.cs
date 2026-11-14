using MapOverhaul;
using Xunit;

public class CaveBuilderTests
{
    private readonly List<string> log = new List<string>();

    private static readonly ChunkCoord Origin = new ChunkCoord(2, -3);

    private static Find Entrance(float x, float z, ChunkCoord origin, int level = 1) =>
        new Find(FindKind.CaveEntrance, Metal.Tin, x, 7f, z, level, origin, true);

    private static Find CaveRock(Metal metal, ChunkCoord origin, int level = -1) =>
        new Find(FindKind.OreRock, metal, 1f, 2f, 3f, level, origin, true);

    private List<Cave> Build(IEnumerable<CaveOrigin> origins, params Find[] finds) =>
        CaveBuilder.Build(origins.ToList(), finds, log.Add);

    [Fact]
    public void A_chunk_info_cave_with_no_read_entrance_is_at_the_center_of_its_origin_chunk()
    {
        var caves = Build(new[] { new CaveOrigin(Origin) });

        var cave = Assert.Single(caves);
        Assert.False(cave.HasEntrance);
        Assert.Equal(Origin, cave.Origin);
        Assert.Equal(2.5f * 32f, cave.X, 3);
        Assert.Equal(-2.5f * 32f, cave.Z, 3);
        Assert.Equal((cave.X, cave.Z), (cave.CenterX, cave.CenterZ));
        Assert.Equal(new[] { 0, 0, 0, 0 }, cave.Ores);
    }

    [Fact]
    public void An_entrance_moves_its_cave_to_the_entrance_and_clears_the_flag()
    {
        var caves = Build(new[] { new CaveOrigin(Origin) }, Entrance(70f, -90f, Origin));

        var cave = Assert.Single(caves);
        Assert.True(cave.HasEntrance);
        Assert.Equal((70f, -90f), (cave.X, cave.Z));
    }

    [Fact]
    public void Two_entrance_entities_at_one_place_give_one_cave()
    {
        var caves = Build(new[] { new CaveOrigin(Origin) },
            Entrance(70f, -90f, Origin), Entrance(70f, -90f, Origin, level: -1));

        var cave = Assert.Single(caves);
        Assert.Equal((70f, -90f), (cave.X, cave.Z));
    }

    [Fact]
    public void The_surface_entrance_wins_over_a_cave_level_entrance()
    {
        var caves = Build(new[] { new CaveOrigin(Origin) },
            Entrance(10f, 10f, Origin, level: -1), Entrance(70f, -90f, Origin));

        Assert.Equal((70f, -90f), (caves[0].X, caves[0].Z));
    }

    [Fact]
    public void Cave_ore_rocks_count_by_metal_toward_the_cave_of_their_origin()
    {
        var other = new ChunkCoord(9, 9);
        var caves = Build(new[] { new CaveOrigin(Origin), new CaveOrigin(other) },
            CaveRock(Metal.Iron, Origin), CaveRock(Metal.Iron, Origin), CaveRock(Metal.Silver, Origin), CaveRock(Metal.Tin, other));

        Assert.Equal(new[] { 0, 0, 2, 1 }, caves.Single(c => c.Origin.Equals(Origin)).Ores);
        Assert.Equal(new[] { 1, 0, 0, 0 }, caves.Single(c => c.Origin.Equals(other)).Ores);
    }

    [Fact]
    public void A_surface_ore_rock_does_not_count_toward_a_cave()
    {
        var caves = Build(new[] { new CaveOrigin(Origin) }, CaveRock(Metal.Iron, Origin, level: 1));

        Assert.Equal(new[] { 0, 0, 0, 0 }, caves[0].Ores);
    }

    [Fact]
    public void A_find_with_no_cave_origin_is_ignored()
    {
        var caves = Build(new[] { new CaveOrigin(Origin) },
            new Find(FindKind.CaveEntrance, Metal.Tin, 1f, 1f, 1f, 0, default, false),
            new Find(FindKind.OreRock, Metal.Iron, 1f, 1f, 1f, -1, default, false));

        Assert.False(caves[0].HasEntrance);
        Assert.Equal(new[] { 0, 0, 0, 0 }, caves[0].Ores);
        Assert.Empty(log);
    }

    [Fact]
    public void A_find_whose_origin_has_no_chunk_info_cave_is_logged_once_and_ignored()
    {
        var unknown = new ChunkCoord(40, 40);
        var caves = Build(new[] { new CaveOrigin(Origin) },
            Entrance(1f, 1f, unknown), CaveRock(Metal.Iron, unknown));

        Assert.False(Assert.Single(caves).HasEntrance);
        Assert.Equal(new[] { "No chunk info cave at (40, 40)" }, log);
    }
}
