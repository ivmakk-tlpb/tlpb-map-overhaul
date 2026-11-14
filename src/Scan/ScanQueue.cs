using System;
using System.Collections.Generic;

namespace MapOverhaul
{
    // The chunks that wait for a read, each once, in order.
    internal sealed class ScanQueue
    {
        private readonly LinkedList<ChunkCoord> order = new LinkedList<ChunkCoord>();
        private readonly Dictionary<ChunkCoord, LinkedListNode<ChunkCoord>> nodes = new Dictionary<ChunkCoord, LinkedListNode<ChunkCoord>>();

        internal int Count => order.Count;

        internal void Add(ChunkCoord chunk)
        {
            if (nodes.ContainsKey(chunk)) return;
            nodes[chunk] = order.AddLast(chunk);
        }

        // Moves the chunks to the front, in the given order. A chunk that is not in the queue comes in.
        internal void Front(IEnumerable<ChunkCoord> chunks)
        {
            LinkedListNode<ChunkCoord> last = null;
            foreach (var chunk in chunks)
            {
                if (nodes.TryGetValue(chunk, out var node)) order.Remove(node);
                else node = new LinkedListNode<ChunkCoord>(chunk);
                if (last == null) order.AddFirst(node);
                else order.AddAfter(last, node);
                nodes[chunk] = node;
                last = node;
            }
        }

        // Reads chunks from the front until the budget is used, and at least one chunk. Gives the count read.
        internal int TakeWhile(Func<bool> budgetUsed, Action<ChunkCoord> read)
        {
            int taken = 0;
            while (order.Count > 0)
            {
                var chunk = order.First.Value;
                order.RemoveFirst();
                nodes.Remove(chunk);
                read(chunk);
                taken++;
                if (budgetUsed()) break;
            }
            return taken;
        }

        internal void Clear()
        {
            order.Clear();
            nodes.Clear();
        }
    }
}
