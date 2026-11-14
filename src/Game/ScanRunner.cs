using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;

namespace MapOverhaul.Game
{
    // Reads the queued chunks into the POI index, a few on each frame within a time budget, so a save load
    // does not stall. It holds the POI state of the world that the game runs.
    public sealed class ScanRunner : MonoBehaviour
    {
        internal const double BudgetMs = 2.0;

        internal static readonly PoiIndex Index = new PoiIndex();
        internal static readonly ScanQueue Queue = new ScanQueue();
        internal static readonly PoiBuilder Builder = new PoiBuilder(Index, Plugin.Debug);
        internal static ChunkReader Reader;

        // The time in milliseconds. A test sets it.
        internal static Func<double> Clock = () => Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency;

        private static IReadOnlyList<CaveOrigin> caveOrigins = new CaveOrigin[0];
        private static bool scanning;
        private static double scanStart;
        private static double longestFrame;
        private static int reads;
        private static int scanTotal;

        // The chunks read and the chunk count of the first scan after a save load, or (0, 0) when it does not run.
        // A map open can queue read chunks again, so the count comes from the chunks that still wait.
        internal static (int Read, int Total) Progress => scanning ? (Math.Max(0, scanTotal - Queue.Count), scanTotal) : (0, 0);
        private static int frames;
        private static int slowFrames;

        // The scan work of one frame above this time is over the frame budget of the scan.
        internal const double SlowFrameMs = 4.0;

        // A chunk read above this time writes a Verbose line, to find what makes the slow reads slow.
        internal const double SlowReadMs = 20.0;

        public ScanRunner(IntPtr pointer) : base(pointer) { }

        internal void Update() => Step();

        internal static void Begin(ChunkReader reader, IEnumerable<ChunkCoord> chunks)
        {
            Reader = reader;
            foreach (var chunk in chunks) Queue.Add(chunk);
            caveOrigins = ChunkReader.CaveOrigins(reader.World);
            Builder.SetCaveOrigins(caveOrigins);
            scanning = true;
            scanTotal = Queue.Count;
            scanStart = Clock();
            longestFrame = 0;
            reads = 0;
            frames = 0;
            slowFrames = 0;
            Plugin.Debug("Scan start: " + Queue.Count + " chunks");
        }

        internal static void Step(Func<bool> budgetUsed = null)
        {
            if (Reader == null || Queue.Count == 0) return;
            double start = Clock();
            budgetUsed ??= () => Clock() - start >= BudgetMs;
            reads += Queue.TakeWhile(budgetUsed, ReadOne);
            double frame = Clock() - start;
            longestFrame = Math.Max(longestFrame, frame);
            frames++;
            if (frame > SlowFrameMs) slowFrames++;
            if (scanning && Queue.Count == 0) End();
        }

        private static void ReadOne(ChunkCoord chunk)
        {
            try
            {
                double start = Clock();
                Index.Put(chunk, Reader.Read(chunk));
                double time = Clock() - start;
                if (time > SlowReadMs)
                    Plugin.Debug(string.Format(CultureInfo.InvariantCulture, "Slow read: chunk {0} {1:F1} ms, {2}, {3} entities",
                        chunk, time, Reader.LastSource, Reader.LastEntities));
            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Map Overhaul could not read chunk " + chunk, e);
            }
        }

        private static void End()
        {
            scanning = false;
            Plugin.Debug(string.Format(CultureInfo.InvariantCulture,
                "Scan end: {0:F0} ms, longest frame {1:F1} ms, {2} of {3} frames over 4 ms, {4} chunks, {5}, highest entity id {6},"
                + " cache hits {7}, file reads {8}",
                Clock() - scanStart, longestFrame, slowFrames, frames, reads, Counts(), Reader.HighestId, Reader.CacheHits,
                Reader.FileReads));
            LogCaveDistances();
            SaveCache();
        }

        // Writes the cache of the world when the reads changed it.
        internal static void SaveCache()
        {
            if (Reader?.Cache != null) CacheStore.Write(Reader.Cache, Reader.World);
        }

        // For each cave with a read entrance: how far it is from the center of its origin chunk.
        private static void LogCaveDistances()
        {
            foreach (var cave in CaveBuilder.Build(caveOrigins, Index.All(), _ => { }))
            {
                if (!cave.HasEntrance) continue;
                double distance = Math.Sqrt((cave.X - cave.CenterX) * (cave.X - cave.CenterX) + (cave.Z - cave.CenterZ) * (cave.Z - cave.CenterZ));
                Plugin.Debug(string.Format(CultureInfo.InvariantCulture,
                    "Cave {0}: entrance {1:F1} m from the chunk center", cave.Origin, distance));
            }
        }

        // The count of each find kind, split by the surface and the caves (a negative level).
        private static string Counts()
        {
            int cabins = 0;
            var surface = new int[5];
            var cave = new int[5];
            foreach (var find in Index.All())
            {
                if (find.Kind == FindKind.Cabin) { cabins++; continue; }
                int slot = find.Kind == FindKind.CaveEntrance ? 4 : (int)find.Metal;
                if (!find.InCave) surface[slot]++;
                else cave[slot]++;
            }
            var names = new[] { "tin", "copper", "iron", "silver" };
            var text = "cabins " + cabins + ", cave entrances " + surface[4] + " surface " + cave[4] + " cave";
            for (int i = 0; i < names.Length; i++) text += ", " + names[i] + " " + surface[i] + " surface " + cave[i] + " cave";
            return text;
        }

        internal static void Reset()
        {
            Index.Clear();
            Queue.Clear();
            caveOrigins = new CaveOrigin[0];
            Builder.SetCaveOrigins(caveOrigins);
            Reader = null;
            scanning = false;
        }
    }
}
