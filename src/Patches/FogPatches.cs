using System;
using HarmonyLib;
using MapOverhaul.Game;
using UnityEngine;

namespace MapOverhaul.Patches
{
    // Builds the fog of war at each map open, and keeps the fog image over the map tiles on each frame of the open map.
    internal static class FogPatches
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
                if (openRule.IsOpen(Time.frameCount)) FogLayer.Build(world, __result);
            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Map Overhaul fog build", e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapUI), nameof(MapUI.Update))]
        internal static void UpdatePostfix(MapUI __instance)
        {
            try
            {
                if (Session.Multiplayer()) return;
                FogLayer.Place(__instance);
            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Map Overhaul fog layer", e);
            }
        }
    }
}
