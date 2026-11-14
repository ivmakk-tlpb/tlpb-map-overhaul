using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapOverhaul.Game
{
    // The kept map objects of the POIs. They are built again only when the POI list changes, so a frame of the open
    // map only adds the objects of the kinds that are on.
    internal static class MapObjects
    {
        private const float IconSize = 28f;
        private const float DotSize = 10f;

        internal static readonly PoiIds Ids = new PoiIds();

        private static readonly List<(PoiKind Kind, MapTileObject Object)> kept = new List<(PoiKind, MapTileObject)>();
        private static int builtVersion = -1;
        private static IntPtr builtWorld;
        private static float builtAt;

        // A build at most every quarter second: while the first read of a save runs with the map open, the POIs
        // change on each frame, and a build on each frame is wasted work.
        internal const float BuildIntervalSeconds = 0.25f;

        internal static void Refresh(World world)
        {
            bool sameWorld = world.Pointer == builtWorld;
            float now = Time.realtimeSinceStartup;
            if (sameWorld && now - builtAt < BuildIntervalSeconds) return;
            var pois = ScanRunner.Builder.Get();
            if (ScanRunner.Builder.Version == builtVersion && sameWorld) return;
            Build(world, pois);
            builtVersion = ScanRunner.Builder.Version;
            builtWorld = world.Pointer;
            builtAt = now;
        }

        // Adds the objects of the kinds that are on, and gives their count.
        internal static int AppendTo(Il2CppSystem.Collections.Generic.List<MapTileObject> objects, Settings settings)
        {
            int added = 0;
            foreach (var (kind, tileObject) in kept)
            {
                if (!settings.IsOn(kind)) continue;
                objects.Add(tileObject);
                added++;
            }
            return added;
        }

        internal static void Clear()
        {
            kept.Clear();
            Ids.Clear();
            builtVersion = -1;
            builtWorld = IntPtr.Zero;
            builtAt = 0f;
        }

        private static void Build(World world, IReadOnlyList<Poi> pois)
        {
            var names = ItemNames.Ores();
            var texts = new string[pois.Count];
            for (int i = 0; i < pois.Count; i++) texts[i] = HoverText.Of(pois[i], names);
            var ids = Ids.Assign(pois, texts);
            var min = world.RegionCoordinatesMin;
            var max = world.RegionCoordinatesMax;
            var position = new MapPosition(min.x, min.y, max.x, max.y, world.chunkSize);
            kept.Clear();
            for (int i = 0; i < pois.Count; i++) kept.Add((pois[i].Kind, Make(pois[i], ids[i], texts[i], position)));
        }

        private static MapTileObject Make(Poi poi, int id, string text, MapPosition position)
        {
            var (x, y) = position.Relative(poi.X, poi.Z);
            var chunk = position.ChunkOf(poi.X, poi.Z);
            bool icon = poi.Kind == PoiKind.Cabin || poi.Kind == PoiKind.Cave;
            return new MapTileObject
            {
                entityId = id,
                chunkCoordinates = new Coordinates { x = chunk.X, y = chunk.Y },
                relativePosition = new Vector2(x, y),
                minimapIconType = MinimapIconType.MARKER,
                mapIconSize = icon ? IconSize : DotSize,
                dynamicSizingFactor = 0f,
                rotation = Quaternion.identity,
                entityLabel = text,
            };
        }
    }
}
