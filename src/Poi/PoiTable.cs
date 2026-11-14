namespace MapOverhaul
{
    // Maps an entity identifier to its POI kind. The parts of a cabin, such as cabin_roof_large, are no POI.
    internal static class PoiTable
    {
        private static readonly (string Prefix, Metal Metal)[] OreRocks =
        {
            ("tin_rock_resource_", Metal.Tin),
            ("copper_rock_resource_", Metal.Copper),
            ("iron_rock_resource_", Metal.Iron),
            ("silver_rock_resource_", Metal.Silver),
        };

        internal static bool TryGet(string identifier, out FindKind kind, out Metal metal)
        {
            kind = FindKind.Cabin;
            metal = Metal.Tin;
            if (string.IsNullOrEmpty(identifier)) return false;
            if (identifier == "cabin1" || identifier == "cabin2") return true;
            if (identifier.StartsWith("cave_entrance_", System.StringComparison.Ordinal))
            {
                kind = FindKind.CaveEntrance;
                return true;
            }
            foreach (var ore in OreRocks)
            {
                if (!identifier.StartsWith(ore.Prefix, System.StringComparison.Ordinal)) continue;
                kind = FindKind.OreRock;
                metal = ore.Metal;
                return true;
            }
            return false;
        }
    }
}
