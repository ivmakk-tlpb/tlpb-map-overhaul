using MapOverhaul;
using Xunit;

public class DepositGrouperTests
{
    private static Find Rock(Metal metal, float x, float z, int level = 1) =>
        new Find(FindKind.OreRock, metal, x, 5f, z, level, default, false);

    [Fact]
    public void Four_close_iron_rocks_give_one_deposit_at_their_mean()
    {
        var deposits = DepositGrouper.Group(new[]
        {
            Rock(Metal.Iron, 100, 100), Rock(Metal.Iron, 110, 100), Rock(Metal.Iron, 100, 110), Rock(Metal.Iron, 110, 110),
        });

        var deposit = Assert.Single(deposits);
        Assert.Equal(Metal.Iron, deposit.Metal);
        Assert.Equal(4, deposit.Count);
        Assert.Equal(105f, deposit.X, 3);
        Assert.Equal(105f, deposit.Z, 3);
    }

    [Fact]
    public void Two_rocks_31_m_apart_give_two_deposits()
    {
        var deposits = DepositGrouper.Group(new[] { Rock(Metal.Iron, 0, 0), Rock(Metal.Iron, 31, 0) });

        Assert.Equal(2, deposits.Count);
        Assert.All(deposits, d => Assert.Equal(1, d.Count));
    }

    [Fact]
    public void A_chain_of_rocks_20_m_apart_gives_one_deposit()
    {
        var deposits = DepositGrouper.Group(new[]
        {
            Rock(Metal.Copper, 0, 0), Rock(Metal.Copper, 20, 0), Rock(Metal.Copper, 40, 0), Rock(Metal.Copper, 60, 0), Rock(Metal.Copper, 80, 0),
        });

        var deposit = Assert.Single(deposits);
        Assert.Equal(5, deposit.Count);
        Assert.Equal(40f, deposit.X, 3);
    }

    [Fact]
    public void A_tin_rock_and_a_copper_rock_5_m_apart_give_two_deposits()
    {
        var deposits = DepositGrouper.Group(new[] { Rock(Metal.Tin, 0, 0), Rock(Metal.Copper, 5, 0) });

        Assert.Equal(new[] { Metal.Tin, Metal.Copper }, deposits.Select(d => d.Metal).OrderBy(m => m));
    }

    [Fact]
    public void Rocks_inside_a_cave_give_no_deposit()
    {
        var deposits = DepositGrouper.Group(new[] { Rock(Metal.Iron, 0, 0, -1), Rock(Metal.Iron, 5, 0, -2) });

        Assert.Empty(deposits);
    }

    [Fact]
    public void Rocks_of_each_level_from_0_up_are_on_the_surface()
    {
        var deposits = DepositGrouper.Group(new[] { Rock(Metal.Iron, 0, 0, 0), Rock(Metal.Iron, 5, 0, 1), Rock(Metal.Iron, 10, 0, 2) });

        Assert.Equal(3, Assert.Single(deposits).Count);
    }

    [Fact]
    public void Other_finds_give_no_deposit()
    {
        var deposits = DepositGrouper.Group(new[]
        {
            new Find(FindKind.Cabin, Metal.Tin, 0, 0, 0, 0, default, false),
            new Find(FindKind.CaveEntrance, Metal.Tin, 0, 0, 0, 0, default, false),
        });

        Assert.Empty(deposits);
    }

    [Fact]
    public void Rocks_on_the_edge_of_a_grid_cell_group_with_the_next_cell()
    {
        var deposits = DepositGrouper.Group(new[] { Rock(Metal.Iron, 29.9f, -0.1f), Rock(Metal.Iron, 30.1f, 0.1f) });

        Assert.Equal(2, Assert.Single(deposits).Count);
    }

    [Fact]
    public void Rocks_with_negative_coordinates_group()
    {
        var deposits = DepositGrouper.Group(new[] { Rock(Metal.Silver, -61, -61), Rock(Metal.Silver, -59, -59), Rock(Metal.Silver, -200, -200) });

        Assert.Equal(new[] { 1, 2 }, deposits.Select(d => d.Count).OrderBy(c => c));
    }
}
