using MapOverhaul;
using MapOverhaul.Game;
using MapOverhaul.Patches;
using UnityEngine;
using UnityEngine.UI;
using Xunit;

public class FogPatchesTests : GameTestBase
{
    private readonly GameWorld game;
    private readonly MapUI ui;
    private readonly RegionMapData map;

    public FogPatchesTests()
    {
        game = new GameWorld().InScene();
        Plugin.Verbose.Value = true;
        ui = new MapUI { tilesArea = new GameObject("Tiles").AddComponent<RectTransform>(), tileSize = 256f };
        map = new RegionMapData { minCoordinates = GameWorld.At(0, 0), maxCoordinates = GameWorld.At(1, 1) };
    }

    private void Generated(int x, int y) => game.World.Generated.Add(GameWorld.At(x, y));

    private MapTileUI Tile(float x, float y)
    {
        var tile = new GameObject("Tile").AddComponent<MapTileUI>();
        tile.transform.SetParent(ui.tilesArea, false);
        tile.transform.localPosition = new Vector3(x, y, 0f);
        ui.activeTileUIs.Add(tile);
        return tile;
    }

    private void Open(int frame = 1)
    {
        Time.frameCount = frame;
        FogPatches.GetRegionMapPostfix(game.World, map);
    }

    private RawImage Fog() => ui.tilesArea.Children.Select(c => c.gameObject.GetComponent<RawImage>()).SingleOrDefault(i => i != null);

    private string[] Builds() => Plugin.Log.Debugs.Where(l => l.StartsWith("Fog build")).ToArray();

    [Fact]
    public void A_map_open_builds_the_fog_from_the_generated_chunks_of_the_world()
    {
        Generated(1, 0);
        Generated(5, 5);

        Open();

        Assert.Equal(0f, FogLayer.Model.AlphaAt(1, 0));
        Assert.Equal(0.7f, FogLayer.Model.AlphaAt(0, 0));
        Assert.Equal(0.7f, FogLayer.Model.AlphaAt(1, 1));
        Assert.Single(Builds());
        Assert.StartsWith("Fog build: 2 x 2, 1 chunks changed, ", Builds()[0]);
        Assert.EndsWith(" ms", Builds()[0]);
    }

    [Fact]
    public void The_fog_image_shows_the_texture_in_black_with_the_fog_alpha()
    {
        Generated(1, 0);
        Open();

        FogPatches.UpdatePostfix(ui);

        var texture = Fog().texture;
        Assert.Equal((2, 2), (texture.width, texture.height));
        Assert.Equal(FilterMode.Bilinear, texture.filterMode);
        Assert.Equal(TextureWrapMode.Clamp, texture.wrapMode);
        Assert.True(texture.Applied);
        Assert.Equal(new byte[] { 0, 0, 0, 179 }, texture.RawData.Skip(0).Take(4));
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, texture.RawData.Skip(4).Take(4));
    }

    [Fact]
    public void A_frame_of_an_open_map_builds_nothing()
    {
        Open(1);
        Open(2);

        Assert.Single(Builds());
    }

    [Fact]
    public void A_new_map_open_with_no_new_generated_chunk_uploads_nothing()
    {
        Generated(1, 0);
        Open(1);
        FogPatches.UpdatePostfix(ui);

        Open(10);

        Assert.StartsWith("Fog build: 2 x 2, 0 chunks changed, ", Builds()[1]);
        Assert.Equal(1, Fog().texture.Uploads);
    }

    [Fact]
    public void A_new_map_open_clears_the_newly_generated_chunks()
    {
        Open(1);
        FogPatches.UpdatePostfix(ui);
        Generated(0, 1);

        Open(10);

        Assert.Equal(0f, FogLayer.Model.AlphaAt(0, 1));
        Assert.Equal(2, Fog().texture.Uploads);
    }

    [Fact]
    public void The_fog_image_is_under_the_tiles_area_and_takes_no_pointer()
    {
        Tile(0f, 0f);
        Open();

        FogPatches.UpdatePostfix(ui);

        var fog = Fog();
        Assert.NotNull(fog);
        Assert.False(fog.raycastTarget);
        Assert.Same(ui.tilesArea, fog.transform.parent);
    }

    [Fact]
    public void Each_frame_puts_the_fog_image_above_the_tiles_also_after_a_new_tile()
    {
        Tile(0f, 0f);
        Open();
        FogPatches.UpdatePostfix(ui);

        Tile(256f, 0f);
        FogPatches.UpdatePostfix(ui);

        Assert.Equal(ui.tilesArea.Children.Count - 1, Fog().transform.GetSiblingIndex());
        Assert.Single(ui.tilesArea.Children, c => c.gameObject.GetComponent<RawImage>() != null);
    }

    [Fact]
    public void The_fog_image_covers_the_corners_of_the_tiles()
    {
        Tile(-128f, -128f);
        Tile(128f, -128f);
        Tile(-128f, 128f);
        Tile(128f, 128f);
        Open();

        FogPatches.UpdatePostfix(ui);

        var rect = Fog().rectTransform;
        Assert.Equal((512f, 512f), (rect.sizeDelta.x, rect.sizeDelta.y));
        Assert.Equal((0f, 0f), (rect.localPosition.x, rect.localPosition.y));
    }

    [Fact]
    public void The_fog_image_covers_only_the_region_part_of_the_tiles_from_their_south_west_corner()
    {
        map.tileSizeRatio = 0.75f;
        Tile(-128f, -128f);
        Tile(128f, -128f);
        Tile(-128f, 128f);
        Tile(128f, 128f);
        Open();

        FogPatches.UpdatePostfix(ui);

        var rect = Fog().rectTransform;
        Assert.Equal((384f, 384f), (rect.sizeDelta.x, rect.sizeDelta.y));
        Assert.Equal((-64f, -64f), (rect.localPosition.x, rect.localPosition.y));
    }

    [Fact]
    public void The_fog_image_follows_the_tiles_when_they_move()
    {
        Tile(0f, 0f);
        Tile(256f, 0f);
        Open();
        FogPatches.UpdatePostfix(ui);
        ui.tileSize = 512f;
        ui.activeTileUIs[1].transform.localPosition = new Vector3(512f, 0f, 0f);

        FogPatches.UpdatePostfix(ui);

        var rect = Fog().rectTransform;
        Assert.Equal((1024f, 512f), (rect.sizeDelta.x, rect.sizeDelta.y));
        Assert.Equal((256f, 0f), (rect.localPosition.x, rect.localPosition.y));
    }

    [Fact]
    public void The_fog_image_hides_when_the_fog_of_war_setting_is_off()
    {
        Tile(0f, 0f);
        Open();
        Plugin.Settings.Set(Settings.FogRow, false);

        FogPatches.UpdatePostfix(ui);
        Assert.False(Fog().gameObject.activeSelf);

        Plugin.Settings.Set(Settings.FogRow, true);
        FogPatches.UpdatePostfix(ui);
        Assert.True(Fog().gameObject.activeSelf);
    }

    [Fact]
    public void A_frame_before_the_first_map_open_adds_no_fog_image()
    {
        Tile(0f, 0f);

        FogPatches.UpdatePostfix(ui);

        Assert.Null(Fog());
    }

    [Fact]
    public void A_network_game_does_nothing()
    {
        Tile(0f, 0f);
        game.Network.Networked = true;

        Open();
        FogPatches.UpdatePostfix(ui);

        Assert.Empty(Builds());
        Assert.Null(Fog());
    }

    [Fact]
    public void An_exception_of_a_build_is_caught_and_logged_once()
    {
        game.World.Generated = null;

        Open(1);
        Open(10);

        Assert.Single(Plugin.Log.Warnings);
    }

    [Fact]
    public void An_exception_of_a_frame_is_caught_and_logged_once()
    {
        Open();
        ui.activeTileUIs = null;

        FogPatches.UpdatePostfix(ui);
        FogPatches.UpdatePostfix(ui);

        Assert.Single(Plugin.Log.Warnings);
    }

    [Fact]
    public void A_world_unload_forgets_the_fog_and_frees_its_texture()
    {
        Tile(0f, 0f);
        Open();
        FogPatches.UpdatePostfix(ui);
        var texture = Fog().texture;

        FogLayer.Reset();

        Assert.Null(FogLayer.Model);
        Assert.Contains(texture, UnityEngine.Object.DestroyedObjects);
        Open(10);
        Assert.Equal(2, Builds().Length);
    }
}
