using System;

namespace MapOverhaul
{
    // The metals of the ore rocks, in the order of the ore lines of a cave hover text.
    public enum Metal
    {
        Tin,
        Copper,
        Iron,
        Silver,
    }

    // What a POI entity of a chunk is.
    public enum FindKind
    {
        Cabin,
        CaveEntrance,
        OreRock,
    }

    // The coordinates of a world chunk.
    public readonly struct ChunkCoord : IEquatable<ChunkCoord>
    {
        public readonly int X;
        public readonly int Y;

        public ChunkCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(ChunkCoord other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is ChunkCoord other && Equals(other);

        public override int GetHashCode() => unchecked(X * 397 ^ Y);

        public override string ToString() => "(" + X + ", " + Y + ")";
    }

    // One POI entity of a chunk: its kind, its world position, its level (negative inside a cave), and the cave origin
    // chunk of a cave entrance or of an ore rock inside a cave.
    public readonly struct Find : IEquatable<Find>
    {
        public readonly FindKind Kind;
        public readonly Metal Metal;
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly int Level;
        public readonly ChunkCoord CaveOrigin;
        public readonly bool HasCave;

        public Find(FindKind kind, Metal metal, float x, float y, float z, int level, ChunkCoord caveOrigin, bool hasCave)
        {
            Kind = kind;
            Metal = metal;
            X = x;
            Y = y;
            Z = z;
            Level = level;
            CaveOrigin = caveOrigin;
            HasCave = hasCave;
        }

        // The save gives level 1 on the surface, 2 for a part of a building, and -1 inside a cave.
        public bool InCave => Level < 0;

        public bool Equals(Find other) =>
            Kind == other.Kind && Metal == other.Metal && X == other.X && Y == other.Y && Z == other.Z
            && Level == other.Level && HasCave == other.HasCave && CaveOrigin.Equals(other.CaveOrigin);

        public override bool Equals(object obj) => obj is Find other && Equals(other);

        public override int GetHashCode() => unchecked((int)Kind * 31 + X.GetHashCode() * 17 + Z.GetHashCode());
    }
}
