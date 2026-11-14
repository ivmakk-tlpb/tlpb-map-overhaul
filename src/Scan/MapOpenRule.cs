namespace MapOverhaul
{
    // The game asks for the region map on each frame while the map is open. A call more than one frame after
    // the previous call is a new map open.
    internal sealed class MapOpenRule
    {
        private int last = int.MinValue;

        internal bool IsOpen(int frame)
        {
            bool open = last == int.MinValue || frame - last > 1;
            last = frame;
            return open;
        }

        internal void Reset() => last = int.MinValue;
    }
}
