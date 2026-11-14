namespace MapOverhaul
{
    // The rows of the map legend in the order of Settings, with their names and states, and the progress text of
    // the first scan after a save load.
    internal sealed class LegendModel
    {
        private readonly Settings settings;

        internal LegendModel(Settings settings)
        {
            this.settings = settings;
        }

        internal string[] Names { get; } = { "Cabins", "Caves", "Tin", "Copper", "Iron", "Silver", "Fog of war" };

        internal bool IsOn(int row) => settings.Get(row);

        internal void Flip(int row) => settings.Set(row, !settings.Get(row));

        // "Loading POIs... 45%" with the share of the chunks read rounded down, or null when no first scan runs.
        internal static string ProgressText(int read, int total)
        {
            if (total <= 0 || read >= total) return null;
            return "Loading POIs... " + (read * 100 / total) + "%";
        }
    }
}
