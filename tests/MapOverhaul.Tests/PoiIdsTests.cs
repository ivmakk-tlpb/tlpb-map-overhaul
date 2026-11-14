using MapOverhaul;
using Xunit;

public class PoiIdsTests
{
    private static Poi Cabin(float x) => new Poi(PoiKind.Cabin, x, 0f);

    [Fact]
    public void Ids_start_at_the_top_of_the_int_range_and_go_down()
    {
        var ids = new PoiIds();

        Assert.Equal(new[] { int.MaxValue, int.MaxValue - 1 }, ids.Assign(new[] { Cabin(1), Cabin(2) }, new[] { "Cabin", "Cabin" }));
    }

    [Fact]
    public void A_poi_keeps_its_id_while_its_kind_position_and_text_stay()
    {
        var ids = new PoiIds();
        var first = ids.Assign(new[] { Cabin(1), Cabin(2) }, new[] { "Cabin", "Cabin" });

        var second = ids.Assign(new[] { Cabin(2), Cabin(1) }, new[] { "Cabin", "Cabin" });

        Assert.Equal(new[] { first[1], first[0] }, second);
    }

    [Fact]
    public void A_poi_whose_text_changes_gets_a_new_id()
    {
        var ids = new PoiIds();
        var deposit = new Poi(PoiKind.Iron, 5f, 5f, 4);
        int old = ids.Assign(new[] { deposit }, new[] { "Iron deposit (4)" })[0];

        int changed = ids.Assign(new[] { new Poi(PoiKind.Iron, 5f, 5f, 3) }, new[] { "Iron deposit (3)" })[0];

        Assert.NotEqual(old, changed);
        Assert.True(changed >= 1);
    }

    [Fact]
    public void A_gone_poi_frees_its_key()
    {
        var ids = new PoiIds();
        int old = ids.Assign(new[] { Cabin(1) }, new[] { "Cabin" })[0];
        ids.Assign(new Poi[0], new string[0]);

        int again = ids.Assign(new[] { Cabin(1) }, new[] { "Cabin" })[0];

        Assert.NotEqual(old, again);
    }

    [Fact]
    public void IsMod_tells_a_mod_id_from_a_game_entity_id()
    {
        var ids = new PoiIds();
        int id = ids.Assign(new[] { Cabin(1) }, new[] { "Cabin" })[0];

        Assert.True(ids.IsMod(id));
        Assert.False(ids.IsMod(12345));
        Assert.False(ids.IsMod(0));
    }

    [Fact]
    public void KindOf_gives_the_kind_of_a_mod_id()
    {
        var ids = new PoiIds();
        int id = ids.Assign(new[] { new Poi(PoiKind.Cave, 1f, 1f) }, new[] { "Cave" })[0];

        Assert.True(ids.TryKindOf(id, out var kind));
        Assert.Equal(PoiKind.Cave, kind);
        Assert.False(ids.TryKindOf(5, out _));
    }

    [Fact]
    public void Clear_forgets_every_id()
    {
        var ids = new PoiIds();
        int id = ids.Assign(new[] { Cabin(1) }, new[] { "Cabin" })[0];

        ids.Clear();

        Assert.False(ids.IsMod(id));
    }
}
