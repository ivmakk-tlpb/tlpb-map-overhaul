using System;
using System.Collections.Generic;

namespace MapOverhaul
{
    // The record manifest of a save slot in the packed save layout of the beta new-save-system
    // (<slot>/chunks.manifest): for each chunk and record type, the 32-byte id of its record in the packs of the
    // world. A changed record gets a new id, so the id stamps the chunk for the cache. The format is the one of the
    // game's PackedRecordManifest version 2: a header of magic, version, flags, and entry count (4 bytes each),
    // entries of x, y (4 bytes each), type (1 byte), and id (32 bytes), and a 4-byte checksum.
    internal sealed class PackedManifest
    {
        private const int Magic = 843927618;
        private const int Version = 2;
        private const int CompleteSnapshotFlag = 1;
        private const int HeaderLength = 16;
        private const int EntryLength = 41;
        private const int ChecksumLength = 4;
        private const byte ChunkEntitiesType = 3;

        private readonly Dictionary<(int X, int Y), (long Low, long High)> entities;

        private PackedManifest(bool complete, Dictionary<(int X, int Y), (long Low, long High)> entities)
        {
            Complete = complete;
            this.entities = entities;
        }

        // True when the manifest lists each record of the slot, so the game reads no legacy chunk file of it.
        internal bool Complete { get; }

        internal int EntityRecords => entities.Count;

        // Gives the manifest of the file bytes, or false with the problem when they are not of the known format.
        internal static bool TryParse(byte[] bytes, out PackedManifest manifest, out string problem)
        {
            manifest = null;
            if (bytes == null || bytes.Length < HeaderLength + ChecksumLength)
            {
                problem = "length " + (bytes?.Length ?? 0) + " is shorter than the header";
                return false;
            }
            if (BitConverter.ToInt32(bytes, 0) != Magic)
            {
                problem = "another magic";
                return false;
            }
            var version = BitConverter.ToInt32(bytes, 4);
            if (version != Version)
            {
                problem = "version " + version;
                return false;
            }
            var flags = BitConverter.ToInt32(bytes, 8);
            var count = BitConverter.ToInt32(bytes, 12);
            if (count < 0 || bytes.Length != HeaderLength + (long)count * EntryLength + ChecksumLength)
            {
                problem = "length " + bytes.Length + " does not fit " + count + " entries";
                return false;
            }

            var entities = new Dictionary<(int X, int Y), (long Low, long High)>();
            for (var i = 0; i < count; i++)
            {
                var at = HeaderLength + i * EntryLength;
                if (bytes[at + 8] != ChunkEntitiesType) continue;
                var chunk = (BitConverter.ToInt32(bytes, at), BitConverter.ToInt32(bytes, at + 4));
                entities[chunk] = (BitConverter.ToInt64(bytes, at + 9), BitConverter.ToInt64(bytes, at + 17));
            }
            manifest = new PackedManifest((flags & CompleteSnapshotFlag) != 0, entities);
            problem = null;
            return true;
        }

        // The first 16 bytes of the id of the entities record of the chunk.
        internal bool TryGetEntities(int x, int y, out long low, out long high)
        {
            var found = entities.TryGetValue((x, y), out var id);
            low = id.Low;
            high = id.High;
            return found;
        }
    }
}
