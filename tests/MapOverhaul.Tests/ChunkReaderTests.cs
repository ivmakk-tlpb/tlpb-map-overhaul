using MapOverhaul;
using MapOverhaul.Game;
using Xunit;

public class ChunkReaderTests : GameTestBase
{
    private readonly GameWorld game = new GameWorld();

    private ChunkReader Reader() => new ChunkReader(game.World, game.Persistence, GameWorld.Slot);

    [Fact]
    public void A_loaded_chunk_reads_the_poi_entities_of_its_live_map()
    {
        game.Loaded(1, 2,
            GameWorld.Live(5, "cabin1", 40f, 70f),
            GameWorld.Live(6, "cabin_roof_large", 40f, 70f),
            GameWorld.Live(7, "iron_rock_resource_1", 41f, 75f),
            GameWorld.Live(8, "stick", 42f, 76f));

        var finds = Reader().Read(new ChunkCoord(1, 2));

        Assert.Equal(new[] { FindKind.Cabin, FindKind.OreRock }, finds.Select(f => f.Kind));
        Assert.Equal((41f, 5f, 75f), (finds[1].X, finds[1].Y, finds[1].Z));
        Assert.Equal(Metal.Iron, finds[1].Metal);
        Assert.Equal(0, game.Persistence.Loads);
    }

    [Fact]
    public void The_live_read_keeps_the_entity_level()
    {
        game.Loaded(0, 0, GameWorld.Live(5, "tin_rock_resource_1", 1f, 1f, level: -1));

        Assert.Equal(-1, Reader().Read(new ChunkCoord(0, 0))[0].Level);
    }

    [Fact]
    public void Another_chunk_reads_its_chunk_file_of_the_kept_slot()
    {
        game.File(3, -4,
            GameWorld.Saved(10, "copper_rock_resource_2", 100f, -120f),
            GameWorld.Saved(11, "twig", 100f, -120f),
            GameWorld.Saved(12, "cave_entrance_2", 101f, -121f, level: 1));

        var finds = Reader().Read(new ChunkCoord(3, -4));

        Assert.Equal(new[] { FindKind.OreRock, FindKind.CaveEntrance }, finds.Select(f => f.Kind));
        Assert.Equal(Metal.Copper, finds[0].Metal);
        Assert.Equal((100f, -120f), (finds[0].X, finds[0].Z));
        Assert.Equal(1, game.Persistence.Loads);
    }

    [Fact]
    public void The_file_read_keeps_the_entity_level()
    {
        game.File(0, 0, GameWorld.Saved(5, "iron_rock_resource_2", 1f, 1f, level: -1));

        Assert.Equal(-1, Reader().Read(new ChunkCoord(0, 0))[0].Level);
    }

    [Fact]
    public void A_failed_load_gives_an_empty_read_and_one_verbose_line()
    {
        Plugin.Verbose.Value = true;

        var finds = Reader().Read(new ChunkCoord(9, 9));

        Assert.Empty(finds);
        Assert.Equal(new[] { "No chunk file for chunk (9, 9)" }, Plugin.Log.Debugs);
    }

    [Fact]
    public void A_surface_cave_entrance_and_a_cave_ore_rock_get_their_cave_origin()
    {
        game.Info(3, 3, closestCave: GameWorld.At(4, 5));
        game.File(3, 3,
            GameWorld.Saved(1, "cave_entrance_2", 100f, 100f, level: 1),
            GameWorld.Saved(2, "cave_entrance_tunnel_4", 100f, 100f, level: -1),
            GameWorld.Saved(3, "iron_rock_resource_1", 101f, 101f, level: -1),
            GameWorld.Saved(4, "iron_rock_resource_1", 102f, 102f, level: 1));

        var finds = Reader().Read(new ChunkCoord(3, 3));

        Assert.Equal(new[] { true, false, true, false }, finds.Select(f => f.HasCave));
        Assert.Equal(new ChunkCoord(4, 5), finds[0].CaveOrigin);
        Assert.Equal(new ChunkCoord(4, 5), finds[2].CaveOrigin);
    }

    [Fact]
    public void A_chunk_with_no_nearby_cave_gives_no_cave_origin()
    {
        game.Info(3, 3);
        game.File(3, 3, GameWorld.Saved(3, "iron_rock_resource_1", 101f, 101f, level: -1));
        game.File(6, 6, GameWorld.Saved(4, "iron_rock_resource_1", 101f, 101f, level: -1));

        Assert.False(Reader().Read(new ChunkCoord(3, 3))[0].HasCave);
        Assert.False(Reader().Read(new ChunkCoord(6, 6))[0].HasCave);
    }

    [Fact]
    public void The_reads_keep_the_highest_game_entity_id()
    {
        game.Loaded(0, 0, GameWorld.Live(70, "cabin2", 1f, 1f));
        game.File(1, 0, GameWorld.Saved(900, "stick", 1f, 1f), GameWorld.Saved(40, "cabin1", 1f, 1f));
        var reader = Reader();

        reader.Read(new ChunkCoord(0, 0));
        reader.Read(new ChunkCoord(1, 0));

        Assert.Equal(900, reader.HighestId);
    }

    [Fact]
    public void The_chunk_info_read_gives_each_cave_origin_chunk()
    {
        game.Info(0, 0);
        game.Info(2, -1, cave: true);
        game.Info(5, 5, cave: true);

        var origins = ChunkReader.CaveOrigins(game.World);

        Assert.Equal(new[] { new ChunkCoord(2, -1), new ChunkCoord(5, 5) }, origins.Select(o => o.Chunk));
    }

    [Fact]
    public void The_generated_chunks_come_from_the_world()
    {
        game.File(1, 1);
        game.Loaded(2, 2);

        Assert.Equal(new[] { new ChunkCoord(1, 1), new ChunkCoord(2, 2) }, ChunkReader.GeneratedChunks(game.World));
    }

    [Fact]
    public void The_loaded_chunks_come_from_the_world()
    {
        game.File(1, 1);
        game.Loaded(2, 2);

        Assert.Equal(new[] { new ChunkCoord(2, 2) }, ChunkReader.LoadedChunks(game.World));
    }

    private const string WorldDir = @"C:\saves\GD_01\45635";
    private readonly Dictionary<string, FileStamp> stamps = new Dictionary<string, FileStamp>();
    private readonly ChunkCache cache = new ChunkCache("1.0.0", "0.12.7");

    private ChunkReader CachedReader()
    {
        ChunkReader.Stamp = path => stamps.TryGetValue(path, out var stamp) ? stamp : (FileStamp?)null;
        return new ChunkReader(game.World, game.Persistence, GameWorld.Slot, cache);
    }

    [Theory]
    [InlineData(true, true, true, @"session\chunk_entities_3_4.dat")]
    [InlineData(false, true, true, @"slot36\chunk_entities_3_4.dat")]
    [InlineData(false, false, true, @"slot36_chunk_entities_3_4.dat")]
    public void A_chunk_file_resolves_the_path_that_the_game_reads(bool session, bool slot, bool old, string key)
    {
        game.File(3, 4, GameWorld.Saved(1, "cabin1", 100f, 130f));
        if (session) stamps[WorldDir + @"\session\chunk_entities_3_4.dat"] = new FileStamp(10, 100);
        if (slot) stamps[WorldDir + @"\slot36\chunk_entities_3_4.dat"] = new FileStamp(20, 200);
        if (old) stamps[WorldDir + @"\slot36_chunk_entities_3_4.dat"] = new FileStamp(30, 300);
        var stamp = stamps[WorldDir + @"\" + key];

        CachedReader().Read(new ChunkCoord(3, 4));

        Assert.True(cache.TryGet(key, stamp.Size, stamp.WriteTicks, out var finds));
        Assert.Equal(FindKind.Cabin, finds.Single().Kind);
    }

    [Fact]
    public void A_cache_hit_gives_the_cached_finds_with_no_game_read()
    {
        stamps[WorldDir + @"\session\chunk_entities_3_4.dat"] = new FileStamp(10, 100);
        var cabin = new Find(FindKind.Cabin, Metal.Tin, 1f, 2f, 3f, 1, default, false);
        cache.Store(@"session\chunk_entities_3_4.dat", 10, 100, new[] { cabin });
        var reader = CachedReader();

        var finds = reader.Read(new ChunkCoord(3, 4));

        Assert.Equal(new[] { cabin }, finds);
        Assert.Equal(0, game.Persistence.Loads);
        Assert.Equal((1, 0), (reader.CacheHits, reader.FileReads));
    }

    [Fact]
    public void A_miss_reads_the_file_and_stores_its_finds()
    {
        game.File(3, 4, GameWorld.Saved(1, "iron_rock_resource_1", 100f, 130f), GameWorld.Saved(2, "stick", 100f, 130f));
        stamps[WorldDir + @"\session\chunk_entities_3_4.dat"] = new FileStamp(10, 100);
        cache.Store(@"session\chunk_entities_3_4.dat", 10, 99, new Find[0]);
        var reader = CachedReader();

        var finds = reader.Read(new ChunkCoord(3, 4));

        Assert.Equal(Metal.Iron, finds.Single().Metal);
        Assert.Equal(1, game.Persistence.Loads);
        Assert.Equal((0, 1), (reader.CacheHits, reader.FileReads));
        Assert.True(cache.TryGet(@"session\chunk_entities_3_4.dat", 10, 100, out var stored));
        Assert.Equal(finds, stored);
    }

    [Fact]
    public void A_failed_game_read_stores_nothing()
    {
        stamps[WorldDir + @"\session\chunk_entities_3_4.dat"] = new FileStamp(10, 100);

        CachedReader().Read(new ChunkCoord(3, 4));

        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void A_chunk_with_no_file_on_disk_reads_through_the_game_and_stores_nothing()
    {
        game.File(3, 4, GameWorld.Saved(1, "cabin1", 100f, 130f));

        var finds = CachedReader().Read(new ChunkCoord(3, 4));

        Assert.Single(finds);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void A_loaded_chunk_is_not_stored()
    {
        game.Loaded(3, 4, GameWorld.Live(5, "cabin1", 100f, 130f));
        stamps[WorldDir + @"\session\chunk_entities_3_4.dat"] = new FileStamp(10, 100);
        var reader = CachedReader();

        reader.Read(new ChunkCoord(3, 4));

        Assert.Equal(0, cache.Count);
        Assert.Equal((0, 0), (reader.CacheHits, reader.FileReads));
    }

    private const string Manifest = WorldDir + @"\slot36\chunks.manifest";
    private const string PackedKey = @"slot36\chunks.manifest#3.4";

    private ChunkReader PackedReader(byte[] manifest)
    {
        ChunkReader.ReadBytes = path => path == Manifest ? manifest : null;
        return CachedReader();
    }

    private static long Id(int first) => System.BitConverter.ToInt64(Enumerable.Range(first, 8).Select(i => (byte)i).ToArray(), 0);

    [Fact]
    public void A_chunk_of_the_slot_manifest_is_stamped_with_the_id_of_its_entities_record()
    {
        game.File(3, 4, GameWorld.Saved(1, "cabin1", 100f, 130f));
        stamps[WorldDir + @"\slot36\chunk_entities_3_4.dat"] = new FileStamp(20, 200);
        var reader = PackedReader(PackedManifestTests.Manifest(entries: new[] { (3, 4, 1, 10), (3, 4, 3, 50) }));

        reader.Read(new ChunkCoord(3, 4));

        Assert.True(cache.TryGet(PackedKey, Id(50), Id(58), out var finds));
        Assert.Equal(FindKind.Cabin, finds.Single().Kind);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void A_packed_cache_hit_gives_the_cached_finds_with_no_game_read()
    {
        var cabin = new Find(FindKind.Cabin, Metal.Tin, 1f, 2f, 3f, 1, default, false);
        cache.Store(PackedKey, Id(50), Id(58), new[] { cabin });
        var reader = PackedReader(PackedManifestTests.Manifest(entries: new[] { (3, 4, 3, 50) }));

        Assert.Equal(new[] { cabin }, reader.Read(new ChunkCoord(3, 4)));
        Assert.Equal(0, game.Persistence.Loads);
    }

    [Fact]
    public void A_complete_manifest_with_no_record_of_the_chunk_reads_through_the_game_and_stores_nothing()
    {
        game.File(3, 4, GameWorld.Saved(1, "cabin1", 100f, 130f));
        stamps[WorldDir + @"\slot36\chunk_entities_3_4.dat"] = new FileStamp(20, 200);
        var reader = PackedReader(PackedManifestTests.Manifest(entries: new[] { (5, 5, 3, 50) }));

        Assert.Single(reader.Read(new ChunkCoord(3, 4)));
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void An_incomplete_manifest_with_no_record_of_the_chunk_uses_the_legacy_file_stamp()
    {
        game.File(3, 4, GameWorld.Saved(1, "cabin1", 100f, 130f));
        stamps[WorldDir + @"\slot36\chunk_entities_3_4.dat"] = new FileStamp(20, 200);
        var reader = PackedReader(PackedManifestTests.Manifest(flags: 0, entries: new[] { (5, 5, 3, 50) }));

        reader.Read(new ChunkCoord(3, 4));

        Assert.True(cache.TryGet(@"slot36\chunk_entities_3_4.dat", 20, 200, out _));
    }

    [Fact]
    public void A_manifest_that_does_not_parse_gives_no_stamp_and_one_verbose_line()
    {
        Plugin.Verbose.Value = true;
        game.File(3, 4, GameWorld.Saved(1, "cabin1", 100f, 130f));
        game.File(5, 5, GameWorld.Saved(2, "cabin1", 100f, 130f));
        stamps[WorldDir + @"\slot36\chunk_entities_3_4.dat"] = new FileStamp(20, 200);
        var reader = PackedReader(PackedManifestTests.Manifest(version: 3));

        reader.Read(new ChunkCoord(3, 4));
        reader.Read(new ChunkCoord(5, 5));

        Assert.Equal(0, cache.Count);
        Assert.Equal(2, game.Persistence.Loads);
        Assert.Equal(new[] { @"Chunk form: no cache, the manifest slot36\chunks.manifest does not parse (version 3)" }, Plugin.Log.Debugs);
    }

    [Fact]
    public void The_first_file_read_names_the_packed_form_in_one_verbose_line()
    {
        Plugin.Verbose.Value = true;
        game.File(3, 4, GameWorld.Saved(1, "cabin1", 100f, 130f));
        game.File(5, 5, GameWorld.Saved(2, "cabin1", 100f, 130f));
        var reader = PackedReader(PackedManifestTests.Manifest(entries: new[] { (3, 4, 3, 50), (5, 5, 3, 90) }));

        reader.Read(new ChunkCoord(3, 4));
        reader.Read(new ChunkCoord(5, 5));

        Assert.Equal(new[] { @"Chunk form: packed, slot36\chunks.manifest with 2 entities records, complete" }, Plugin.Log.Debugs);
    }

    [Fact]
    public void The_first_file_read_names_the_legacy_form_in_one_verbose_line()
    {
        Plugin.Verbose.Value = true;
        game.File(3, 4, GameWorld.Saved(1, "cabin1", 100f, 130f));
        var reader = PackedReader(null);

        reader.Read(new ChunkCoord(3, 4));
        reader.Read(new ChunkCoord(3, 4));

        Assert.Equal(new[] { "Chunk form: legacy chunk files" }, Plugin.Log.Debugs);
    }

    [Fact]
    public void A_changed_chunk_reads_through_the_game_and_stores_nothing()
    {
        game.File(3, 4, GameWorld.Saved(1, "iron_rock_resource_1", 100f, 130f));
        stamps[WorldDir + @"\session\chunk_entities_3_4.dat"] = new FileStamp(10, 100);
        var stale = new Find(FindKind.Cabin, Metal.Tin, 1f, 2f, 3f, 1, default, false);
        cache.Store(@"session\chunk_entities_3_4.dat", 10, 100, new[] { stale });
        var reader = CachedReader();

        reader.MarkChanged(new ChunkCoord(3, 4));
        var finds = reader.Read(new ChunkCoord(3, 4));

        Assert.Equal(Metal.Iron, finds.Single().Metal);
        Assert.Equal((0, 1), (reader.CacheHits, reader.FileReads));
        Assert.True(cache.TryGet(@"session\chunk_entities_3_4.dat", 10, 100, out var kept));
        Assert.Equal(new[] { stale }, kept);
    }

    [Fact]
    public void After_a_save_into_another_slot_the_read_takes_the_chunk_of_the_new_slot()
    {
        game.File(3, 4, GameWorld.Saved(1, "cabin1", 100f, 130f));
        game.Persistence.Files[("auto2", 3, 4)] = new ChunkEntitiesSave();
        game.Persistence.Files[("auto2", 3, 4)].serializedEntities.Add(GameWorld.Saved(2, "iron_rock_resource_1", 100f, 130f));
        var slot = GameWorld.Slot;
        var reader = Reader();
        reader.CurrentSlot = () => slot;
        Assert.Equal(FindKind.Cabin, reader.Read(new ChunkCoord(3, 4)).Single().Kind);

        slot = "auto2";

        Assert.Equal(Metal.Iron, reader.Read(new ChunkCoord(3, 4)).Single().Metal);
    }

    [Fact]
    public void After_a_save_into_another_slot_the_stamp_comes_from_the_manifest_of_the_new_slot()
    {
        game.File(3, 4, GameWorld.Saved(1, "cabin1", 100f, 130f));
        game.Persistence.Files[("auto2", 3, 4)] = game.Persistence.Files[(GameWorld.Slot, 3, 4)];
        var manifests = new Dictionary<string, byte[]>
        {
            [Manifest] = PackedManifestTests.Manifest(entries: new[] { (3, 4, 3, 50) }),
            [WorldDir + @"\auto2\chunks.manifest"] = PackedManifestTests.Manifest(entries: new[] { (3, 4, 3, 90) }),
        };
        ChunkReader.ReadBytes = path => manifests.TryGetValue(path, out var bytes) ? bytes : null;
        var slot = GameWorld.Slot;
        var reader = CachedReader();
        reader.CurrentSlot = () => slot;
        reader.Read(new ChunkCoord(3, 4));

        slot = "auto2";
        reader.Read(new ChunkCoord(3, 4));

        Assert.True(cache.TryGet(PackedKey, Id(50), Id(58), out _));
        Assert.True(cache.TryGet(@"auto2\chunks.manifest#3.4", Id(90), Id(98), out _));
    }

    [Fact]
    public void The_disk_bytes_give_the_content_of_a_file_or_null()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });

            Assert.Equal(new byte[] { 1, 2, 3 }, ChunkReader.DiskBytes(path));
            Assert.Null(ChunkReader.DiskBytes(path + ".missing"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void The_disk_stamp_gives_the_size_and_the_write_time_of_a_file()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "abc");
            var time = new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(path, time);

            Assert.Equal(new FileStamp(3, time.Ticks), ChunkReader.DiskStamp(path));
            Assert.Null(ChunkReader.DiskStamp(path + ".missing"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
