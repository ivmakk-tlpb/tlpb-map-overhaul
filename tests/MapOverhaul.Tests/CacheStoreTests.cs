using MapOverhaul;
using MapOverhaul.Game;
using Xunit;

public class CacheStoreTests : GameTestBase
{
    private readonly GameWorld game = new GameWorld();
    private static readonly Find Cabin = new Find(FindKind.Cabin, Metal.Tin, 1f, 2f, 3f, 1, default, false);

    public CacheStoreTests()
    {
        Plugin.Verbose.Value = true;
    }

    private string CachePath => Path.Combine(CacheStore.Folder, "GD_01_45635.txt");

    [Fact]
    public void The_default_folder_is_in_the_cache_folder_of_bepinex()
    {
        Assert.Equal(@"C:\Game\BepInEx\cache\MapOverhaul", CacheStore.DefaultFolder());
    }

    [Fact]
    public void The_file_name_comes_from_the_game_and_world_names_with_other_characters_replaced()
    {
        game.World.game.name = "My Game: 2";
        game.World.name = "wörld/1-a_b";

        Assert.Equal("My_Game__2_w_rld_1-a_b.txt", CacheStore.FileName(game.World));
    }

    [Fact]
    public void A_write_creates_the_folder_and_a_read_gives_the_entries_back()
    {
        var cache = CacheStore.Read(game.World);
        cache.Store(@"session\a.dat", 10, 100, new[] { Cabin });

        CacheStore.Write(cache, game.World);
        var again = CacheStore.Read(game.World);

        Assert.True(File.Exists(CachePath));
        Assert.True(again.TryGet(@"session\a.dat", 10, 100, out var finds));
        Assert.Equal(new[] { Cabin }, finds);
        Assert.Empty(Plugin.Log.Warnings);
    }

    [Fact]
    public void A_write_of_an_unchanged_cache_writes_no_file()
    {
        CacheStore.Write(CacheStore.Read(game.World), game.World);

        Assert.False(File.Exists(CachePath));
    }

    [Fact]
    public void A_missing_file_gives_an_empty_cache_with_no_warning()
    {
        var cache = CacheStore.Read(game.World);

        Assert.Equal(0, cache.Count);
        Assert.Empty(Plugin.Log.Warnings);
        Assert.Contains("Cache: no file GD_01_45635.txt", Plugin.Log.Debugs);
    }

    [Fact]
    public void A_broken_file_gives_an_empty_cache_and_one_warning()
    {
        Directory.CreateDirectory(CacheStore.Folder);
        File.WriteAllText(CachePath, "garbage");

        var cache = CacheStore.Read(game.World);
        CacheStore.Read(game.World);

        Assert.Equal(0, cache.Count);
        Assert.Single(Plugin.Log.Warnings);
        Assert.Contains("broken", Plugin.Log.Warnings[0]);
    }

    [Fact]
    public void A_file_of_another_game_version_gives_an_empty_cache_with_no_warning()
    {
        var cache = CacheStore.Read(game.World);
        cache.Store(@"session\a.dat", 10, 100, new[] { Cabin });
        CacheStore.Write(cache, game.World);
        UnityEngine.Application.version = "0.12.8";

        var again = CacheStore.Read(game.World);

        Assert.Equal(0, again.Count);
        Assert.Empty(Plugin.Log.Warnings);
        Assert.Contains("Cache: GD_01_45635.txt is of another version", Plugin.Log.Debugs);
    }

    [Fact]
    public void A_read_gives_a_debug_line_with_the_entry_count()
    {
        var cache = CacheStore.Read(game.World);
        cache.Store(@"session\a.dat", 10, 100, new[] { Cabin });
        CacheStore.Write(cache, game.World);

        CacheStore.Read(game.World);

        Assert.Contains("Cache: read 1 chunk files from GD_01_45635.txt", Plugin.Log.Debugs);
    }

    [Fact]
    public void A_failed_write_logs_one_warning()
    {
        File.WriteAllText(CacheStore.Folder, "a file where the folder goes");
        var cache = CacheStore.Read(game.World);
        cache.Store(@"session\a.dat", 10, 100, new[] { Cabin });

        CacheStore.Write(cache, game.World);

        Assert.Single(Plugin.Log.Warnings);
    }
}
