using MapOverhaul;
using Xunit;

public class PoiBuilderTests
{
    private static readonly ChunkCoord Origin = new ChunkCoord(0, 0);

    private readonly PoiIndex index = new PoiIndex();
    private readonly List<string> log = new List<string>();
    private readonly PoiBuilder builder;

    public PoiBuilderTests()
    {
        builder = new PoiBuilder(index, log.Add);
        builder.SetCaveOrigins(new[] { new CaveOrigin(Origin) });
    }

    private static Find Make(FindKind kind, Metal metal, float x, float z, int level = 0, bool hasCave = false) =>
        new Find(kind, metal, x, 0f, z, level, Origin, hasCave);

    [Fact]
    public void The_list_holds_each_cabin_cave_and_deposit()
    {
        index.Put(new ChunkCoord(0, 0), new[]
        {
            Make(FindKind.Cabin, Metal.Tin, 3, 4),
            Make(FindKind.OreRock, Metal.Iron, 10, 10),
            Make(FindKind.OreRock, Metal.Iron, 12, 10),
            Make(FindKind.OreRock, Metal.Silver, 5, 5, level: -1, hasCave: true),
        });
        index.Put(new ChunkCoord(5, 5), new[] { Make(FindKind.Cabin, Metal.Tin, 170, 170) });

        var pois = builder.Get();

        Assert.Equal(new[] { PoiKind.Cabin, PoiKind.Cabin, PoiKind.Cave, PoiKind.Iron }, pois.Select(p => p.Kind).OrderBy(k => k));
        var deposit = pois.Single(p => p.Kind == PoiKind.Iron);
        Assert.Equal(2, deposit.Count);
        Assert.Equal(11f, deposit.X, 3);
        var cave = pois.Single(p => p.Kind == PoiKind.Cave);
        Assert.Equal((16f, 16f), (cave.X, cave.Z));
        Assert.Equal(new[] { 0, 0, 0, 1 }, cave.CaveOres);
        Assert.Contains(pois, p => p.Kind == PoiKind.Cabin && p.X == 170f && p.Z == 170f);
    }

    [Fact]
    public void Each_metal_gives_its_own_poi_kind()
    {
        index.Put(Origin, new[]
        {
            Make(FindKind.OreRock, Metal.Tin, 0, 0),
            Make(FindKind.OreRock, Metal.Copper, 100, 0),
            Make(FindKind.OreRock, Metal.Silver, 200, 0),
        });

        Assert.Equal(new[] { PoiKind.Cave, PoiKind.Tin, PoiKind.Copper, PoiKind.Silver }, builder.Get().Select(p => p.Kind).OrderBy(k => k));
    }

    [Fact]
    public void The_list_is_built_again_only_after_a_change_of_the_index()
    {
        index.Put(Origin, new[] { Make(FindKind.Cabin, Metal.Tin, 3, 4) });
        var first = builder.Get();
        int version = builder.Version;

        Assert.Same(first, builder.Get());
        Assert.Equal(version, builder.Version);
        Assert.False(index.Changed);

        index.Put(Origin, new[] { Make(FindKind.Cabin, Metal.Tin, 5, 4) });

        Assert.NotSame(first, builder.Get());
        Assert.Equal(version + 1, builder.Version);
    }

    [Fact]
    public void New_cave_origins_build_the_list_again()
    {
        var first = builder.Get();

        builder.SetCaveOrigins(new[] { new CaveOrigin(Origin), new CaveOrigin(new ChunkCoord(3, 3)) });

        Assert.Equal(2, builder.Get().Count(p => p.Kind == PoiKind.Cave));
        Assert.NotSame(first, builder.Get());
    }

    [Fact]
    public void A_cave_log_line_goes_to_the_log()
    {
        index.Put(Origin, new[] { new Find(FindKind.CaveEntrance, Metal.Tin, 1, 1, 1, 0, new ChunkCoord(8, 8), true) });

        builder.Get();

        Assert.Single(log);
    }
}
