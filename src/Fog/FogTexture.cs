namespace MapOverhaul
{
    // The alpha values of the fog of war over the region: one pixel for each chunk, clear for a chunk that the game
    // generated, else dark (alpha 0.7). Row 0 is the south edge of the region (the lowest chunk y) and column 0 its west edge.
    internal sealed class FogTexture
    {
        internal const float FogAlpha = 0.7f;

        private readonly int minX;
        private readonly int minY;

        internal FogTexture(int minX, int minY, int maxX, int maxY)
        {
            this.minX = minX;
            this.minY = minY;
            Width = maxX - minX + 1;
            Height = maxY - minY + 1;
            Alpha = new float[Width * Height];
            for (int i = 0; i < Alpha.Length; i++) Alpha[i] = FogAlpha;
        }

        internal int Width { get; }

        internal int Height { get; }

        // Row by row from the south edge.
        internal float[] Alpha { get; }

        internal float AlphaAt(int x, int y) => Alpha[y * Width + x];

        // Clears a generated chunk. Gives true when it was under the fog.
        internal bool SetGenerated(int chunkX, int chunkY)
        {
            int x = chunkX - minX;
            int y = chunkY - minY;
            if (x < 0 || y < 0 || x >= Width || y >= Height) return false;
            int slot = y * Width + x;
            if (Alpha[slot] == 0f) return false;
            Alpha[slot] = 0f;
            return true;
        }
    }
}
