using MapOverhaul;
using MapOverhaul.Game;
using Xunit;

public class ScanRunnerTests : GameTestBase
{
    private readonly GameWorld game = new GameWorld();
    private double now;

    public ScanRunnerTests()
    {
        Plugin.Verbose.Value = true;
        ScanRunner.Clock = () => now;
    }

    private ChunkReader Reader() => new ChunkReader(game.World, game.Persistence, GameWorld.Slot);

    private void Fill()
    {
        game.File(0, 0, GameWorld.Saved(1, "cabin1", 1f, 1f), GameWorld.Saved(2, "iron_rock_resource_1", 5f, 5f));
        game.File(1, 0, GameWorld.Saved(3, "iron_rock_resource_1", 40f, 5f, level: -1), GameWorld.Saved(77, "stick", 40f, 5f));
        game.Loaded(2, 0, GameWorld.Live(4, "tin_rock_resource_1", 70f, 5f), GameWorld.Live(5, "cave_entrance_2", 72f, 5f));
    }

    [Fact]
    public void Begin_queues_the_chunks_and_logs_the_scan_start()
    {
        Fill();

        ScanRunner.Begin(Reader(), ChunkReader.GeneratedChunks(game.World));

        Assert.Equal(3, ScanRunner.Queue.Count);
        Assert.Contains("Scan start: 3 chunks", Plugin.Log.Debugs);
    }

    [Fact]
    public void A_step_reads_the_chunks_within_the_budget_into_the_index()
    {
        Fill();
        ScanRunner.Begin(Reader(), ChunkReader.GeneratedChunks(game.World));

        ScanRunner.Step(() => ScanRunner.Index.ChunkCount >= 2);

        Assert.Equal(2, ScanRunner.Index.ChunkCount);
        Assert.Equal(1, ScanRunner.Queue.Count);
        Assert.Equal(2, ScanRunner.Index.Get(new ChunkCoord(0, 0)).Count);
    }

    [Fact]
    public void The_scan_end_logs_the_time_the_counts_and_the_highest_entity_id_once()
    {
        Fill();
        ScanRunner.Begin(Reader(), ChunkReader.GeneratedChunks(game.World));
        now = 1000;

        ScanRunner.Step(() => { now += 0.5; return false; });
        ScanRunner.Step(() => false);

        Assert.Single(Plugin.Log.Debugs, line => line.StartsWith("Scan end"));
        Assert.Contains("Scan end: 1002 ms, longest frame 1.5 ms, 0 of 1 frames over 4 ms, 3 chunks, cabins 1, cave entrances 1 surface 0 cave,"
            + " tin 1 surface 0 cave, copper 0 surface 0 cave, iron 1 surface 1 cave, silver 0 surface 0 cave, highest entity id 77,"
            + " cache hits 0, file reads 2", Plugin.Log.Debugs);
    }

    [Fact]
    public void The_first_scan_gives_the_chunks_read_and_its_chunk_count()
    {
        Fill();
        Assert.Equal((0, 0), ScanRunner.Progress);
        ScanRunner.Begin(Reader(), ChunkReader.GeneratedChunks(game.World));
        Assert.Equal((0, 3), ScanRunner.Progress);

        ScanRunner.Step(() => ScanRunner.Index.ChunkCount >= 2);

        Assert.Equal((2, 3), ScanRunner.Progress);
    }

    [Fact]
    public void A_chunk_queued_again_during_the_first_scan_keeps_the_progress_below_its_end()
    {
        Fill();
        ScanRunner.Begin(Reader(), ChunkReader.GeneratedChunks(game.World));
        ScanRunner.Step(() => ScanRunner.Index.ChunkCount >= 2);

        ScanRunner.Queue.Front(new[] { new ChunkCoord(0, 0), new ChunkCoord(1, 0) });
        ScanRunner.Step(() => ScanRunner.Queue.Count <= 1);
        Assert.Equal(1, ScanRunner.Queue.Count);

        var (read, total) = ScanRunner.Progress;
        Assert.True(read < total, read + " of " + total);
        Assert.NotNull(LegendModel.ProgressText(read, total));
    }

    [Fact]
    public void There_are_no_progress_counts_after_the_scan_end_or_for_a_later_read()
    {
        Fill();
        ScanRunner.Begin(Reader(), ChunkReader.GeneratedChunks(game.World));
        ScanRunner.Step(() => false);
        Assert.Equal((0, 0), ScanRunner.Progress);

        ScanRunner.Queue.Add(new ChunkCoord(0, 0));
        ScanRunner.Queue.Add(new ChunkCoord(1, 0));
        ScanRunner.Step(() => true);

        Assert.Equal((0, 0), ScanRunner.Progress);
    }

    [Fact]
    public void A_chunk_read_over_20_ms_logs_its_chunk_time_source_and_entity_count()
    {
        Fill();
        game.Persistence.LoadHook = () => now += game.Persistence.Loads == 1 ? 25 : 20;
        ScanRunner.Begin(Reader(), ChunkReader.GeneratedChunks(game.World));

        ScanRunner.Step(() => false);

        Assert.Equal(new[] { "Slow read: chunk (0, 0) 25.0 ms, file, 2 entities" },
            Plugin.Log.Debugs.Where(line => line.StartsWith("Slow read")));
    }

    [Fact]
    public void A_slow_live_read_counts_all_entities_of_the_chunk()
    {
        game.Loaded(2, 0, GameWorld.Live(4, "tin_rock_resource_1", 70f, 5f), GameWorld.Live(5, "stick", 72f, 5f), GameWorld.Live(6, "stick", 73f, 5f));
        ScanRunner.Begin(Reader(), ChunkReader.GeneratedChunks(game.World));
        ScanRunner.Clock = () => now += 30;

        ScanRunner.Step(() => false);

        Assert.Contains(Plugin.Log.Debugs, line => line.StartsWith("Slow read: chunk (2, 0)") && line.EndsWith("live, 3 entities"));
    }

    private ChunkReader CachedReader()
    {
        ChunkReader.Stamp = path => new FileStamp(1, 1);
        return new ChunkReader(game.World, game.Persistence, GameWorld.Slot, CacheStore.Read(game.World));
    }

    [Fact]
    public void The_scan_end_writes_the_cache_and_a_second_scan_takes_the_chunks_from_it()
    {
        Fill();
        ScanRunner.Begin(CachedReader(), ChunkReader.GeneratedChunks(game.World));
        ScanRunner.Step(() => false);
        Assert.Contains(Plugin.Log.Debugs, line => line.StartsWith("Scan end") && line.EndsWith("cache hits 0, file reads 2"));
        var loads = game.Persistence.Loads;

        ScanRunner.Reset();
        Plugin.Log.Debugs.Clear();
        ScanRunner.Begin(CachedReader(), ChunkReader.GeneratedChunks(game.World));
        ScanRunner.Step(() => false);

        Assert.Contains(Plugin.Log.Debugs, line => line.StartsWith("Scan end") && line.EndsWith("cache hits 2, file reads 0"));
        Assert.Equal(loads, game.Persistence.Loads);
        Assert.Equal(3, ScanRunner.Index.ChunkCount);
    }

    [Fact]
    public void The_scan_end_writes_no_cache_when_it_did_not_change()
    {
        game.Loaded(2, 0, GameWorld.Live(4, "tin_rock_resource_1", 70f, 5f));
        ScanRunner.Begin(CachedReader(), ChunkReader.GeneratedChunks(game.World));

        ScanRunner.Step(() => false);

        Assert.False(Directory.Exists(CacheStore.Folder));
    }

    [Fact]
    public void The_scan_end_logs_the_distance_between_the_entrance_and_the_chunk_center_of_each_cave()
    {
        Fill();
        game.Info(1, 0, cave: true);
        game.Info(2, 0, closestCave: GameWorld.At(1, 0));
        game.Info(5, 5, cave: true);
        ScanRunner.Begin(Reader(), ChunkReader.GeneratedChunks(game.World));

        ScanRunner.Step(() => false);

        Assert.Single(Plugin.Log.Debugs, line => line.StartsWith("Cave "));
        Assert.Contains("Cave (1, 0): entrance 26.4 m from the chunk center", Plugin.Log.Debugs);
    }

    [Fact]
    public void The_scan_end_counts_the_frames_over_4_ms()
    {
        Fill();
        ScanRunner.Begin(Reader(), ChunkReader.GeneratedChunks(game.World));

        ScanRunner.Step(() => { now += 5; return true; });
        ScanRunner.Step(() => { now += 1; return true; });
        ScanRunner.Step(() => { now += 4; return true; });

        Assert.Contains(Plugin.Log.Debugs, line => line.StartsWith("Scan end: 10 ms, longest frame 5.0 ms, 1 of 3 frames over 4 ms, 3 chunks"));
    }

    [Fact]
    public void Update_steps_with_the_frame_budget_of_2_ms()
    {
        Fill();
        ScanRunner.Begin(Reader(), ChunkReader.GeneratedChunks(game.World));
        var runner = new ScanRunner(IntPtr.Zero);

        ScanRunner.Clock = () => now += 0.5;
        runner.Update();

        Assert.Equal(2, ScanRunner.Index.ChunkCount);
    }

    [Fact]
    public void A_step_with_no_reader_does_nothing()
    {
        ScanRunner.Queue.Add(new ChunkCoord(0, 0));

        ScanRunner.Step(() => false);

        Assert.Equal(1, ScanRunner.Queue.Count);
    }

    [Fact]
    public void A_later_read_after_the_scan_logs_no_scan_end()
    {
        Fill();
        ScanRunner.Begin(Reader(), ChunkReader.GeneratedChunks(game.World));
        ScanRunner.Step(() => false);

        ScanRunner.Queue.Add(new ChunkCoord(0, 0));
        ScanRunner.Step(() => false);

        Assert.Single(Plugin.Log.Debugs, line => line.StartsWith("Scan end"));
    }

    [Fact]
    public void A_failed_read_is_logged_once_and_the_scan_goes_on()
    {
        Fill();
        game.World.activeChunks.Add(GameWorld.At(5, 5), null);
        game.Loaded(6, 6).identifierToEntitiesMap = null;
        ScanRunner.Begin(Reader(), new[] { new ChunkCoord(6, 6), new ChunkCoord(6, 6), new ChunkCoord(0, 0) });
        ScanRunner.Queue.Add(new ChunkCoord(6, 6));

        ScanRunner.Step(() => false);

        Assert.Single(Plugin.Log.Warnings);
        Assert.Equal(1, ScanRunner.Index.ChunkCount);
    }

    [Fact]
    public void Reset_clears_the_index_the_queue_and_the_reader()
    {
        Fill();
        ScanRunner.Begin(Reader(), ChunkReader.GeneratedChunks(game.World));
        ScanRunner.Step(() => true);

        ScanRunner.Reset();

        Assert.Equal(0, ScanRunner.Index.ChunkCount);
        Assert.Equal(0, ScanRunner.Queue.Count);
        Assert.Null(ScanRunner.Reader);
    }
}
