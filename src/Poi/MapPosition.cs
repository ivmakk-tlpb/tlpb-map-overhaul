using System;

namespace MapOverhaul
{
    // The place of a world position on the region map: the fraction of the region in each direction, from -0.5
    // to 0.5 around the center, as the game places its own map objects.
    internal readonly struct MapPosition
    {
        private readonly float minX;
        private readonly float minZ;
        private readonly float sizeX;
        private readonly float sizeZ;
        private readonly int chunkSize;

        internal MapPosition(int minChunkX, int minChunkY, int maxChunkX, int maxChunkY, int chunkSize)
        {
            this.chunkSize = chunkSize;
            minX = minChunkX * chunkSize;
            minZ = minChunkY * chunkSize;
            sizeX = (maxChunkX + 1) * chunkSize - minX;
            sizeZ = (maxChunkY + 1) * chunkSize - minZ;
        }

        internal (float X, float Y) Relative(float worldX, float worldZ) => (Fraction(worldX, minX, sizeX), Fraction(worldZ, minZ, sizeZ));

        internal ChunkCoord ChunkOf(float worldX, float worldZ) =>
            new ChunkCoord((int)Math.Floor(worldX / chunkSize), (int)Math.Floor(worldZ / chunkSize));

        private static float Fraction(float world, float min, float size)
        {
            if (size <= 0f) return 0f;
            return Math.Max(0f, Math.Min(1f, (world - min) / size)) - 0.5f;
        }
    }
}
