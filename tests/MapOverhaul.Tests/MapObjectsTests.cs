using MapOverhaul;
using MapOverhaul.Game;
using UnityEngine;
using Xunit;

public class MapObjectsTests : GameTestBase
{
    private readonly GameWorld game = new GameWorld();

    private static Find Cabin(float x) => new Find(FindKind.Cabin, Metal.Tin, x, 0f, 5f, 1, default, false);

    private int Shown()
    {
        var objects = new Il2CppSystem.Collections.Generic.List<MapTileObject>();
        return MapObjects.AppendTo(objects, Plugin.Settings);
    }

    [Fact]
    public void A_change_of_the_pois_within_a_quarter_second_of_the_last_build_waits_for_a_later_frame()
    {
        Time.realtimeSinceStartup = 10f;
        ScanRunner.Index.Put(new ChunkCoord(0, 0), new[] { Cabin(5f) });
        MapObjects.Refresh(game.World);
        Assert.Equal(1, Shown());

        ScanRunner.Index.Put(new ChunkCoord(1, 0), new[] { Cabin(40f) });
        Time.realtimeSinceStartup = 10.1f;
        MapObjects.Refresh(game.World);
        Assert.Equal(1, Shown());

        Time.realtimeSinceStartup = 10.3f;
        MapObjects.Refresh(game.World);
        Assert.Equal(2, Shown());
    }

    [Fact]
    public void Another_world_builds_at_once()
    {
        Time.realtimeSinceStartup = 10f;
        ScanRunner.Index.Put(new ChunkCoord(0, 0), new[] { Cabin(5f) });
        MapObjects.Refresh(game.World);
        ScanRunner.Index.Put(new ChunkCoord(1, 0), new[] { Cabin(40f) });

        MapObjects.Refresh(new GameWorld().World);

        Assert.Equal(2, Shown());
    }
}
