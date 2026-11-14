using System;
using System.Collections.Generic;
using System.IO;

namespace MapOverhaul.Game
{
    // The size and the last write time (UTC ticks) of a file.
    internal readonly struct FileStamp : IEquatable<FileStamp>
    {
        internal readonly long Size;
        internal readonly long WriteTicks;

        internal FileStamp(long size, long writeTicks)
        {
            Size = size;
            WriteTicks = writeTicks;
        }

        public bool Equals(FileStamp other) => Size == other.Size && WriteTicks == other.WriteTicks;

        public override bool Equals(object obj) => obj is FileStamp other && Equals(other);

        public override int GetHashCode() => unchecked((int)Size * 397 ^ WriteTicks.GetHashCode());
    }

    // Reads the POI entities of one chunk: the live entities of a loaded chunk, otherwise the chunk file of the
    // save slot through the game's own reader. A surface cave entrance and an ore rock inside a cave get the
    // cave origin chunk of the chunk info.
    internal sealed class ChunkReader
    {
        private readonly World world;
        private readonly PersistenceManager persistence;
        private readonly string slotName;
        private readonly HashSet<(int X, int Y)> changed = new HashSet<(int X, int Y)>();

        // The slot manifest of the packed save layout, read at the first file read of each slot.
        private const string ManifestFile = "chunks.manifest";
        private string formSlot;
        private bool noStamp;
        private PackedManifest manifest;
        private string manifestKey;

        internal ChunkReader(World world, PersistenceManager persistence, string slotName, ChunkCache cache = null)
        {
            this.world = world;
            this.persistence = persistence;
            this.slotName = slotName;
            Cache = cache;
        }

        // The size and the write time of a file, or null when it does not exist. A test sets it.
        internal static Func<string, FileStamp?> Stamp = DiskStamp;

        internal static FileStamp? DiskStamp(string path)
        {
            var info = new FileInfo(path);
            return info.Exists ? new FileStamp(info.Length, info.LastWriteTimeUtc.Ticks) : (FileStamp?)null;
        }

        // The content of a file, or null when it does not exist. A test sets it.
        internal static Func<string, byte[]> ReadBytes = DiskBytes;

        internal static byte[] DiskBytes(string path) => File.Exists(path) ? File.ReadAllBytes(path) : null;

        // The slot of the last save. A save into another slot (an autosave) changes it, and the game then reads that
        // slot, so the reader asks at each read. Null keeps the slot of the load.
        internal Func<string> CurrentSlot;

        private string Slot() => CurrentSlot?.Invoke() ?? slotName;

        // The game keeps the record of a chunk that it generates or unloads in its session, which no file on disk
        // stamps, so such a chunk reads through the game with no cache for the rest of the world session.
        internal void MarkChanged(ChunkCoord chunk) => changed.Add((chunk.X, chunk.Y));

        // The finds of the chunk files read before, or null for no cache.
        internal ChunkCache Cache { get; }

        internal int CacheHits { get; private set; }

        internal int FileReads { get; private set; }

        // The highest game entity id of the reads, to check against the id range of the mod.
        internal int HighestId { get; private set; }

        internal World World => world;

        internal List<Find> Read(ChunkCoord chunk)
        {
            var coordinates = new Coordinates { x = chunk.X, y = chunk.Y };
            var finds = new List<Find>();
            LastEntities = 0;
            if (world.activeChunks.TryGetValue(coordinates, out var live) && live != null) ReadLive(live, coordinates, finds);
            else ReadFile(coordinates, finds);
            return finds;
        }

        // Where the last read came from ("live", "cache", or "file"), and the count of all its entities (0 for the cache).
        internal string LastSource { get; private set; }

        internal int LastEntities { get; private set; }

        private void ReadLive(Chunk chunk, Coordinates coordinates, List<Find> finds)
        {
            LastSource = "live";
            var map = chunk.identifierToEntitiesMap.GetEnumerator();
            while (map.MoveNext())
            {
                var pair = map.Current;
                LastEntities += pair.Value.Count;
                if (!PoiTable.TryGet(pair.Key, out var kind, out var metal)) continue;
                var entities = pair.Value.GetEnumerator();
                while (entities.MoveNext())
                {
                    var entity = entities.Current;
                    if (entity == null) continue;
                    Keep(entity.id);
                    var position = entity.Position;
                    finds.Add(Make(kind, metal, position.x, position.y, position.z, entity.level, coordinates));
                }
            }
        }

        private void ReadFile(Coordinates coordinates, List<Find> finds)
        {
            string key = null;
            FileStamp stamp = default;
            var stamped = Cache != null && !changed.Contains((coordinates.x, coordinates.y)) && TryStamp(coordinates, out key, out stamp);
            if (stamped && Cache.TryGet(key, stamp.Size, stamp.WriteTicks, out var cached))
            {
                CacheHits++;
                LastSource = "cache";
                finds.AddRange(cached);
                return;
            }
            FileReads++;
            LastSource = "file";
            if (!LoadFile(coordinates, finds)) return;
            if (stamped) Cache.Store(key, stamp.Size, stamp.WriteTicks, finds);
        }

        // The stamp of the record that LoadChunkEntities reads, in the order of the game: the entities record of the
        // slot manifest, else, with no manifest or an incomplete one, the legacy chunk file. The record id takes the
        // place of the size and the write time.
        private bool TryStamp(Coordinates coordinates, out string key, out FileStamp stamp)
        {
            ReadForm();
            key = null;
            stamp = default;
            if (noStamp) return false;
            if (manifest != null)
            {
                if (manifest.TryGetEntities(coordinates.x, coordinates.y, out var low, out var high))
                {
                    key = manifestKey + "#" + coordinates.x + "." + coordinates.y;
                    stamp = new FileStamp(low, high);
                    return true;
                }
                if (manifest.Complete)
                {
                    Plugin.Debug("Chunk stamp: no entities record of chunk " + Text(coordinates) + " in the complete manifest");
                    return false;
                }
            }
            return TryFile(coordinates, out key, out stamp);
        }

        private void ReadForm()
        {
            var slot = Slot();
            if (slot == formSlot) return;
            formSlot = slot;
            manifest = null;
            noStamp = false;
            var worldDir = persistence.getWorldDirectory(world.game, world.name);
            var path = Path.Combine(persistence.getSlotDirectory(world.game, world.name, slot), ManifestFile);
            manifestKey = Path.GetRelativePath(worldDir, path);
            var bytes = ReadBytes(path);
            if (bytes == null)
            {
                Plugin.Debug("Chunk form: legacy chunk files");
                return;
            }
            if (!PackedManifest.TryParse(bytes, out manifest, out var problem))
            {
                noStamp = true;
                Plugin.Debug("Chunk form: no cache, the manifest " + manifestKey + " does not parse (" + problem + ")");
                return;
            }
            Plugin.Debug("Chunk form: packed, " + manifestKey + " with " + manifest.EntityRecords + " entities records, "
                + (manifest.Complete ? "complete" : "incomplete"));
        }

        // The chunk file that LoadChunkEntities reads: the session file, else the slot file, else the old file name,
        // with its path relative to the world folder.
        private bool TryFile(Coordinates coordinates, out string key, out FileStamp stamp)
        {
            var worldDir = persistence.getWorldDirectory(world.game, world.name);
            var file = ChunkEntitiesSave.GetFilename(coordinates);
            var paths = new[]
            {
                Path.Combine(Path.Combine(worldDir, "session"), file),
                Path.Combine(persistence.getSlotDirectory(world.game, world.name, Slot()), file),
                Path.Combine(worldDir, ChunkEntitiesSave.GetFilenameDEPRECATED(Slot(), coordinates)),
            };
            foreach (var path in paths)
            {
                var found = Stamp(path);
                if (found == null) continue;
                key = Path.GetRelativePath(worldDir, path);
                stamp = found.Value;
                return true;
            }
            key = null;
            stamp = default;
            return false;
        }

        private bool LoadFile(Coordinates coordinates, List<Find> finds)
        {
            if (!persistence.LoadChunkEntities(world, Slot(), coordinates, out var save) || save?.serializedEntities == null)
            {
                Plugin.Debug("No chunk file for chunk " + Text(coordinates));
                return false;
            }
            var entities = save.serializedEntities;
            LastEntities = entities.Count;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                Keep(entity.id);
                if (!PoiTable.TryGet(entity.identifier, out var kind, out var metal)) continue;
                var position = entity.position;
                finds.Add(Make(kind, metal, position.x, position.y, position.z, entity.level, coordinates));
            }
            return true;
        }

        private Find Make(FindKind kind, Metal metal, float x, float y, float z, int level, Coordinates coordinates)
        {
            bool inCave = kind == FindKind.CaveEntrance ? level >= 0 : kind == FindKind.OreRock && level < 0;
            ChunkCoord origin = default;
            bool hasCave = inCave && CaveOriginOf(coordinates, out origin);
            return new Find(kind, metal, x, y, z, level, origin, hasCave);
        }

        private bool CaveOriginOf(Coordinates coordinates, out ChunkCoord origin)
        {
            origin = default;
            var info = world.GetChunkInfo(coordinates);
            if (info == null || !info.GetClosestFeature(ChunkInfoFeatureType.CAVE, coordinates, out var feature)) return false;
            origin = new ChunkCoord(feature.originCoordinates.x, feature.originCoordinates.y);
            return true;
        }

        private void Keep(int id)
        {
            if (id > HighestId) HighestId = id;
        }

        private static string Text(Coordinates coordinates) => "(" + coordinates.x + ", " + coordinates.y + ")";

        // The caves of the chunk info of the whole region.
        internal static List<CaveOrigin> CaveOrigins(World world)
        {
            var origins = new List<CaveOrigin>();
            var infos = world.chunkInfos.GetEnumerator();
            while (infos.MoveNext())
            {
                var info = infos.Current.Value;
                if (info == null || !info.containsCave) continue;
                var key = infos.Current.Key;
                origins.Add(new CaveOrigin(new ChunkCoord(key.x, key.y)));
            }
            return origins;
        }

        internal static List<ChunkCoord> GeneratedChunks(World world)
        {
            var chunks = new List<ChunkCoord>();
            var all = world.GetAllGeneratedChunkCoordinates().GetEnumerator();
            while (all.MoveNext()) chunks.Add(new ChunkCoord(all.Current.x, all.Current.y));
            return chunks;
        }

        internal static List<ChunkCoord> LoadedChunks(World world)
        {
            var chunks = new List<ChunkCoord>();
            var all = world.activeChunks.GetEnumerator();
            while (all.MoveNext()) chunks.Add(new ChunkCoord(all.Current.Key.x, all.Current.Key.y));
            return chunks;
        }
    }
}
