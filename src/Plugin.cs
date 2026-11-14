using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using MapOverhaul.Game;

namespace MapOverhaul
{
    [BepInPlugin(PluginGuid, "Map Overhaul", PluginVersion)]
    [BepInProcess("TheLastPlague.exe")]
    public sealed class Plugin : BasePlugin
    {
        public const string PluginGuid = "com.ivmakk.thelastplagueblight.mapoverhaul";
        public const string PluginVersion = "1.0.0";

        internal static new ManualLogSource Log;
        internal static ConfigEntry<bool> Verbose;
        internal static Settings Settings;

        // The config key and the description of each row of the map legend, in the order of Settings.
        private static readonly (string Key, string Description)[] Rows =
        {
            ("ShowCabins", "Shows the cabin icons on the Map. The Cabins row of the legend."),
            ("ShowCaves", "Shows the cave icons on the Map. The Caves row of the legend."),
            ("ShowTin", "Shows the dots of the Cassiterite deposits. The Tin row of the legend."),
            ("ShowCopper", "Shows the dots of the Copper-rich Rock deposits. The Copper row of the legend."),
            ("ShowIron", "Shows the dots of the Iron-rich Rock deposits. The Iron row of the legend."),
            ("ShowSilver", "Shows the dots of the Silver-rich Rock deposits. The Silver row of the legend."),
            ("ShowFogOfWar", "Darkens the land that the game has not generated yet. The Fog of war row of the legend."),
        };

        // A patch group is applied alone with its own Harmony id: a missing target removes only that group.
        internal sealed class PatchGroup
        {
            public readonly string Name;
            public readonly string OffText;
            public readonly List<Type> Types;

            // Runs before the patches of the group, such as the register of an injected type.
            public Action<Plugin> Setup;

            public PatchGroup(string name, string offText, params Type[] types)
            {
                Name = name;
                OffText = offText;
                Types = new List<Type>(types);
            }
        }

        internal static readonly List<PatchGroup> Groups = new List<PatchGroup>
        {
            new PatchGroup("poi", "Map Overhaul shows no POIs on the region map", typeof(Patches.ScanPatches), typeof(Patches.MapPoiPatches)) { Setup = AddScanRunner },
            new PatchGroup("fog", "Map Overhaul shows no fog of war on the region map", typeof(Patches.FogPatches)),
            new PatchGroup("legend", "Map Overhaul shows no map legend on the region map", typeof(Patches.LegendPatches)),
        };

        private static readonly HashSet<string> warned = new HashSet<string>();

        // The names of the patch groups that are in place.
        internal static readonly HashSet<string> Applied = new HashSet<string>();

        public override void Load()
        {
            Log = base.Log;
            warned.Clear();
            Applied.Clear();
            BindSettings();
            Verbose = Config.Bind(
                "General", "Verbose", false,
                "Writes extra diagnostic lines to the BepInEx log for troubleshooting. Keep off in normal play.");
            foreach (var group in Groups) Apply(group, this);
            Log.LogInfo("Map Overhaul loaded.");
        }

        private static void AddScanRunner(Plugin plugin)
        {
            ClassInjector.RegisterTypeInIl2Cpp<ScanRunner>();
            plugin.AddComponent<ScanRunner>();
        }
        private void BindSettings()
        {
            var entries = new ConfigEntry<bool>[Rows.Length];
            var shown = new bool[Rows.Length];
            for (int i = 0; i < Rows.Length; i++)
            {
                entries[i] = Config.Bind("Map", Rows[i].Key, true, Rows[i].Description);
                shown[i] = entries[i].Value;
            }
            Settings = new Settings(shown, (row, value) => entries[row].Value = value);
        }

        // A game update can rename a target. Then the patches of that group go, and the other groups stay.
        private static void Apply(PatchGroup group, Plugin plugin)
        {
            var harmony = new Harmony(PluginGuid + "." + group.Name);
            try
            {
                group.Setup?.Invoke(plugin);
                foreach (var patches in group.Types) harmony.PatchAll(patches);
                Applied.Add(group.Name);
                Debug("Patch group " + group.Name + " applied");
            }
            catch (Exception e)
            {
                harmony.UnpatchSelf();
                Log.LogWarning(group.OffText + ": " + e.Message);
            }
        }

        // One warning for each distinct error, so a repeated error does not flood the log.
        internal static void WarnOnce(string where, Exception e)
        {
            string text = where + ": " + e.GetType().Name + ": " + e.Message;
            if (warned.Add(text)) Log.LogWarning(text + "\n" + e);
        }

        internal static void Debug(string line)
        {
            if (Verbose.Value) Log.LogDebug(line);
        }
    }
}
