using MapOverhaul;
using MapOverhaul.Game;
using MapOverhaul.Patches;
using Xunit;

public class ScanPatchesTests : GameTestBase
{
    private readonly GameWorld game;

    public ScanPatchesTests()
    {
        game = new GameWorld().InScene();
        Plugin.Verbose.Value = true;
        game.Info(0, 0, cave: true);
        game.File(0, 0, GameWorld.Saved(1, "cabin1", 1f, 1f));
        game.File(1, 0, GameWorld.Saved(2, "cabin2", 40f, 1f));
    }

    private void Activate(World world = null) => ScanPatches.SetWorldStatePostfix(world ?? game.World, WorldState.ACTIVE);

    [Fact]
    public void Active_starts_the_scan_of_the_generated_chunks_with_the_kept_slot()
    {
        Activate();

        Assert.Equal(2, ScanRunner.Queue.Count);
        ScanRunner.Step(() => false);
        Assert.Equal(2, ScanRunner.Index.ChunkCount);
        Assert.Contains("Scan start: 2 chunks", Plugin.Log.Debugs);
    }

    [Fact]
    public void Active_reads_the_chunk_info_caves()
    {
        Activate();

        Assert.Single(ScanRunner.Builder.Get(), p => p.Kind == PoiKind.Cave);
    }

    [Theory]
    [InlineData(WorldState.LOADING)]
    [InlineData(WorldState.LOADED)]
    [InlineData(WorldState.UNLOADED)]
    public void Another_state_starts_no_scan(WorldState state)
    {
        ScanPatches.SetWorldStatePostfix(game.World, state);

        Assert.Null(ScanRunner.Reader);
    }

    [Fact]
    public void A_second_active_of_the_same_world_starts_no_new_scan()
    {
        Activate();
        ScanRunner.Step(() => false);

        Activate();

        Assert.Equal(0, ScanRunner.Queue.Count);
        Assert.Single(Plugin.Log.Debugs, line => line.StartsWith("Scan start"));
        Assert.Contains("SetWorldState: the scan of this world ran", Plugin.Log.Debugs);
    }

    [Fact]
    public void Active_with_no_game_manager_starts_no_scan()
    {
        UnityEngine.Object.Scene.Clear();

        Activate();

        Assert.Null(ScanRunner.Reader);
        Assert.Contains("SetWorldState: no game manager", Plugin.Log.Debugs);
    }

    [Fact]
    public void A_generated_chunk_goes_into_the_queue()
    {
        Activate();
        ScanRunner.Step(() => false);

        ScanPatches.AddGeneratedChunkPostfix(game.World, new Chunk { coordinates = GameWorld.At(7, 8) });

        Assert.Equal(1, ScanRunner.Queue.Count);
        Assert.Contains("Queue add (generated): (7, 8)", Plugin.Log.Debugs);
    }

    [Fact]
    public void An_unloaded_chunk_goes_into_the_queue()
    {
        Activate();
        ScanRunner.Step(() => false);

        ScanPatches.UnloadChunkPostfix(new Chunk { coordinates = GameWorld.At(0, 0) });

        Assert.Equal(1, ScanRunner.Queue.Count);
        Assert.Contains("Queue add (unloaded): (0, 0)", Plugin.Log.Debugs);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_generated_or_unloaded_chunk_reads_again_with_no_cache(bool unloaded)
    {
        ChunkReader.Stamp = path => new FileStamp(1, 1);
        Activate();
        ScanRunner.Step(() => false);
        Assert.Equal((0, 2), (ScanRunner.Reader.CacheHits, ScanRunner.Reader.FileReads));

        var chunk = new Chunk { coordinates = GameWorld.At(0, 0) };
        if (unloaded) ScanPatches.UnloadChunkPostfix(chunk);
        else ScanPatches.AddGeneratedChunkPostfix(game.World, chunk);
        ScanRunner.Step(() => false);

        Assert.Equal((0, 3), (ScanRunner.Reader.CacheHits, ScanRunner.Reader.FileReads));
    }

    [Fact]
    public void A_read_after_a_save_into_another_slot_takes_the_chunk_of_that_slot()
    {
        Activate();
        ScanRunner.Step(() => false);
        game.Persistence.Files[("auto2", 1, 0)] = new ChunkEntitiesSave();
        game.Persistence.Files[("auto2", 1, 0)].serializedEntities.Add(GameWorld.Saved(3, "iron_rock_resource_1", 40f, 1f));

        Session.Manager().currentGameSlotName = "auto2";
        ScanPatches.UnloadChunkPostfix(new Chunk { coordinates = GameWorld.At(1, 0) });
        ScanRunner.Step(() => false);

        Assert.Equal(Metal.Iron, ScanRunner.Index.Get(new ChunkCoord(1, 0)).Single().Metal);
    }

    [Fact]
    public void A_chunk_before_the_scan_start_is_left_out()
    {
        ScanPatches.AddGeneratedChunkPostfix(game.World, new Chunk { coordinates = GameWorld.At(7, 8) });
        ScanPatches.UnloadChunkPostfix(new Chunk { coordinates = GameWorld.At(7, 8) });

        Assert.Equal(0, ScanRunner.Queue.Count);
    }

    [Fact]
    public void A_generated_chunk_of_another_world_is_left_out()
    {
        Activate();

        ScanPatches.AddGeneratedChunkPostfix(new World(), new Chunk { coordinates = GameWorld.At(7, 8) });

        Assert.Equal(2, ScanRunner.Queue.Count);
    }

    [Fact]
    public void A_world_unload_clears_the_state_so_the_next_active_scans_again()
    {
        Activate();
        ScanRunner.Step(() => false);
        FogLayer.Model = new FogTexture(0, 0, 0, 0);

        ScanPatches.UnloadWorldPostfix(game.World);

        Assert.Equal(0, ScanRunner.Index.ChunkCount);
        Assert.Null(ScanRunner.Reader);
        Assert.Null(FogLayer.Model);
        Assert.Contains("World unload: POI state cleared", Plugin.Log.Debugs);
        Activate();
        Assert.Equal(2, ScanRunner.Queue.Count);
    }

    [Fact]
    public void Active_reads_the_cache_file_of_the_world_for_the_scan()
    {
        var cache = CacheStore.Read(game.World);
        cache.Store(@"session\chunk_entities_0_0.dat", 1, 1, new[] { new Find(FindKind.Cabin, Metal.Tin, 9f, 9f, 9f, 1, default, false) });
        CacheStore.Write(cache, game.World);
        ChunkReader.Stamp = path => new FileStamp(1, 1);

        Activate();
        ScanRunner.Step(() => false);

        Assert.Equal(9f, ScanRunner.Index.Get(new ChunkCoord(0, 0)).Single().X);
        Assert.Equal((1, 1), (ScanRunner.Reader?.CacheHits ?? -1, game.Persistence.Loads));
    }

    [Fact]
    public void A_world_unload_writes_the_cache_when_it_changed()
    {
        ChunkReader.Stamp = path => new FileStamp(1, 1);
        Activate();
        ScanRunner.Step(() => true);
        Assert.False(File.Exists(Path.Combine(CacheStore.Folder, "GD_01_45635.txt")));

        ScanPatches.UnloadWorldPostfix(game.World);

        var again = CacheStore.Read(game.World);
        Assert.Equal(1, again.Count);
    }

    [Fact]
    public void A_network_game_does_nothing_and_logs_one_line()
    {
        game.Network.Networked = true;

        Activate();
        Activate();
        ScanPatches.AddGeneratedChunkPostfix(game.World, new Chunk { coordinates = GameWorld.At(7, 8) });

        Assert.Null(ScanRunner.Reader);
        Assert.Equal(0, ScanRunner.Queue.Count);
        Assert.Single(Plugin.Log.Infos, line => line == "Map Overhaul is off in multiplayer games.");
    }

    [Fact]
    public void An_exception_is_caught_and_logged_once()
    {
        game.World.chunkInfos = null;

        Activate();
        Activate();

        Assert.Single(Plugin.Log.Warnings);
    }

    [Fact]
    public void Each_hook_catches_its_exception()
    {
        Activate();

        ScanPatches.AddGeneratedChunkPostfix(game.World, null);
        ScanPatches.UnloadChunkPostfix(null);

        Assert.Equal(2, Plugin.Log.Warnings.Count);
    }
}
