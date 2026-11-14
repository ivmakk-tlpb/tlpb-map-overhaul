using MapOverhaul;
using Xunit;

// The patch classes and the fake game types hold static state, so their tests do not run in parallel.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GameState
{
    public const string Name = "Game state";
}

// Each test starts from a clean state with a loaded plugin.
[Collection(GameState.Name)]
public abstract class GameTestBase : IDisposable
{
    protected readonly Plugin Loaded;

    protected GameTestBase()
    {
        FakeState.Reset();
        Loaded = new Plugin();
        Loaded.Load();
    }

    // Removes the cache folder of the test.
    public void Dispose()
    {
        var folder = MapOverhaul.Game.CacheStore.Folder;
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        else if (File.Exists(folder)) File.Delete(folder);
    }

    protected bool ConfigValue(string key) => ((BepInEx.Configuration.ConfigEntry<bool>)Loaded.Config.Entries[key]).Value;
}
