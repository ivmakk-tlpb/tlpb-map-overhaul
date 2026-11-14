using MapOverhaul;
using Xunit;

public class ScanQueueTests
{
    private static ChunkCoord C(int x) => new ChunkCoord(x, 0);

    private static List<ChunkCoord> TakeAll(ScanQueue queue)
    {
        var read = new List<ChunkCoord>();
        queue.TakeWhile(() => false, read.Add);
        return read;
    }

    [Fact]
    public void It_keeps_each_chunk_once()
    {
        var queue = new ScanQueue();
        queue.Add(C(1));
        queue.Add(C(2));
        queue.Add(C(1));

        Assert.Equal(2, queue.Count);
        Assert.Equal(new[] { C(1), C(2) }, TakeAll(queue));
    }

    [Fact]
    public void Front_moves_the_given_chunks_to_the_front()
    {
        var queue = new ScanQueue();
        foreach (int x in new[] { 1, 2, 3 }) queue.Add(C(x));

        queue.Front(new[] { C(3), C(9) });

        Assert.Equal(new[] { C(3), C(9), C(1), C(2) }, TakeAll(queue));
    }

    [Fact]
    public void TakeWhile_stops_when_the_budget_is_used()
    {
        var queue = new ScanQueue();
        foreach (int x in new[] { 1, 2, 3, 4 }) queue.Add(C(x));
        var read = new List<ChunkCoord>();

        int taken = queue.TakeWhile(() => read.Count >= 2, read.Add);

        Assert.Equal(2, taken);
        Assert.Equal(new[] { C(1), C(2) }, read);
        Assert.Equal(2, queue.Count);
    }

    [Fact]
    public void TakeWhile_takes_at_least_one_chunk()
    {
        var queue = new ScanQueue();
        queue.Add(C(1));
        queue.Add(C(2));
        var read = new List<ChunkCoord>();

        queue.TakeWhile(() => true, read.Add);

        Assert.Equal(new[] { C(1) }, read);
    }

    [Fact]
    public void TakeWhile_on_an_empty_queue_takes_nothing()
    {
        var queue = new ScanQueue();
        var read = new List<ChunkCoord>();

        Assert.Equal(0, queue.TakeWhile(() => false, read.Add));
        Assert.Empty(read);
    }

    [Fact]
    public void A_taken_chunk_can_be_added_again()
    {
        var queue = new ScanQueue();
        queue.Add(C(1));
        TakeAll(queue);

        queue.Add(C(1));

        Assert.Equal(1, queue.Count);
    }

    [Fact]
    public void Clear_empties_it()
    {
        var queue = new ScanQueue();
        queue.Add(C(1));
        queue.Clear();

        Assert.Equal(0, queue.Count);
        Assert.Empty(TakeAll(queue));
    }
}
