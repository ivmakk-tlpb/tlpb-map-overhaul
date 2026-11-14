using System;
using HarmonyLib;
using MapOverhaul.Game;

namespace MapOverhaul.Patches
{
    // Builds the map legend when the map window is created, and keeps it up to date on each frame of the open map.
    internal static class LegendPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapUI), nameof(MapUI.Created))]
        internal static void CreatedPostfix(MapUI __instance)
        {
            try
            {
                LegendView.Build(__instance);
            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Map Overhaul legend build", e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapUI), nameof(MapUI.Update))]
        internal static void UpdatePostfix(MapUI __instance)
        {
            try
            {
                LegendView.Refresh(Session.Multiplayer());
            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Map Overhaul legend", e);
            }
        }
    }
}
