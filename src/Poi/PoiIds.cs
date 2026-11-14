using System.Collections.Generic;

namespace MapOverhaul
{
    // The map ids of the POIs, from int.MaxValue down, far above the game entity ids. A POI keeps its id while its
    // kind, its position, and its hover text stay. The game makes the hover label of an icon only for a new id, so a
    // POI whose text changes gets a new id.
    internal sealed class PoiIds
    {
        private readonly Dictionary<(PoiKind, float, float, string), int> byKey = new Dictionary<(PoiKind, float, float, string), int>();
        private readonly Dictionary<int, PoiKind> kinds = new Dictionary<int, PoiKind>();
        private int next = int.MaxValue;

        internal int[] Assign(IReadOnlyList<Poi> pois, IReadOnlyList<string> texts)
        {
            var ids = new int[pois.Count];
            var kept = new Dictionary<(PoiKind, float, float, string), int>();
            for (int i = 0; i < pois.Count; i++)
            {
                var key = (pois[i].Kind, pois[i].X, pois[i].Z, texts[i]);
                if (!byKey.TryGetValue(key, out int id) || kept.ContainsKey(key)) id = next--;
                kept[key] = id;
                ids[i] = id;
            }
            byKey.Clear();
            kinds.Clear();
            foreach (var pair in kept)
            {
                byKey[pair.Key] = pair.Value;
                kinds[pair.Value] = pair.Key.Item1;
            }
            return ids;
        }

        internal bool IsMod(int id) => kinds.ContainsKey(id);

        internal bool TryKindOf(int id, out PoiKind kind) => kinds.TryGetValue(id, out kind);

        internal void Clear()
        {
            byKey.Clear();
            kinds.Clear();
        }
    }
}
