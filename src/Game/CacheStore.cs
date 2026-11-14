using System;
using System.IO;
using System.Text;
using BepInEx;
using UnityEngine;

namespace MapOverhaul.Game
{
    // Keeps the chunk cache of each world in its own file in the cache folder of BepInEx, outside the save folder.
    internal static class CacheStore
    {
        // The folder of the cache files. A test sets it.
        internal static string Folder = DefaultFolder();

        private static bool warnedBroken;

        internal static string DefaultFolder() => Path.Combine(Paths.CachePath, "MapOverhaul");

        // The game and world names, with each character other than a letter, a digit, '-', and '_' replaced by '_'.
        internal static string FileName(World world) => Clean(world.game.name) + "_" + Clean(world.name) + ".txt";

        // Gives the cache of the world, or an empty cache when the file is missing, of other versions, or broken.
        internal static ChunkCache Read(World world)
        {
            var name = FileName(world);
            var empty = new ChunkCache(Plugin.PluginVersion, Application.version);
            try
            {
                var path = Path.Combine(Folder, name);
                if (!File.Exists(path))
                {
                    Plugin.Debug("Cache: no file " + name);
                    return empty;
                }
                var cache = ChunkCache.Parse(File.ReadAllText(path), Plugin.PluginVersion, Application.version, out var result);
                if (result == CacheParse.OtherVersion) Plugin.Debug("Cache: " + name + " is of another version");
                else if (result == CacheParse.Broken && !warnedBroken)
                {
                    warnedBroken = true;
                    Plugin.Log.LogWarning("Map Overhaul cache file " + name + " is broken: the scan reads each chunk file again");
                }
                else if (result == CacheParse.Ok) Plugin.Debug("Cache: read " + cache.Count + " chunk files from " + name);
                return cache;
            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Map Overhaul cache read", e);
                return empty;
            }
        }

        // Writes the cache of the world when it changed since the last read or write.
        internal static void Write(ChunkCache cache, World world)
        {
            if (!cache.Changed) return;
            try
            {
                Directory.CreateDirectory(Folder);
                var name = FileName(world);
                File.WriteAllText(Path.Combine(Folder, name), cache.Write());
                Plugin.Debug("Cache: wrote " + cache.Count + " chunk files to " + name);
            }
            catch (Exception e)
            {
                Plugin.WarnOnce("Map Overhaul cache write", e);
            }
        }

        internal static void Reset() => warnedBroken = false;

        private static string Clean(string name)
        {
            var text = new StringBuilder(name.Length);
            foreach (var c in name)
                text.Append(c < 128 && (char.IsLetterOrDigit(c) || c == '-' || c == '_') ? c : '_');
            return text.ToString();
        }
    }
}
