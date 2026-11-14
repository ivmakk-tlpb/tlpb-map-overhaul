using System.Collections.Generic;
using System.Linq;

namespace MapOverhaul
{
    // The POI entities of each chunk read so far. A chunk read replaces the whole entry of that chunk,
    // so a rock that the game removed goes with the next read of its chunk.
    internal sealed class PoiIndex
    {
        private static readonly Find[] None = new Find[0];

        private readonly Dictionary<ChunkCoord, Find[]> chunks = new Dictionary<ChunkCoord, Find[]>();

        // True after a change of the finds, until PoiBuilder builds the POI list again.
        internal bool Changed { get; private set; }

        internal int ChunkCount => chunks.Count;

        internal void Put(ChunkCoord chunk, IReadOnlyCollection<Find> finds)
        {
            chunks.TryGetValue(chunk, out var old);
            if (finds.Count == 0)
            {
                if (old == null) return;
                chunks.Remove(chunk);
            }
            else
            {
                if (old != null && old.SequenceEqual(finds)) return;
                chunks[chunk] = finds.ToArray();
            }
            Changed = true;
        }

        internal IReadOnlyList<Find> Get(ChunkCoord chunk) => chunks.TryGetValue(chunk, out var finds) ? finds : None;

        internal IEnumerable<Find> All() => chunks.Values.SelectMany(finds => finds);

        internal void ClearChanged() => Changed = false;

        internal void Clear()
        {
            chunks.Clear();
            Changed = true;
        }
    }
}
