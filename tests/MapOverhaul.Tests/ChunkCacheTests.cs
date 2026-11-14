using System.Linq;
using MapOverhaul;
using Xunit;

public class ChunkCacheTests
{
    private const string Mod = "1.0.0";
    private const string Game = "0.12.7 (21345678)";
    private const string FileA = @"session\chunk_1_2.dat";
    private const string FileB = @"slot36\chunk_-3_4.dat";

    private static readonly Find Cabin = new Find(FindKind.Cabin, Metal.Tin, 10.5f, 3.25f, -20.125f, 1, default, false);
    private static readonly Find CaveIron = new Find(FindKind.OreRock, Metal.Iron, -1.1f, -7.7f, 1e-3f, -1, new ChunkCoord(-5, 6), true);
    private static readonly Find Entrance = new Find(FindKind.CaveEntrance, Metal.Tin, 0.1f, 0.2f, 0.3f, 1, new ChunkCoord(7, -8), true);

    [Fact]
    public void A_stored_file_gives_its_finds_back_for_the_same_size_and_time()
    {
        var cache = new ChunkCache(Mod, Game);
        cache.Store(FileA, 1234, 638000000000000001, new[] { Cabin, CaveIron });

        Assert.True(cache.TryGet(FileA, 1234, 638000000000000001, out var finds));
        Assert.Equal(new[] { Cabin, CaveIron }, finds);
    }

    [Fact]
    public void Another_size_or_time_or_path_is_a_miss()
    {
        var cache = new ChunkCache(Mod, Game);
        cache.Store(FileA, 1234, 500, new[] { Cabin });

        Assert.False(cache.TryGet(FileA, 1235, 500, out _));
        Assert.False(cache.TryGet(FileA, 1234, 501, out _));
        Assert.False(cache.TryGet(FileB, 1234, 500, out _));
    }

    [Fact]
    public void A_store_replaces_the_entry_of_that_path()
    {
        var cache = new ChunkCache(Mod, Game);
        cache.Store(FileA, 1, 1, new[] { Cabin });
        cache.Store(FileA, 2, 2, new[] { Entrance });

        Assert.False(cache.TryGet(FileA, 1, 1, out _));
        Assert.True(cache.TryGet(FileA, 2, 2, out var finds));
        Assert.Equal(new[] { Entrance }, finds);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void A_file_with_no_finds_is_kept_as_an_empty_hit()
    {
        var cache = new ChunkCache(Mod, Game);
        cache.Store(FileA, 9, 9, new Find[0]);

        var parsed = ChunkCache.Parse(cache.Write(), Mod, Game, out _);

        Assert.True(parsed.TryGet(FileA, 9, 9, out var finds));
        Assert.Empty(finds);
    }

    [Fact]
    public void The_text_form_parses_to_the_same_entries()
    {
        var cache = new ChunkCache(Mod, Game);
        cache.Store(FileA, 1234, 638000000000000001, new[] { Cabin, CaveIron });
        cache.Store(FileB, 77, -5, new[] { Entrance });

        var parsed = ChunkCache.Parse(cache.Write(), Mod, Game, out var result);

        Assert.Equal(CacheParse.Ok, result);
        Assert.Equal(2, parsed.Count);
        Assert.True(parsed.TryGet(FileA, 1234, 638000000000000001, out var a));
        Assert.Equal(new[] { Cabin, CaveIron }, a);
        Assert.True(parsed.TryGet(FileB, 77, -5, out var b));
        Assert.Equal(new[] { Entrance }, b);
        Assert.False(parsed.Changed);
    }

    [Theory]
    [InlineData("2.0.0", Game)]
    [InlineData(Mod, "0.12.8 (21345679)")]
    public void A_header_of_another_mod_or_game_version_gives_an_empty_cache(string mod, string game)
    {
        var cache = new ChunkCache(Mod, Game);
        cache.Store(FileA, 1, 1, new[] { Cabin });

        var parsed = ChunkCache.Parse(cache.Write(), mod, game, out var result);

        Assert.Equal(CacheParse.OtherVersion, result);
        Assert.Equal(0, parsed.Count);
    }

    [Fact]
    public void A_header_of_another_format_version_gives_an_empty_cache()
    {
        var cache = new ChunkCache(Mod, Game);
        cache.Store(FileA, 1, 1, new[] { Cabin });
        var lines = cache.Write().Split('\n');
        lines[0] = lines[0].Replace("MapOverhaulCache\t" + ChunkCache.FormatVersion + "\t", "MapOverhaulCache\t999\t");

        var parsed = ChunkCache.Parse(string.Join("\n", lines), Mod, Game, out var result);

        Assert.Equal(CacheParse.OtherVersion, result);
        Assert.Equal(0, parsed.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a header\n")]
    [InlineData("session\\chunk_1_2.dat\t1\t1\t\n")]
    public void A_missing_header_gives_an_empty_broken_cache(string text)
    {
        var parsed = ChunkCache.Parse(text, Mod, Game, out var result);

        Assert.Equal(CacheParse.Broken, result);
        Assert.Equal(0, parsed.Count);
    }

    [Theory]
    [InlineData("session\\a.dat\tx\t1\t")]
    [InlineData("session\\a.dat\t1\t1")]
    [InlineData("session\\a.dat\t1\t1\tCabin,Tin,1,2")]
    [InlineData("session\\a.dat\t1\t1\tNoKind,Tin,1,2,3,1,0,0,0")]
    [InlineData("session\\a.dat\t1\t1\tCabin,Tin,1,2,3,1,maybe,0,0")]
    public void A_line_that_does_not_parse_gives_an_empty_broken_cache(string line)
    {
        var cache = new ChunkCache(Mod, Game);
        cache.Store(FileB, 1, 1, new[] { Cabin });
        var text = cache.Write() + line + "\n";

        var parsed = ChunkCache.Parse(text, Mod, Game, out var result);

        Assert.Equal(CacheParse.Broken, result);
        Assert.Equal(0, parsed.Count);
    }

    [Fact]
    public void A_broken_or_other_version_cache_keeps_the_running_versions()
    {
        var parsed = ChunkCache.Parse("", Mod, Game, out _);
        parsed.Store(FileA, 1, 1, new[] { Cabin });

        var again = ChunkCache.Parse(parsed.Write(), Mod, Game, out var result);

        Assert.Equal(CacheParse.Ok, result);
        Assert.Equal(1, again.Count);
    }

    [Fact]
    public void A_store_sets_the_changed_flag_and_a_write_clears_it()
    {
        var cache = new ChunkCache(Mod, Game);
        Assert.False(cache.Changed);

        cache.Store(FileA, 1, 1, new[] { Cabin });
        Assert.True(cache.Changed);

        cache.Write();
        Assert.False(cache.Changed);
    }

    [Fact]
    public void A_store_of_the_same_entry_sets_no_change()
    {
        var cache = new ChunkCache(Mod, Game);
        cache.Store(FileA, 1, 1, new[] { Cabin });
        cache.Write();

        cache.Store(FileA, 1, 1, new[] { Cabin });

        Assert.False(cache.Changed);
    }

    [Fact]
    public void Positions_keep_each_bit_through_the_text_form()
    {
        var odd = new Find(FindKind.OreRock, Metal.Silver, 1f / 3f, float.Epsilon, -123456.79f, 2, new ChunkCoord(int.MinValue, int.MaxValue), true);
        var cache = new ChunkCache(Mod, Game);
        cache.Store(FileA, long.MaxValue, long.MinValue, new[] { odd });

        var parsed = ChunkCache.Parse(cache.Write(), Mod, Game, out _);

        Assert.True(parsed.TryGet(FileA, long.MaxValue, long.MinValue, out var finds));
        Assert.Equal(odd, finds.Single());
    }
}
