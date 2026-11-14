using MapOverhaul;
using MapOverhaul.Game;
using MapOverhaul.Patches;
using UnityEngine;
using UnityEngine.UI;
using Xunit;

public class MapPoiPatchesTests : GameTestBase
{
    private readonly GameWorld game;
    private readonly RegionMapData map = new RegionMapData();
    private readonly ChunkInfo info;

    public MapPoiPatchesTests()
    {
        L.Texts.Clear();
        game = new GameWorld().InScene();
        Plugin.Verbose.Value = true;
        info = game.Info(0, 0, cave: true);
        game.File(0, 0,
            GameWorld.Saved(1, "cabin1", 40f, 70f),
            GameWorld.Saved(2, "iron_rock_resource_1", 100f, 100f),
            GameWorld.Saved(3, "iron_rock_resource_1", 110f, 100f));
        ScanPatches.SetWorldStatePostfix(game.World, WorldState.ACTIVE);
        ScanRunner.Step(() => false);
    }

    private void Frame(int frame)
    {
        UnityEngine.Time.frameCount = frame;
        UnityEngine.Time.realtimeSinceStartup = frame;
        map.mapTileObjects = new Il2CppSystem.Collections.Generic.List<MapTileObject>();
        MapPoiPatches.GetRegionMapPostfix(game.World, map);
    }

    private List<MapTileObject> Added()
    {
        var list = new List<MapTileObject>();
        for (int i = 0; i < map.mapTileObjects.Count; i++) list.Add(map.mapTileObjects[i]);
        return list;
    }

    private MapTileObject Of(string label) => Added().Single(o => o.entityLabel == label);

    [Fact]
    public void Each_poi_is_appended_with_its_hover_text_and_a_marker_icon()
    {
        Frame(10);

        Assert.Equal(new[] { "Cabin", "Cave", "Iron deposit (2)" }, Added().Select(o => o.entityLabel).OrderBy(t => t));
        Assert.All(Added(), o => Assert.Equal(MinimapIconType.MARKER, o.minimapIconType));
        Assert.All(Added(), o => Assert.Equal(1f, o.rotation.w));
        Assert.Contains("Map open: 3 POIs added", Plugin.Log.Debugs);
    }

    [Fact]
    public void The_ids_are_positive_from_the_top_of_the_int_range()
    {
        Frame(10);

        Assert.All(Added(), o => Assert.True(o.entityId > 1_000_000_000));
        Assert.Equal(3, Added().Select(o => o.entityId).Distinct().Count());
    }

    [Fact]
    public void The_position_is_the_region_fraction_and_the_chunk_of_the_world_position()
    {
        Frame(10);

        var cabin = Of("Cabin");
        // The fake region spans the chunks -32 to 31: the world from -1024 to 1024.
        Assert.Equal(40f / 2048f, cabin.relativePosition.x, 5);
        Assert.Equal(70f / 2048f, cabin.relativePosition.y, 5);
        Assert.Equal((1, 2), (cabin.chunkCoordinates.x, cabin.chunkCoordinates.y));
    }

    [Fact]
    public void Icons_and_dots_have_their_sizes()
    {
        Frame(10);

        Assert.Equal(28f, Of("Cabin").mapIconSize);
        Assert.Equal(10f, Of("Iron deposit (2)").mapIconSize);
        Assert.All(Added(), o => Assert.Equal(0f, o.dynamicSizingFactor));
    }

    [Fact]
    public void A_frame_with_no_poi_change_adds_the_same_objects()
    {
        Frame(10);
        var first = Added();

        Frame(11);

        Assert.Equal(first, Added());
    }

    [Fact]
    public void A_poi_keeps_its_id_while_its_text_stays_and_gets_a_new_one_when_it_changes()
    {
        Frame(10);
        int cabin = Of("Cabin").entityId;
        int deposit = Of("Iron deposit (2)").entityId;
        game.File(0, 0, GameWorld.Saved(1, "cabin1", 40f, 70f), GameWorld.Saved(2, "iron_rock_resource_1", 100f, 100f));
        ScanRunner.Queue.Add(new ChunkCoord(0, 0));
        ScanRunner.Step(() => false);

        Frame(11);

        Assert.Equal(cabin, Of("Cabin").entityId);
        Assert.NotEqual(deposit, Of("Iron deposit (1)").entityId);
    }

    [Fact]
    public void A_kind_turned_off_is_left_out_on_the_next_call()
    {
        Frame(10);

        Plugin.Settings.Set(Settings.Row(PoiKind.Iron), false);
        Frame(11);

        Assert.DoesNotContain(Added(), o => o.entityLabel.StartsWith("Iron"));
        Assert.Equal(2, Added().Count);
    }

    [Fact]
    public void A_map_open_moves_the_loaded_chunks_to_the_front_of_the_queue()
    {
        game.Loaded(5, 5);
        ScanRunner.Queue.Add(new ChunkCoord(9, 9));

        Frame(10);

        var read = new List<ChunkCoord>();
        ScanRunner.Queue.TakeWhile(() => false, read.Add);
        Assert.Equal(new ChunkCoord(5, 5), read[0]);
    }

    [Fact]
    public void A_call_on_the_next_frame_is_no_map_open()
    {
        Frame(10);
        game.Loaded(5, 5);

        Frame(11);

        Assert.Equal(0, ScanRunner.Queue.Count);
        Assert.Single(Plugin.Log.Debugs, line => line.StartsWith("Map open"));
    }

    [Fact]
    public void The_ore_names_of_the_game_show_in_the_cave_hover_text()
    {
        L.Texts["item.iron_ore.name"] = "Eisenerz";
        info.ClosestCave = GameWorld.At(0, 0);
        game.File(0, 0, GameWorld.Saved(4, "iron_rock_resource_2", 1f, 1f, level: -1));
        ScanRunner.Queue.Add(new ChunkCoord(0, 0));
        ScanRunner.Step(() => false);

        Frame(10);

        Assert.Contains(Added(), o => o.entityLabel == "Cave\nEisenerz (1)");
    }

    [Fact]
    public void A_network_game_adds_nothing()
    {
        game.Network.Networked = true;

        Frame(10);

        Assert.Empty(Added());
    }

    [Fact]
    public void An_exception_is_caught_and_logged_once()
    {
        map.mapTileObjects = null;

        MapPoiPatches.GetRegionMapPostfix(game.World, map);
        MapPoiPatches.GetRegionMapPostfix(game.World, map);

        Assert.Single(Plugin.Log.Warnings);
    }

    [Fact]
    public void A_world_unload_clears_the_kept_objects()
    {
        Frame(10);

        ScanPatches.UnloadWorldPostfix(game.World);
        Frame(20);

        Assert.Empty(Added());
    }

    // The icons.

    private static GameObject IconObject(Sprite sprite = null)
    {
        var image = new Image { sprite = sprite ?? new Sprite(), color = new Color(0.5f, 0.5f, 0.5f, 1f) };
        var icon = new GameObject();
        icon.Components.Add(image);
        return icon;
    }

    private static Image ImageOf(GameObject icon) => icon.GetComponent<Image>();

    [Fact]
    public void A_new_mod_icon_gets_the_sprite_of_its_kind_in_white()
    {
        Frame(10);
        var cabin = IconObject();
        var dot = IconObject();

        MapPoiPatches.EntityIconPostfix(Of("Cabin"), cabin);
        MapPoiPatches.EntityIconPostfix(Of("Iron deposit (2)"), dot);

        Assert.Same(IconSprites.Get(PoiKind.Cabin), ImageOf(cabin).sprite);
        Assert.Same(IconSprites.Get(PoiKind.Iron), ImageOf(dot).sprite);
        Assert.Equal(Color.white, ImageOf(cabin).color);
        Assert.Contains(Plugin.Log.Debugs, line => line.StartsWith("New mod icon: Cabin"));
    }

    [Fact]
    public void A_mod_icon_is_styled_once()
    {
        Frame(10);
        var cabin = IconObject();
        MapPoiPatches.EntityIconPostfix(Of("Cabin"), cabin);
        ImageOf(cabin).sprite = null;

        MapPoiPatches.EntityIconPostfix(Of("Cabin"), cabin);

        Assert.Null(ImageOf(cabin).sprite);
    }

    [Fact]
    public void A_mod_icon_on_an_object_that_a_game_icon_used_in_between_is_styled_again()
    {
        Frame(10);
        var pooled = IconObject(new Sprite());
        MapPoiPatches.EntityIconPostfix(Of("Cabin"), pooled);
        MapPoiPatches.CoordinatesIconPostfix(pooled);

        MapPoiPatches.EntityIconPostfix(Of("Cabin"), pooled);

        Assert.Same(IconSprites.Get(PoiKind.Cabin), ImageOf(pooled).sprite);
    }

    [Fact]
    public void A_mod_icon_on_an_object_that_another_poi_used_in_between_is_styled_again()
    {
        Frame(10);
        var pooled = IconObject(new Sprite());
        MapPoiPatches.EntityIconPostfix(Of("Cabin"), pooled);
        MapPoiPatches.EntityIconPostfix(Of("Iron deposit (2)"), pooled);

        MapPoiPatches.EntityIconPostfix(Of("Cabin"), pooled);

        Assert.Same(IconSprites.Get(PoiKind.Cabin), ImageOf(pooled).sprite);
    }

    [Fact]
    public void A_game_icon_on_a_pooled_mod_object_gets_the_look_of_the_pool_back()
    {
        Frame(10);
        var original = new Sprite();
        var pooled = IconObject(original);
        MapPoiPatches.EntityIconPostfix(Of("Cabin"), pooled);

        MapPoiPatches.CoordinatesIconPostfix(pooled);

        Assert.Same(original, ImageOf(pooled).sprite);
        Assert.Equal(0.5f, ImageOf(pooled).color.r);
    }

    [Fact]
    public void Each_game_getter_puts_the_look_back()
    {
        Frame(10);
        var original = new Sprite();
        foreach (var getter in new Action<GameObject>[]
        {
            o => MapPoiPatches.PlayerMarkerIconPostfix(o),
            o => MapPoiPatches.AutoMarkerIconPostfix(o),
            o => MapPoiPatches.CoordinatesIconPostfix(o),
            o => MapPoiPatches.EntityIconPostfix(new MapTileObject { entityId = 77 }, o),
        })
        {
            var pooled = IconObject(original);
            MapPoiPatches.EntityIconPostfix(Of("Cabin"), pooled);
            MapPoiPatches.EntityIconPostfix(Of("Cave"), IconObject());

            getter(pooled);

            Assert.Same(original, ImageOf(pooled).sprite);
            MapObjects.Clear();
            Frame(100);
        }
    }

    [Fact]
    public void A_game_icon_that_was_never_a_mod_icon_stays_as_it_is()
    {
        Frame(10);
        var sprite = new Sprite();
        var icon = IconObject(sprite);
        ImageOf(icon).color = Color.white;

        MapPoiPatches.PlayerMarkerIconPostfix(icon);
        MapPoiPatches.EntityIconPostfix(new MapTileObject { entityId = 5 }, icon);

        Assert.Same(sprite, ImageOf(icon).sprite);
        Assert.Equal(Color.white, ImageOf(icon).color);
    }

    [Fact]
    public void A_null_icon_is_left_alone_and_an_icon_exception_is_logged_once()
    {
        Frame(10);

        MapPoiPatches.EntityIconPostfix(Of("Cabin"), null);
        MapPoiPatches.EntityIconPostfix(Of("Cabin"), new GameObject());
        MapPoiPatches.EntityIconPostfix(Of("Cabin"), new GameObject());

        Assert.Single(Plugin.Log.Warnings);
    }

    [Fact]
    public void The_sprites_are_made_once_and_again_after_unity_drops_them()
    {
        var cabin = IconSprites.Get(PoiKind.Cabin);

        Assert.Same(cabin, IconSprites.Get(PoiKind.Cabin));
        Assert.Equal(64, cabin.texture.width);
        Assert.True(cabin.texture.Applied);
        Assert.Equal(HideFlags.HideAndDontSave, cabin.hideFlags);
        Assert.Equal(32, IconSprites.Get(PoiKind.Silver).texture.width);

        IconSprites.Reset();
        Assert.NotSame(cabin, IconSprites.Get(PoiKind.Cabin));
    }

    [Fact]
    public void The_icon_mask_lies_in_ink_color_with_the_texture_rows_from_the_bottom()
    {
        var texture = IconSprites.Get(PoiKind.Cave).texture;

        // The fake decode has one pixel at x 32 and texture row 32: the mask row 31 from the top.
        var pixel = texture.GetPixel(32, 32);
        Assert.Equal(1f, pixel.a, 3);
        Assert.Equal(52f / 255, pixel.r, 3);
    }
}
