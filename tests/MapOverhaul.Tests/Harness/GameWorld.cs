using UnityEngine;

// A world with chunk info, loaded chunks, and chunk files of one slot.
internal sealed class GameWorld
{
    public const string Slot = "slot36";

    public readonly World World = new World();
    public readonly PersistenceManager Persistence = new PersistenceManager();
    public readonly NetworkManager Network = new NetworkManager();

    // Puts the game manager of this world into the scene, as a loaded game has it.
    public GameWorld InScene()
    {
        var manager = new GameManager { networkManager = Network, persistenceManager = Persistence, currentGameSlotName = Slot };
        UnityEngine.Object.Scene.Add(new GameLoop { gameManager = manager });
        return this;
    }

    public static Coordinates At(int x, int y) => new Coordinates { x = x, y = y };

    // A chunk info for the chunk, with a cave origin there when cave is set, and the nearest cave origin.
    public ChunkInfo Info(int x, int y, bool cave = false, Coordinates? closestCave = null)
    {
        var info = new ChunkInfo { containsCave = cave, ClosestCave = closestCave };
        World.chunkInfos.Add(At(x, y), info);
        return info;
    }

    public Chunk Loaded(int x, int y, params Entity[] entities)
    {
        var chunk = new Chunk { coordinates = At(x, y) };
        foreach (var entity in entities)
        {
            if (!chunk.identifierToEntitiesMap.TryGetValue(entity.identifier, out var set))
            {
                set = new Il2CppSystem.Collections.Generic.HashSet<Entity>();
                chunk.identifierToEntitiesMap.Add(entity.identifier, set);
            }
            set.Add(entity);
        }
        World.activeChunks.Add(At(x, y), chunk);
        World.Generated.Add(At(x, y));
        return chunk;
    }

    public ChunkEntitiesSave File(int x, int y, params SerializedEntity[] entities)
    {
        var save = new ChunkEntitiesSave();
        foreach (var entity in entities) save.serializedEntities.Add(entity);
        Persistence.Files[(Slot, x, y)] = save;
        World.Generated.Add(At(x, y));
        return save;
    }

    public static Entity Live(int id, string identifier, float x, float z, int level = 1) =>
        new Entity { id = id, identifier = identifier, level = level, Position = new Vector3(x, 5f, z) };

    public static SerializedEntity Saved(int id, string identifier, float x, float z, int level = 1) =>
        new SerializedEntity { id = id, identifier = identifier, level = level, position = new Vector3(x, 5f, z) };
}
