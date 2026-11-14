using System;
using HarmonyLib;
using MapOverhaul.Game;
using UnityEngine;

namespace MapOverhaul.Patches
{
    // Adds the POIs to the region map on each frame while the map is open, and keeps the look of the icon objects:
    // a POI icon gets its own sprite, and a game icon on an object that carried a POI sprite gets the pool look back.
    internal static class MapPoiPatches
    {
        private static readonly MapOpenRule openRule = new MapOpenRule();

        internal static void Reset() => openRule.Reset();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MappingManager), nameof(MappingManager.GetRegionMap))]
        internal static void GetRegionMapPostfix(World world, RegionMapData __result)
        {
            try
            {
                if (__result == null || world == null || Session.Multiplayer()) return;
                bool open = openRule.IsOpen(Time.frameCount);
                if (open && ScanRunner.Reader != null) ScanRunner.Queue.Front(ChunkReader.LoadedChunks(world));
                MapObjects.Refresh(world);
                int added = MapObjects.AppendTo(__result.mapTileObjects, Plugin.Settings);
                if (open) Plugin.Debug("Map open: " + added + " POIs added");            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Map Overhaul region map", e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapUI), nameof(MapUI.getEntityIconObject))]
        internal static void EntityIconPostfix(MapTileObject mapTileObject, GameObject __result)
        {
            try
            {
                if (__result == null || mapTileObject == null) return;
                int id = mapTileObject.entityId;
                if (MapObjects.Ids.TryKindOf(id, out var kind)) IconSprites.StyleMod(id, kind, __result);
                else IconSprites.RestoreGame(__result);
            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Map Overhaul entity icon", e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapUI), nameof(MapUI.getPlayerMarkerIconObject))]
        internal static void PlayerMarkerIconPostfix(GameObject __result) => Restore(__result);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapUI), nameof(MapUI.getAutoMapMarketIconObject))]
        internal static void AutoMarkerIconPostfix(GameObject __result) => Restore(__result);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapUI), nameof(MapUI.getCoordinatesIconObject))]
        internal static void CoordinatesIconPostfix(GameObject __result) => Restore(__result);

        private static void Restore(GameObject icon)
        {
            try
            {
                if (icon != null) IconSprites.RestoreGame(icon);
            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Map Overhaul game icon", e);
            }
        }
    }
}
