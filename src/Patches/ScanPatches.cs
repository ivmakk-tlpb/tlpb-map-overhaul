using System;
using HarmonyLib;
using MapOverhaul.Game;

namespace MapOverhaul.Patches
{
    // Starts the scan of the chunk files when a world becomes active, queues each chunk that the game generates
    // or unloads, and clears the POI state when the world unloads.
    internal static class ScanPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(World), nameof(World.SetWorldState))]
        internal static void SetWorldStatePostfix(World __instance, WorldState state)
        {
            try
            {
                if (state != WorldState.ACTIVE) return;
                if (Session.Multiplayer()) return;
                if (ScanRunner.Reader != null && ScanRunner.Reader.World.Pointer == __instance.Pointer)
                {
                    Plugin.Debug("SetWorldState: the scan of this world ran");
                    return;
                }
                var manager = Session.Manager();
                var persistence = manager?.persistenceManager?.TryCast<PersistenceManager>();
                if (persistence == null)
                {
                    Plugin.Debug("SetWorldState: no game manager");
                    return;
                }
                ScanRunner.SaveCache();
                ScanRunner.Reset();
                var reader = new ChunkReader(__instance, persistence, manager.currentGameSlotName, CacheStore.Read(__instance))
                {
                    CurrentSlot = () => manager.currentGameSlotName,
                };
                ScanRunner.Begin(reader, ChunkReader.GeneratedChunks(__instance));
            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Map Overhaul scan start", e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(World), nameof(World.AddGeneratedChunk))]
        internal static void AddGeneratedChunkPostfix(World __instance, Chunk chunk)
        {
            try
            {
                if (ScanRunner.Reader == null || ScanRunner.Reader.World.Pointer != __instance.Pointer || Session.Multiplayer()) return;
                Queue(chunk, "generated");
            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Map Overhaul generated chunk", e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.unloadChunk))]
        internal static void UnloadChunkPostfix(Chunk chunk)
        {
            try
            {
                if (ScanRunner.Reader == null || Session.Multiplayer()) return;
                Queue(chunk, "unloaded");
            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Map Overhaul unloaded chunk", e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.unloadWorld))]
        internal static void UnloadWorldPostfix(World world)
        {
            try
            {
                Session.Clear();
                MapObjects.Clear();
                IconSprites.Forget();
                MapPoiPatches.Reset();
                FogLayer.Reset();
                FogPatches.Reset();
                LegendView.Expand();
                if (ScanRunner.Reader == null) return;
                ScanRunner.SaveCache();
                ScanRunner.Reset();
                Plugin.Debug("World unload: POI state cleared");
            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Map Overhaul world unload", e);
            }
        }

        private static void Queue(Chunk chunk, string why)
        {
            var coordinates = chunk.coordinates;
            var coord = new ChunkCoord(coordinates.x, coordinates.y);
            ScanRunner.Reader?.MarkChanged(coord);
            ScanRunner.Queue.Add(coord);
            Plugin.Debug("Queue add (" + why + "): (" + coordinates.x + ", " + coordinates.y + ")");
        }
    }
}
