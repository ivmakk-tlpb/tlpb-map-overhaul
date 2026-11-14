// Fake game types: only the types and members that the mod source reaches.
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

public struct Coordinates
{
    public int x;
    public int y;

    public override string ToString() => "(" + x + ", " + y + ")";
}

public enum ChunkInfoFeatureType
{
    CAVE = 0,
}

public struct ChunkInfoFeature
{
    public Coordinates originCoordinates;
    public float radius;
}

public class ChunkInfo : Il2CppObjectBase
{
    public bool containsCave;

    // The cave origin that GetClosestFeature gives. A test sets it.
    public Coordinates? ClosestCave;

    public bool GetClosestFeature(ChunkInfoFeatureType type, Coordinates fromCoordinates, out ChunkInfoFeature resultFeature)
    {
        resultFeature = new ChunkInfoFeature { originCoordinates = ClosestCave ?? default };
        return type == ChunkInfoFeatureType.CAVE && ClosestCave.HasValue;
    }
}

public class Entity : Il2CppObjectBase
{
    public int id;
    public string identifier;
    public int level;
    public Vector3 Position;
}

public class Chunk : Il2CppObjectBase
{
    public Coordinates coordinates;
    public Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.HashSet<Entity>> identifierToEntitiesMap =
        new Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.HashSet<Entity>>();
}

public partial class World : Il2CppObjectBase
{
    public int chunkSize = 32;
    public Il2CppSystem.Collections.Generic.Dictionary<Coordinates, ChunkInfo> chunkInfos = new Il2CppSystem.Collections.Generic.Dictionary<Coordinates, ChunkInfo>();
    public Il2CppSystem.Collections.Generic.Dictionary<Coordinates, Chunk> activeChunks = new Il2CppSystem.Collections.Generic.Dictionary<Coordinates, Chunk>();
    public Il2CppSystem.Collections.Generic.HashSet<Coordinates> Generated = new Il2CppSystem.Collections.Generic.HashSet<Coordinates>();

    public ChunkInfo GetChunkInfo(Coordinates coordinates) => chunkInfos.TryGetValue(coordinates, out var info) ? info : null;

    public Il2CppSystem.Collections.Generic.HashSet<Coordinates> GetAllGeneratedChunkCoordinates() => Generated;
}

public enum MinimapIconType
{
    PLAYER = 0,
    MARKER = 5,
}

public class MapTileObject : Il2CppObjectBase
{
    public int entityId;
    public Coordinates chunkCoordinates;
    public Vector2 relativePosition;
    public MinimapIconType minimapIconType;
    public float mapIconSize;
    public float dynamicSizingFactor;
    public Quaternion rotation;
    public string entityLabel;
}

public class RegionMapData : Il2CppObjectBase
{
    public Il2CppSystem.Collections.Generic.List<MapTileObject> mapTileObjects = new Il2CppSystem.Collections.Generic.List<MapTileObject>();
    public Coordinates minCoordinates = new Coordinates { x = -32, y = -32 };
    public Coordinates maxCoordinates = new Coordinates { x = 31, y = 31 };
    public float tileSizeRatio = 1f;
}

public class LocalPlayer : Il2CppObjectBase { }

public class MapTileUI : MonoBehaviour { }

// An item label of the world. The mod reads the font and the material of its text.
public class HudObjectLabel : MonoBehaviour
{
    public TMPro.TextMeshProUGUI text;
}

public class MappingManager : Il2CppObjectBase
{
    public RegionMapData GetRegionMap(World world, LocalPlayer localPlayer) => null;
}

public class MapUI : MonoBehaviour
{
    public Transform tilesArea;
    public GameObject body;
    public TMPro.TMP_FontAsset mapFont;
    public TMPro.TMP_FontAsset handwrittenFont;

    public void Created() { }
    public float tileSize = 256f;
    public Il2CppSystem.Collections.Generic.List<MapTileUI> activeTileUIs = new Il2CppSystem.Collections.Generic.List<MapTileUI>();

    public void Update() { }

    public GameObject getEntityIconObject(MapTileObject mapTileObject) => null;
    public GameObject getPlayerMarkerIconObject(MapTileObject mapTileObject) => null;
    public GameObject getAutoMapMarketIconObject(MapTileObject mapTileObject) => null;
    public GameObject getCoordinatesIconObject(MapTileObject mapTileObject) => null;
}

// The localization of the game: a test sets the text of a key. A key with no text gives the key.
public static class L
{
    public static readonly Dictionary<string, string> Texts = new Dictionary<string, string>();

    public static string T(string key) => Texts.TryGetValue(key, out var text) ? text : key;
}

public enum WorldState
{
    UNLOADED = 0,
    LOADING = 1,
    LOADED = 2,
    ACTIVE = 3,
}

public partial class World
{
    public Coordinates RegionCoordinatesMin = new Coordinates { x = -32, y = -32 };
    public Coordinates RegionCoordinatesMax = new Coordinates { x = 31, y = 31 };

    public void SetWorldState(WorldState state) { }

    public void AddGeneratedChunk(Chunk chunk) { }
}

public class IWorldManager : Il2CppObjectBase { }

public class WorldManager : IWorldManager
{
    public void unloadChunk(Chunk chunk, bool destroyEntities) { }

    public void unloadWorld(World world) { }
}

public class INetworkManager : Il2CppObjectBase { }

public class NetworkManager : INetworkManager
{
    public bool Networked;

    public bool IsNetworkedGame() => Networked;
}

public class IGameManager : Il2CppObjectBase { }

public class GameManager : IGameManager
{
    public INetworkManager networkManager;
    public IPersistenceManager persistenceManager;
    public string currentGameSlotName;
}

public class GameLoop : MonoBehaviour
{
    public IGameManager gameManager;
}

public class SerializedEntity : Il2CppObjectBase
{
    public int id;
    public string identifier;
    public Vector3 position;
    public int level;
}

public class ChunkEntitiesSave : Il2CppObjectBase
{
    public Il2CppSystem.Collections.Generic.List<SerializedEntity> serializedEntities = new Il2CppSystem.Collections.Generic.List<SerializedEntity>();

    public static string GetFilename(Coordinates coordinates) => "chunk_entities_" + coordinates.x + "_" + coordinates.y + ".dat";

    public static string GetFilenameDEPRECATED(string slotName, Coordinates coordinates) =>
        slotName + "_chunk_entities_" + coordinates.x + "_" + coordinates.y + ".dat";
}

public class Game : Il2CppObjectBase
{
    public string name;
}

public partial class World
{
    public string name = "45635";
    public Game game = new Game { name = "GD_01" };
}

public class IPersistenceManager : Il2CppObjectBase { }

public class PersistenceManager : IPersistenceManager
{
    public const string Root = @"C:\saves";

    // The chunk files of the slot. A test adds them.
    public readonly Dictionary<(string Slot, int X, int Y), ChunkEntitiesSave> Files = new Dictionary<(string, int, int), ChunkEntitiesSave>();

    public int Loads;

    // Runs in each load, such as a clock step that makes the read slow. A test sets it.
    public System.Action LoadHook;

    public string getWorldDirectory(Game game, string worldName) => Root + @"\" + game.name + @"\" + worldName;

    public string getSlotDirectory(Game game, string worldName, string slotName) => getWorldDirectory(game, worldName) + @"\" + slotName;

    public bool LoadChunkEntities(World world, string slotName, Coordinates coordinates, out ChunkEntitiesSave chunkEntitiesSave)
    {
        Loads++;
        LoadHook?.Invoke();
        return Files.TryGetValue((slotName, coordinates.x, coordinates.y), out chunkEntitiesSave);
    }
}
