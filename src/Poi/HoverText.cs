using System.Text;

namespace MapOverhaul
{
    // The hover text of a POI: "Cabin", "Iron deposit (4)", or "Cave" with one line for each metal inside
    // the cave. The ore names come from the game, in the order of Metal.
    internal static class HoverText
    {
        private static readonly string[] MetalNames = { "Tin", "Copper", "Iron", "Silver" };

        internal static string Of(Poi poi, string[] oreNames)
        {
            switch (poi.Kind)
            {
                case PoiKind.Cabin: return "Cabin";
                case PoiKind.Cave: return Cave(poi, oreNames);
                default: return MetalNames[poi.Kind - PoiKind.Tin] + " deposit (" + poi.Count + ")";
            }
        }

        private static string Cave(Poi poi, string[] oreNames)
        {
            var text = new StringBuilder("Cave");
            for (int metal = 0; metal < MetalNames.Length; metal++)
                if (poi.CaveOres[metal] > 0) text.Append('\n').Append(oreNames[metal]).Append(" (").Append(poi.CaveOres[metal]).Append(')');
            return text.ToString();
        }
    }
}
