using System;
using System.Collections.Generic;

namespace MapOverhaul
{
    // One ore deposit: a group of surface ore rocks of one metal, at the mean position of its rocks.
    internal readonly struct Deposit
    {
        internal readonly Metal Metal;
        internal readonly float X;
        internal readonly float Z;
        internal readonly int Count;

        internal Deposit(Metal metal, float x, float z, int count)
        {
            Metal = metal;
            X = x;
            Z = z;
            Count = count;
        }
    }

    // Groups the surface ore rocks (a level of 0 or more) of one metal: two rocks less than 30 m apart are in one group,
    // also through a chain of rocks. A grid of 30 m cells limits each compare to the rocks of 9 cells.
    internal static class DepositGrouper
    {
        internal const float Distance = 30f;

        internal static List<Deposit> Group(IEnumerable<Find> finds)
        {
            var rocks = new List<Find>();
            foreach (var find in finds)
                if (find.Kind == FindKind.OreRock && !find.InCave) rocks.Add(find);

            var parent = new int[rocks.Count];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;
            var grid = new Dictionary<(Metal, int, int), List<int>>();
            for (int i = 0; i < rocks.Count; i++)
            {
                var (cx, cz) = Cell(rocks[i]);
                for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                        if (grid.TryGetValue((rocks[i].Metal, cx + dx, cz + dz), out var near))
                            foreach (int j in near)
                                if (Close(rocks[i], rocks[j])) Join(parent, i, j);
                var key = (rocks[i].Metal, cx, cz);
                if (!grid.TryGetValue(key, out var cell)) grid[key] = cell = new List<int>();
                cell.Add(i);
            }
            return Collect(rocks, parent);
        }

        private static (int, int) Cell(Find rock) =>
            ((int)Math.Floor(rock.X / Distance), (int)Math.Floor(rock.Z / Distance));

        private static bool Close(Find a, Find b)
        {
            float dx = a.X - b.X, dz = a.Z - b.Z;
            return dx * dx + dz * dz < Distance * Distance;
        }

        private static int Root(int[] parent, int i)
        {
            while (parent[i] != i) i = parent[i] = parent[parent[i]];
            return i;
        }

        private static void Join(int[] parent, int a, int b) => parent[Root(parent, a)] = Root(parent, b);

        private static List<Deposit> Collect(List<Find> rocks, int[] parent)
        {
            var sums = new Dictionary<int, (Metal Metal, float X, float Z, int Count)>();
            var order = new List<int>();
            for (int i = 0; i < rocks.Count; i++)
            {
                int root = Root(parent, i);
                if (!sums.TryGetValue(root, out var sum))
                {
                    order.Add(root);
                    sum = (rocks[i].Metal, 0f, 0f, 0);
                }
                sums[root] = (sum.Metal, sum.X + rocks[i].X, sum.Z + rocks[i].Z, sum.Count + 1);
            }
            var deposits = new List<Deposit>(order.Count);
            foreach (int root in order)
            {
                var sum = sums[root];
                deposits.Add(new Deposit(sum.Metal, sum.X / sum.Count, sum.Z / sum.Count, sum.Count));
            }
            return deposits;
        }
    }
}
