using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MapOverhaul
{
    // The result of a parse of the cache text.
    internal enum CacheParse
    {
        Ok,
        OtherVersion,
        Broken,
    }

    // The finds of each chunk file that the scan read, with the size and the write time of that file.
    // The key is the path of the file relative to the world folder, so a hit means the game would read the same file.
    // The text form is one header line (format, mod, and game version), then one line for each file:
    // path, size, write time, and the finds, with tabs between the fields and ';' between the finds.
    internal sealed class ChunkCache
    {
        internal const int FormatVersion = 1;

        private const string Magic = "MapOverhaulCache";
        private const int FindFields = 9;

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static readonly Find[] None = new Find[0];

        private readonly string modVersion;
        private readonly string gameVersion;
        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        internal ChunkCache(string modVersion, string gameVersion)
        {
            this.modVersion = modVersion;
            this.gameVersion = gameVersion;
        }

        internal int Count => entries.Count;

        // True after a store that changed an entry, until the next write.
        internal bool Changed { get; private set; }

        internal bool TryGet(string path, long size, long writeTicks, out IReadOnlyList<Find> finds)
        {
            if (entries.TryGetValue(path, out var entry) && entry.Size == size && entry.WriteTicks == writeTicks)
            {
                finds = entry.Finds;
                return true;
            }
            finds = None;
            return false;
        }

        internal void Store(string path, long size, long writeTicks, IReadOnlyCollection<Find> finds)
        {
            if (entries.TryGetValue(path, out var old) && old.Size == size && old.WriteTicks == writeTicks
                && old.Finds.SequenceEqual(finds))
                return;
            entries[path] = new Entry(size, writeTicks, finds.ToArray());
            Changed = true;
        }

        internal string Write()
        {
            var text = new StringBuilder();
            text.Append(Magic).Append('\t').Append(FormatVersion).Append('\t').Append(modVersion).Append('\t')
                .Append(gameVersion).Append('\n');
            foreach (var pair in entries)
            {
                var entry = pair.Value;
                text.Append(pair.Key).Append('\t').Append(entry.Size.ToString(Inv)).Append('\t')
                    .Append(entry.WriteTicks.ToString(Inv)).Append('\t');
                for (var i = 0; i < entry.Finds.Length; i++)
                {
                    if (i > 0) text.Append(';');
                    AppendFind(text, entry.Finds[i]);
                }
                text.Append('\n');
            }
            Changed = false;
            return text.ToString();
        }

        // Gives the cache of the text, or an empty cache when the text comes from other versions or does not parse.
        internal static ChunkCache Parse(string text, string modVersion, string gameVersion, out CacheParse result)
        {
            var empty = new ChunkCache(modVersion, gameVersion);
            var lines = text.Split('\n');
            var header = lines[0].TrimEnd('\r').Split('\t');
            if (header.Length != 4 || header[0] != Magic)
            {
                result = CacheParse.Broken;
                return empty;
            }
            if (header[1] != FormatVersion.ToString(Inv) || header[2] != modVersion || header[3] != gameVersion)
            {
                result = CacheParse.OtherVersion;
                return empty;
            }

            var cache = new ChunkCache(modVersion, gameVersion);
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r');
                if (line.Length == 0) continue;
                if (!TryParseLine(line, out var path, out var entry))
                {
                    result = CacheParse.Broken;
                    return empty;
                }
                cache.entries[path] = entry;
            }
            result = CacheParse.Ok;
            return cache;
        }

        private static void AppendFind(StringBuilder text, Find find)
        {
            text.Append(find.Kind).Append(',').Append(find.Metal).Append(',')
                .Append(find.X.ToString("R", Inv)).Append(',').Append(find.Y.ToString("R", Inv)).Append(',')
                .Append(find.Z.ToString("R", Inv)).Append(',').Append(find.Level.ToString(Inv)).Append(',')
                .Append(find.HasCave ? '1' : '0').Append(',')
                .Append(find.CaveOrigin.X.ToString(Inv)).Append(',').Append(find.CaveOrigin.Y.ToString(Inv));
        }

        private static bool TryParseLine(string line, out string path, out Entry entry)
        {
            path = null;
            entry = null;
            var fields = line.Split('\t');
            if (fields.Length != 4) return false;
            if (!long.TryParse(fields[1], NumberStyles.Integer, Inv, out var size)) return false;
            if (!long.TryParse(fields[2], NumberStyles.Integer, Inv, out var ticks)) return false;

            var finds = new List<Find>();
            if (fields[3].Length > 0)
            {
                foreach (var part in fields[3].Split(';'))
                {
                    if (!TryParseFind(part, out var find)) return false;
                    finds.Add(find);
                }
            }
            path = fields[0];
            entry = new Entry(size, ticks, finds.ToArray());
            return true;
        }

        private static bool TryParseFind(string text, out Find find)
        {
            find = default;
            var f = text.Split(',');
            if (f.Length != FindFields) return false;
            if (!TryEnum(f[0], out FindKind kind) || !TryEnum(f[1], out Metal metal)) return false;
            if (!TryFloat(f[2], out var x) || !TryFloat(f[3], out var y) || !TryFloat(f[4], out var z)) return false;
            if (!TryInt(f[5], out var level) || !TryInt(f[7], out var caveX) || !TryInt(f[8], out var caveY)) return false;
            if (f[6] != "0" && f[6] != "1") return false;
            find = new Find(kind, metal, x, y, z, level, new ChunkCoord(caveX, caveY), f[6] == "1");
            return true;
        }

        private static bool TryEnum<T>(string text, out T value) where T : struct =>
            Enum.TryParse(text, false, out value) && Enum.IsDefined(typeof(T), value) && !char.IsDigit(text[0]);

        private static bool TryFloat(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, Inv, out value);

        private static bool TryInt(string text, out int value) =>
            int.TryParse(text, NumberStyles.Integer, Inv, out value);

        private sealed class Entry
        {
            internal readonly long Size;
            internal readonly long WriteTicks;
            internal readonly Find[] Finds;

            internal Entry(long size, long writeTicks, Find[] finds)
            {
                Size = size;
                WriteTicks = writeTicks;
                Finds = finds;
            }
        }
    }
}
