using MapOverhaul;
using MapOverhaul.Game;
using MapOverhaul.Patches;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Xunit;

public class LegendPatchesTests : GameTestBase
{
    private readonly GameWorld game;
    private readonly MapUI ui;

    public LegendPatchesTests()
    {
        game = new GameWorld().InScene();
        Plugin.Verbose.Value = true;
        var body = new GameObject("Body");
        body.AddComponent<RectTransform>();
        ui = new MapUI { body = body, mapFont = new TMP_FontAsset { name = "UncialAntiqua-Regular SDF" }, handwrittenFont = new TMP_FontAsset { name = "EagleLake-Regular SDF" } };
        label = new HudObjectLabel { text = new TextMeshProUGUI { font = new TMP_FontAsset { name = "Newsreader SDF Outline" }, fontSharedMaterial = new Material() } };
        UnityEngine.Object.Scene.Add(label);
    }

    private readonly HudObjectLabel label;

    private Transform Root() => ui.body.transform.Children.SingleOrDefault(c => c.gameObject.name == "MapOverhaulLegend");

    private static Transform Child(Transform parent, string name) => parent.Children.Single(c => c.gameObject.name == name);

    private Transform Expanded() => Child(Root(), "Expanded");

    private Transform Collapsed() => Child(Root(), "Collapsed");

    private Transform Row(string name) => Child(Expanded(), "Row " + name);

    private static bool Checked(Transform row) => Child(Child(row, "Box"), "Check").gameObject.activeSelf;

    private TextMeshProUGUI Text(Transform parent, string name) => Child(parent, name).gameObject.GetComponent<TextMeshProUGUI>();

    private TextMeshProUGUI Progress() => Text(Expanded(), "Progress");

    private static void Press(Transform button) => button.gameObject.GetComponent<Button>().onClick.Invoke();

    [Fact]
    public void Created_builds_the_legend_on_the_map_body_in_the_top_right_corner()
    {
        LegendPatches.CreatedPostfix(ui);

        var rect = (RectTransform)Root();
        Assert.Equal((1f, 1f), (rect.anchorMin.x, rect.anchorMin.y));
        Assert.Equal((1f, 1f), (rect.anchorMax.x, rect.anchorMax.y));
        Assert.Equal((1f, 1f), (rect.pivot.x, rect.pivot.y));
        Assert.Equal((-8f, -8f), (rect.anchoredPosition.x, rect.anchoredPosition.y));
    }

    [Fact]
    public void The_title_is_in_the_handwritten_map_font_and_the_rows_in_the_item_label_font()
    {
        LegendPatches.CreatedPostfix(ui);

        var title = Text(Child(Expanded(), "Header"), "Title");
        Assert.Equal("Legend", title.text);
        Assert.Same(ui.handwrittenFont, title.font);
        var rows = Expanded().Children.Where(c => c.gameObject.name.StartsWith("Row ")).ToList();
        Assert.Equal(new[] { "Cabins", "Caves", "Tin", "Copper", "Iron", "Silver", "Fog of war" },
            rows.Select(r => Text(r, "Name").text));
        Assert.All(rows, r => Assert.Same(label.text.font, Text(r, "Name").font));
        Assert.All(rows, r => Assert.Same(label.text.fontSharedMaterial, Text(r, "Name").fontSharedMaterial));
        Assert.Same(label.text.font, Progress().font);
        Assert.Same(label.text.fontSharedMaterial, Progress().fontSharedMaterial);
        Assert.Contains("Legend built, fonts EagleLake-Regular SDF and Newsreader SDF Outline", Plugin.Log.Debugs);
    }

    [Fact]
    public void With_no_item_label_the_rows_use_the_title_font()
    {
        UnityEngine.Object.Scene.Remove(label);

        LegendPatches.CreatedPostfix(ui);

        Assert.Same(ui.handwrittenFont, Text(Row("Iron"), "Name").font);
        Assert.Contains("Legend built, fonts EagleLake-Regular SDF and EagleLake-Regular SDF", Plugin.Log.Debugs);
    }

    [Fact]
    public void The_legend_text_uses_a_sharp_copy_of_the_font_material_and_leaves_the_game_material_as_it_is()
    {
        var game = new Material();
        game.SetFloat("_OutlineSoftness", 0.632f);
        game.SetFloat("_OutlineWidth", 0.34f);
        game.SetFloat("_FaceDilate", 0.6f);
        game.SetFloat("_GradientScale", 10f);
        ui.handwrittenFont.material = game;

        LegendPatches.CreatedPostfix(ui);

        var title = Text(Child(Expanded(), "Header"), "Title");
        var material = title.fontSharedMaterial;
        Assert.NotSame(game, material);
        Assert.Equal((0f, 0.15f, 0.15f, 10f),
            (material.GetFloat("_OutlineSoftness"), material.GetFloat("_OutlineWidth"), material.GetFloat("_FaceDilate"), material.GetFloat("_GradientScale")));
        Assert.Same(material, Text(Child(Expanded(), "Header"), "Title").fontSharedMaterial);
        Assert.Equal(0.632f, game.GetFloat("_OutlineSoftness"));
    }

    [Fact]
    public void With_no_handwritten_font_the_title_uses_the_map_font()
    {
        ui.handwrittenFont = null;
        UnityEngine.Object.Scene.Remove(label);

        LegendPatches.CreatedPostfix(ui);

        Assert.Same(ui.mapFont, Text(Child(Expanded(), "Header"), "Title").font);
        Assert.Contains("Legend built, fonts UncialAntiqua-Regular SDF and UncialAntiqua-Regular SDF", Plugin.Log.Debugs);
    }

    [Fact]
    public void The_panel_and_the_button_take_the_pointer_events_and_let_the_map_show_through()
    {
        LegendPatches.CreatedPostfix(ui);

        foreach (var part in new[] { Expanded(), Collapsed() })
        {
            Assert.NotNull(part.gameObject.GetComponent<EventTrigger>());
            var background = part.gameObject.GetComponent<Image>();
            Assert.True(background.raycastTarget);
            Assert.InRange(background.color.a, 0.4f, 0.65f);
        }
    }

    [Fact]
    public void A_poi_row_shows_the_sprite_of_its_kind()
    {
        LegendPatches.CreatedPostfix(ui);

        Assert.Same(IconSprites.Get(PoiKind.Cave), Child(Row("Caves"), "Icon").gameObject.GetComponent<Image>().sprite);
        Assert.Same(IconSprites.Get(PoiKind.Iron), Child(Row("Iron"), "Icon").gameObject.GetComponent<Image>().sprite);
    }

    [Fact]
    public void Each_row_shows_a_light_highlight_under_the_pointer()
    {
        LegendPatches.CreatedPostfix(ui);

        var button = Row("Iron").gameObject.GetComponent<Button>();
        Assert.Equal(Selectable.Transition.ColorTint, button.transition);
        Assert.Same(Row("Iron").gameObject.GetComponent<Image>(), button.targetGraphic);
        Assert.Equal(0f, button.colors.normalColor.a);
        Assert.InRange(button.colors.highlightedColor.a, 0.05f, 0.3f);
    }

    [Fact]
    public void The_toggles_show_the_settings()
    {
        Plugin.Settings.Set(Settings.Row(PoiKind.Iron), false);

        LegendPatches.CreatedPostfix(ui);

        Assert.False(Checked(Row("Iron")));
        Assert.True(Checked(Row("Cabins")));
    }

    [Fact]
    public void A_click_flips_the_setting_writes_the_config_and_one_verbose_line()
    {
        LegendPatches.CreatedPostfix(ui);

        Press(Row("Iron"));
        LegendPatches.UpdatePostfix(ui);

        Assert.False(Plugin.Settings.IsOn(PoiKind.Iron));
        Assert.False(ConfigValue("Map/ShowIron"));
        Assert.False(Checked(Row("Iron")));
        Assert.Contains("Legend click: Iron off", Plugin.Log.Debugs);
    }

    [Fact]
    public void After_a_click_the_poi_postfix_leaves_out_that_kind_on_the_next_frame()
    {
        ScanRunner.Index.Put(new ChunkCoord(0, 0), new[] { new Find(FindKind.OreRock, Metal.Iron, 5f, 0f, 5f, 1, default, false) });
        LegendPatches.CreatedPostfix(ui);
        var before = new RegionMapData();
        Time.frameCount = 1;
        MapPoiPatches.GetRegionMapPostfix(game.World, before);
        Assert.Equal(1, before.mapTileObjects.Count);

        Press(Row("Iron"));
        var after = new RegionMapData();
        Time.frameCount = 2;
        MapPoiPatches.GetRegionMapPostfix(game.World, after);

        Assert.Equal(0, after.mapTileObjects.Count);
    }

    [Fact]
    public void The_legend_starts_expanded()
    {
        LegendPatches.CreatedPostfix(ui);

        Assert.True(Expanded().gameObject.activeSelf);
        Assert.False(Collapsed().gameObject.activeSelf);
    }

    [Fact]
    public void A_click_on_the_title_collapses_the_legend_to_one_small_button_and_the_button_expands_it()
    {
        LegendPatches.CreatedPostfix(ui);

        Press(Child(Expanded(), "Header"));

        Assert.False(Expanded().gameObject.activeSelf);
        Assert.True(Collapsed().gameObject.activeSelf);
        var size = ((RectTransform)Collapsed()).sizeDelta;
        Assert.True(size.x <= 32f && size.y <= 32f, size.x + " x " + size.y);
        Assert.DoesNotContain(Collapsed().Children, c => c.gameObject.GetComponent<TextMeshProUGUI>() != null);
        Assert.Contains("Legend collapsed", Plugin.Log.Debugs);

        Press(Child(Collapsed(), "Hit"));

        Assert.True(Expanded().gameObject.activeSelf);
        Assert.False(Collapsed().gameObject.activeSelf);
    }

    [Fact]
    public void A_world_unload_expands_the_legend_and_the_config_keeps_no_collapse_state()
    {
        LegendPatches.CreatedPostfix(ui);
        Press(Child(Expanded(), "Header"));

        ScanPatches.UnloadWorldPostfix(game.World);

        Assert.True(Expanded().gameObject.activeSelf);
        Assert.False(Collapsed().gameObject.activeSelf);
        Assert.DoesNotContain(Loaded.Config.Entries.Keys, k => k.Contains("Collapse") || k.Contains("Legend"));
    }

    [Fact]
    public void The_progress_line_shows_while_the_first_scan_runs_and_goes_at_its_end()
    {
        game.File(0, 0);
        game.File(1, 0);
        game.File(2, 0);
        LegendPatches.CreatedPostfix(ui);
        ScanRunner.Begin(new ChunkReader(game.World, game.Persistence, GameWorld.Slot), ChunkReader.GeneratedChunks(game.World));

        LegendPatches.UpdatePostfix(ui);
        Assert.True(Progress().gameObject.activeSelf);
        Assert.Equal("Loading POIs... 0%", Progress().text);

        ScanRunner.Step(() => true);
        LegendPatches.UpdatePostfix(ui);
        Assert.Equal("Loading POIs... 33%", Progress().text);

        ScanRunner.Step(() => false);
        LegendPatches.UpdatePostfix(ui);
        Assert.False(Progress().gameObject.activeSelf);
    }

    [Fact]
    public void The_progress_text_is_set_only_when_the_percentage_changes()
    {
        game.File(0, 0);
        game.File(1, 0);
        LegendPatches.CreatedPostfix(ui);
        ScanRunner.Begin(new ChunkReader(game.World, game.Persistence, GameWorld.Slot), ChunkReader.GeneratedChunks(game.World));
        LegendPatches.UpdatePostfix(ui);
        var sets = Progress().TextSets;

        LegendPatches.UpdatePostfix(ui);
        LegendPatches.UpdatePostfix(ui);

        Assert.Equal(sets, Progress().TextSets);
    }

    [Fact]
    public void With_no_scan_the_progress_line_is_hidden()
    {
        LegendPatches.CreatedPostfix(ui);

        LegendPatches.UpdatePostfix(ui);

        Assert.False(Progress().gameObject.activeSelf);
    }

    [Fact]
    public void A_network_game_hides_the_legend()
    {
        LegendPatches.CreatedPostfix(ui);
        game.Network.Networked = true;

        LegendPatches.UpdatePostfix(ui);

        Assert.False(Root().gameObject.activeSelf);
    }

    [Fact]
    public void A_frame_before_the_legend_is_built_does_nothing()
    {
        LegendPatches.UpdatePostfix(ui);

        Assert.Null(Root());
        Assert.Empty(Plugin.Log.Warnings);
    }

    [Fact]
    public void An_exception_is_caught_and_logged_once()
    {
        ui.body = null;

        LegendPatches.CreatedPostfix(ui);
        LegendPatches.CreatedPostfix(ui);

        Assert.Single(Plugin.Log.Warnings);
    }
}
