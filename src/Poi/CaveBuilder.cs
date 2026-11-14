using System;
using System.Collections.Generic;

namespace MapOverhaul
{
    // A cave of the chunk info: its origin chunk.
    internal readonly struct CaveOrigin
    {
        internal readonly ChunkCoord Chunk;

        internal CaveOrigin(ChunkCoord chunk)
        {
            Chunk = chunk;
        }
    }

    // One cave: at its entrance when the entrance chunk was read, otherwise at the center of its origin chunk,
    // where the game places the surface entrance. Ores holds the count of the ore rocks inside the cave for each metal.
    internal sealed class Cave
    {
        internal readonly ChunkCoord Origin;
        internal float X;
        internal float Z;
        internal bool HasEntrance;
        internal readonly int[] Ores = new int[4];

        // The center of the origin chunk.
        internal readonly float CenterX;
        internal readonly float CenterZ;

        internal Cave(ChunkCoord origin, float x, float z)
        {
            Origin = origin;
            X = CenterX = x;
            Z = CenterZ = z;
        }
    }

    // Makes the caves of the region from the chunk info caves, and links the cave entrances and the cave ore
    // rocks to the cave of their origin chunk.
    internal static class CaveBuilder
    {
        internal const float ChunkSize = 32f;

        internal static List<Cave> Build(IReadOnlyList<CaveOrigin> origins, IEnumerable<Find> finds, Action<string> log)
        {
            var caves = new List<Cave>(origins.Count);
            var byOrigin = new Dictionary<ChunkCoord, Cave>();
            foreach (var origin in origins)
            {
                var cave = new Cave(origin.Chunk, (origin.Chunk.X + 0.5f) * ChunkSize, (origin.Chunk.Y + 0.5f) * ChunkSize);
                caves.Add(cave);
                byOrigin[origin.Chunk] = cave;
            }
            var unknown = new HashSet<ChunkCoord>();
            var atSurface = new HashSet<Cave>();
            foreach (var find in finds)
            {
                if (!find.HasCave || find.Kind == FindKind.Cabin) continue;
                if (find.Kind == FindKind.OreRock && !find.InCave) continue;
                if (!byOrigin.TryGetValue(find.CaveOrigin, out var cave))
                {
                    if (unknown.Add(find.CaveOrigin)) log("No chunk info cave at " + find.CaveOrigin);
                    continue;
                }
                if (find.Kind == FindKind.OreRock) cave.Ores[(int)find.Metal]++;
                else PlaceEntrance(cave, find, atSurface);
            }
            return caves;
        }

        // The first surface entrance wins. An entrance of another level places the cave only while no surface entrance did.
        private static void PlaceEntrance(Cave cave, Find entrance, HashSet<Cave> atSurface)
        {
            if (atSurface.Contains(cave)) return;
            if (!entrance.InCave) atSurface.Add(cave);
            else if (cave.HasEntrance) return;
            cave.X = entrance.X;
            cave.Z = entrance.Z;
            cave.HasEntrance = true;
        }
    }
}
