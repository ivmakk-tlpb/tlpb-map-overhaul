using MapOverhaul;
using Xunit;

public class PoiIndexTests
{
    private static readonly ChunkCoord A = new ChunkCoord(1, 2);
    private static readonly ChunkCoord B = new ChunkCoord(-3, 4);

    private static Find Iron(float x, float z, int level = 1) =>
        new Find(FindKind.OreRock, Metal.Iron, x, 10f, z, level, new ChunkCoord(5, 6), true);

    [Fact]
    public void A_find_holds_its_kind_position_level_and_cave_origin()
    {
        var find = new Find(FindKind.OreRock, Metal.Copper, 1f, 2f, 3f, -1, new ChunkCoord(7, 8), true);

        Assert.Equal(FindKind.OreRock, find.Kind);
        Assert.Equal(Metal.Copper, find.Metal);
        Assert.Equal((1f, 2f, 3f), (find.X, find.Y, find.Z));
        Assert.Equal(-1, find.Level);
        Assert.True(find.HasCave);
        Assert.Equal(new ChunkCoord(7, 8), find.CaveOrigin);
    }

    [Theory]
    [InlineData(-2, true)]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    public void A_find_of_a_negative_level_is_inside_a_cave(int level, bool inCave)
    {
        Assert.Equal(inCave, Iron(0, 0, level).InCave);
    }

    [Fact]
    public void A_find_compares_by_value()
    {
        Assert.True(Iron(1, 1).Equals((object)Iron(1, 1)));
        Assert.False(Iron(1, 1).Equals((object)"other"));
        Assert.Equal(Iron(1, 1).GetHashCode(), Iron(1, 1).GetHashCode());
    }

    [Fact]
    public void A_chunk_coordinate_compares_by_value()
    {
        Assert.Equal(new ChunkCoord(1, 2), A);
        Assert.NotEqual(new ChunkCoord(2, 1), A);
        Assert.Equal(new ChunkCoord(1, 2).GetHashCode(), A.GetHashCode());
        Assert.Equal("(1, 2)", A.ToString());
    }

    [Fact]
    public void A_chunk_read_replaces_the_whole_entry_of_that_chunk()
    {
        var index = new PoiIndex();
        index.Put(A, new[] { Iron(1, 1), Iron(2, 2) });
        index.Put(B, new[] { Iron(9, 9) });

        index.Put(A, new[] { Iron(3, 3) });

        Assert.Equal(new[] { Iron(3, 3) }, index.Get(A));
        Assert.Equal(new[] { Iron(9, 9) }, index.Get(B));
        Assert.Equal(2, index.ChunkCount);
    }

    [Fact]
    public void A_read_with_no_finds_removes_the_entry()
    {
        var index = new PoiIndex();
        index.Put(A, new[] { Iron(1, 1) });

        index.Put(A, new Find[0]);

        Assert.Empty(index.Get(A));
        Assert.Equal(0, index.ChunkCount);
    }

    [Fact]
    public void A_change_sets_the_changed_flag_until_it_is_cleared()
    {
        var index = new PoiIndex();
        Assert.False(index.Changed);

        index.Put(A, new[] { Iron(1, 1) });
        Assert.True(index.Changed);

        index.ClearChanged();
        Assert.False(index.Changed);
    }

    [Fact]
    public void A_read_with_the_same_finds_is_no_change()
    {
        var index = new PoiIndex();
        index.Put(A, new[] { Iron(1, 1) });
        index.ClearChanged();

        index.Put(A, new[] { Iron(1, 1) });
        index.Put(B, new Find[0]);

        Assert.False(index.Changed);
    }

    [Fact]
    public void Clear_empties_the_index_and_marks_a_change()
    {
        var index = new PoiIndex();
        index.Put(A, new[] { Iron(1, 1) });
        index.ClearChanged();

        index.Clear();

        Assert.Equal(0, index.ChunkCount);
        Assert.True(index.Changed);
    }

    [Fact]
    public void All_gives_the_finds_of_each_chunk()
    {
        var index = new PoiIndex();
        index.Put(A, new[] { Iron(1, 1) });
        index.Put(B, new[] { Iron(2, 2), Iron(3, 3) });

        Assert.Equal(3, index.All().Count());
    }
}
