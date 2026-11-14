using MapOverhaul;
using Xunit;

public class SettingsTests
{
    private readonly List<(int Row, bool Value)> saves = new List<(int, bool)>();

    private Settings Make(bool[] shown) => new Settings(shown, (row, value) => saves.Add((row, value)));

    private static bool[] AllOn() => Enumerable.Repeat(true, Settings.RowCount).ToArray();

    [Theory]
    [InlineData(PoiKind.Cabin, 0)]
    [InlineData(PoiKind.Cave, 1)]
    [InlineData(PoiKind.Tin, 2)]
    [InlineData(PoiKind.Copper, 3)]
    [InlineData(PoiKind.Iron, 4)]
    [InlineData(PoiKind.Silver, 5)]
    public void Each_poi_kind_reads_its_own_row(PoiKind kind, int row)
    {
        var shown = AllOn();
        shown[row] = false;

        var settings = Make(shown);

        Assert.False(settings.IsOn(kind));
        Assert.Single(Enum.GetValues<PoiKind>(), k => !settings.IsOn(k));
        Assert.Equal(row, Settings.Row(kind));
    }

    [Fact]
    public void The_fog_of_war_reads_the_last_row()
    {
        var shown = AllOn();
        shown[6] = false;

        var settings = Make(shown);

        Assert.Equal(6, Settings.FogRow);
        Assert.Equal(7, Settings.RowCount);
        Assert.False(settings.FogOfWar);
        Assert.All(Enum.GetValues<PoiKind>(), k => Assert.True(settings.IsOn(k)));
    }

    [Fact]
    public void A_change_calls_the_save_callback_once()
    {
        var settings = Make(AllOn());

        settings.Set(4, false);

        Assert.False(settings.IsOn(PoiKind.Iron));
        Assert.Equal(new[] { (4, false) }, saves);
    }

    [Fact]
    public void A_set_to_the_same_value_saves_nothing()
    {
        var settings = Make(AllOn());

        settings.Set(Settings.FogRow, true);

        Assert.Empty(saves);
    }

    [Fact]
    public void Get_reads_a_row()
    {
        var shown = AllOn();
        shown[2] = false;

        var settings = Make(shown);

        Assert.False(settings.Get(2));
        Assert.True(settings.Get(3));
    }

    [Fact]
    public void The_settings_keep_their_own_copy_of_the_values()
    {
        var shown = AllOn();
        var settings = Make(shown);

        shown[0] = false;

        Assert.True(settings.IsOn(PoiKind.Cabin));
    }
}
