using MapOverhaul;
using Xunit;

public class PluginTests : GameTestBase
{
    [Fact]
    public void The_guid_names_the_mod()
    {
        Assert.Equal("com.ivmakk.thelastplagueblight.mapoverhaul", Plugin.PluginGuid);
    }

    [Theory]
    [InlineData("Map/ShowCabins")]
    [InlineData("Map/ShowCaves")]
    [InlineData("Map/ShowTin")]
    [InlineData("Map/ShowCopper")]
    [InlineData("Map/ShowIron")]
    [InlineData("Map/ShowSilver")]
    [InlineData("Map/ShowFogOfWar")]
    public void Each_map_setting_is_on_by_default(string key)
    {
        Assert.True(ConfigValue(key));
    }

    [Fact]
    public void Debug_lines_are_off_by_default()
    {
        Assert.False(ConfigValue("General/Verbose"));
    }

    [Fact]
    public void The_settings_start_from_the_config()
    {
        Assert.All(Enum.GetValues<PoiKind>(), kind => Assert.True(Plugin.Settings.IsOn(kind)));
        Assert.True(Plugin.Settings.FogOfWar);
    }

    [Fact]
    public void A_settings_change_writes_its_config_entry()
    {
        Plugin.Settings.Set(Settings.Row(PoiKind.Cave), false);
        Plugin.Settings.Set(Settings.FogRow, false);

        Assert.False(ConfigValue("Map/ShowCaves"));
        Assert.False(ConfigValue("Map/ShowFogOfWar"));
        Assert.True(ConfigValue("Map/ShowCabins"));
    }

    [Fact]
    public void The_debug_helper_writes_only_while_verbose_is_on()
    {
        Plugin.Debug("hidden");
        Plugin.Verbose.Value = true;
        Plugin.Debug("shown");

        Assert.Equal(new[] { "shown" }, Plugin.Log.Debugs);
    }

    [Fact]
    public void The_plugin_logs_that_it_loaded_with_no_warning()
    {
        Assert.Contains("Map Overhaul loaded.", Plugin.Log.Infos);
        Assert.Empty(Plugin.Log.Warnings);
    }

    [Fact]
    public void The_mod_has_three_feature_groups()
    {
        Assert.Equal(new[] { "poi", "fog", "legend" }, Plugin.Groups.Select(g => g.Name));
    }

    [Fact]
    public void The_scan_runner_is_registered_and_added()
    {
        Assert.Contains(typeof(MapOverhaul.Game.ScanRunner), Il2CppInterop.Runtime.Injection.ClassInjector.Registered);
        Assert.Contains(typeof(MapOverhaul.Game.ScanRunner), BepInEx.Unity.IL2CPP.BasePlugin.Added);
    }

    [Fact]
    public void A_failed_register_of_the_scan_runner_removes_the_poi_group_with_one_warning()
    {
        FakeState.Reset();
        Il2CppInterop.Runtime.Injection.ClassInjector.ThrowFor = typeof(MapOverhaul.Game.ScanRunner);
        new Plugin().Load();

        Assert.Equal(new[] { "Map Overhaul shows no POIs on the region map: register failed" }, Plugin.Log.Warnings);
        Assert.DoesNotContain("poi", Plugin.Applied);
        Assert.Contains("fog", Plugin.Applied);
    }

    [Fact]
    public void The_fog_patches_are_in_the_fog_group()
    {
        Assert.Equal(new[] { typeof(MapOverhaul.Patches.FogPatches) }, Plugin.Groups.Single(g => g.Name == "fog").Types);
        Assert.Contains(typeof(MapOverhaul.Patches.FogPatches), HarmonyLib.Harmony.Patched);
    }

    [Fact]
    public void The_legend_patches_are_in_the_legend_group()
    {
        Assert.Equal(new[] { typeof(MapOverhaul.Patches.LegendPatches) }, Plugin.Groups.Single(g => g.Name == "legend").Types);
        Assert.Contains(typeof(MapOverhaul.Patches.LegendPatches), HarmonyLib.Harmony.Patched);
    }

    [Fact]
    public void The_scan_patches_are_in_the_poi_group()
    {
        Assert.Contains(typeof(MapOverhaul.Patches.ScanPatches), Plugin.Groups.Single(g => g.Name == "poi").Types);
        Assert.Contains(typeof(MapOverhaul.Patches.ScanPatches), HarmonyLib.Harmony.Patched);
    }

    [Fact]
    public void A_missing_poi_target_removes_only_that_group_with_one_warning()
    {
        FakeState.Reset();
        HarmonyLib.Harmony.ThrowFor = typeof(MapOverhaul.Patches.ScanPatches);
        new Plugin().Load();

        Assert.Equal(new[] { Plugin.PluginGuid + ".poi" }, HarmonyLib.Harmony.Unpatched);
        Assert.Equal(new[] { "Map Overhaul shows no POIs on the region map: Target not found" }, Plugin.Log.Warnings);
        Assert.Contains("fog", Plugin.Applied);
    }

    [Fact]
    public void A_repeated_error_gives_one_warning()
    {
        Plugin.WarnOnce("Part", new InvalidOperationException("broken"));
        Plugin.WarnOnce("Part", new InvalidOperationException("broken"));

        Assert.Single(Plugin.Log.Warnings);
    }
}
