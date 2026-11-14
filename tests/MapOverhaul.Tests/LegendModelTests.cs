using MapOverhaul;
using Xunit;

public class LegendModelTests
{
    private readonly List<(int Row, bool Value)> saves = new List<(int, bool)>();
    private readonly Settings settings;
    private readonly LegendModel model;

    public LegendModelTests()
    {
        settings = new Settings(new[] { true, true, true, true, true, true, true }, (row, value) => saves.Add((row, value)));
        model = new LegendModel(settings);
    }

    [Fact]
    public void The_rows_are_the_poi_kinds_and_the_fog_of_war_in_order()
    {
        Assert.Equal(new[] { "Cabins", "Caves", "Tin", "Copper", "Iron", "Silver", "Fog of war" }, model.Names);
    }

    [Fact]
    public void Each_row_shows_its_setting()
    {
        settings.Set(Settings.Row(PoiKind.Iron), false);

        Assert.False(model.IsOn(Settings.Row(PoiKind.Iron)));
        Assert.True(model.IsOn(Settings.FogRow));
    }

    [Fact]
    public void A_flip_changes_the_setting_and_asks_for_one_config_save()
    {
        model.Flip(Settings.Row(PoiKind.Iron));

        Assert.False(settings.IsOn(PoiKind.Iron));
        Assert.Equal(new[] { (Settings.Row(PoiKind.Iron), false) }, saves);

        model.Flip(Settings.Row(PoiKind.Iron));
        Assert.True(settings.IsOn(PoiKind.Iron));
        Assert.Equal(2, saves.Count);
    }

    [Theory]
    [InlineData(459, 1000, "Loading POIs... 45%")]
    [InlineData(0, 1054, "Loading POIs... 0%")]
    [InlineData(1053, 1054, "Loading POIs... 99%")]
    public void The_progress_text_gives_the_share_of_the_chunks_read_rounded_down(int read, int total, string text)
    {
        Assert.Equal(text, LegendModel.ProgressText(read, total));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1054, 1054)]
    public void There_is_no_progress_text_with_no_first_scan_or_at_its_end(int read, int total)
    {
        Assert.Null(LegendModel.ProgressText(read, total));
    }
}
