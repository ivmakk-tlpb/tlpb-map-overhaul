using System;

namespace MapOverhaul
{
    // The on and off state of each row of the map legend: one row for each POI kind, then the fog of war.
    // A change goes to the save callback, which writes the config entry of that row.
    internal sealed class Settings
    {
        internal const int FogRow = 6;
        internal const int RowCount = 7;

        private readonly bool[] shown;
        private readonly Action<int, bool> save;

        internal Settings(bool[] shown, Action<int, bool> save)
        {
            this.shown = (bool[])shown.Clone();
            this.save = save;
        }

        internal static int Row(PoiKind kind) => (int)kind;

        internal bool IsOn(PoiKind kind) => shown[Row(kind)];

        internal bool FogOfWar => shown[FogRow];

        internal bool Get(int row) => shown[row];

        internal void Set(int row, bool value)
        {
            if (shown[row] == value) return;
            shown[row] = value;
            save(row, value);
        }
    }
}
