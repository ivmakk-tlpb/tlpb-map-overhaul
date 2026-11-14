using System;
using System.Collections.Generic;

namespace MapOverhaul
{
    // One POI of the region map, with the data of its hover text.
    internal sealed class Poi
    {
        internal readonly PoiKind Kind;
        internal readonly float X;
        internal readonly float Z;

        // The rock count of an ore deposit.
        internal readonly int Count;

        // The ore rocks inside a cave for each metal.
        internal readonly int[] CaveOres;

        internal Poi(PoiKind kind, float x, float z, int count = 0, int[] caveOres = null)
        {
            Kind = kind;
            X = x;
            Z = z;
            Count = count;
            CaveOres = caveOres;
        }
    }

    // Makes the POI list from the index and the chunk info caves: each cabin, each cave, and each ore deposit.
    // It builds the list again only after a change.
    internal sealed class PoiBuilder
    {
        private readonly PoiIndex index;
        private readonly Action<string> log;
        private IReadOnlyList<CaveOrigin> origins = new CaveOrigin[0];
        private List<Poi> pois = new List<Poi>();
        private bool originsChanged = true;

        internal PoiBuilder(PoiIndex index, Action<string> log)
        {
            this.index = index;
            this.log = log;
        }

        // Counts each build of the list.
        internal int Version { get; private set; }

        internal void SetCaveOrigins(IReadOnlyList<CaveOrigin> caves)
        {
            origins = caves;
            originsChanged = true;
        }

        internal IReadOnlyList<Poi> Get()
        {
            if (!index.Changed && !originsChanged) return pois;
            index.ClearChanged();
            originsChanged = false;
            Version++;
            pois = Build();
            return pois;
        }

        private List<Poi> Build()
        {
            var result = new List<Poi>();
            foreach (var find in index.All())
                if (find.Kind == FindKind.Cabin) result.Add(new Poi(PoiKind.Cabin, find.X, find.Z));
            foreach (var cave in CaveBuilder.Build(origins, index.All(), log))
                result.Add(new Poi(PoiKind.Cave, cave.X, cave.Z, caveOres: cave.Ores));
            foreach (var deposit in DepositGrouper.Group(index.All()))
                result.Add(new Poi(KindOf(deposit.Metal), deposit.X, deposit.Z, deposit.Count));
            return result;
        }

        internal static PoiKind KindOf(Metal metal) => PoiKind.Tin + (int)metal;
    }
}
