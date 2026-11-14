namespace MapOverhaul.Game
{
    // The game's names of the ore items, in the order of Metal, for the ore lines of a cave hover text.
    internal static class ItemNames
    {
        private static readonly string[] Keys = { "item.tin_ore.name", "item.copper_ore.name", "item.iron_ore.name", "item.silver_ore.name" };
        private static readonly string[] English = { "Tin Ore", "Copper Ore", "Iron Ore", "Silver Ore" };

        internal static string[] Ores()
        {
            var names = new string[Keys.Length];
            for (int i = 0; i < Keys.Length; i++)
            {
                string text = L.T(Keys[i]);
                names[i] = string.IsNullOrEmpty(text) || text == Keys[i] ? English[i] : text;
            }
            return names;
        }
    }
}
