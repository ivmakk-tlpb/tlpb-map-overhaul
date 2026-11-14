using HarmonyLib;

// Puts the static state of the fake game types back to a known start.
internal static class FakeState
{
    public static void Reset()
    {
        UnityEngine.Object.Scene.Clear();
        UnityEngine.Object.DestroyedObjects.Clear();
        UnityEngine.Time.frameCount = 0;
        UnityEngine.Time.realtimeSinceStartup = 0f;
        BepInEx.Unity.IL2CPP.BasePlugin.Added.Clear();
        Il2CppInterop.Runtime.Injection.ClassInjector.Registered.Clear();
        Il2CppInterop.Runtime.Injection.ClassInjector.ThrowFor = null;
        MapOverhaul.Game.ScanRunner.Reset();
        MapOverhaul.Game.Session.Reset();
        MapOverhaul.Game.MapObjects.Clear();
        MapOverhaul.Game.IconSprites.Reset();
        MapOverhaul.Patches.MapPoiPatches.Reset();
        MapOverhaul.Game.FogLayer.Reset();
        MapOverhaul.Patches.FogPatches.Reset();
        MapOverhaul.Game.LegendView.Reset();
        L.Texts.Clear();
        MapOverhaul.Game.ScanRunner.Clock = () => 0;
        MapOverhaul.Game.ChunkReader.Stamp = MapOverhaul.Game.ChunkReader.DiskStamp;
        MapOverhaul.Game.ChunkReader.ReadBytes = MapOverhaul.Game.ChunkReader.DiskBytes;
        MapOverhaul.Game.CacheStore.Folder = Path.Combine(Path.GetTempPath(), "MapOverhaulTests", Guid.NewGuid().ToString("N"));
        UnityEngine.Application.version = "0.12.7";
        MapOverhaul.Game.CacheStore.Reset();
        Harmony.Patched.Clear();
        Harmony.Unpatched.Clear();
        Harmony.ThrowFor = null;
    }
}
