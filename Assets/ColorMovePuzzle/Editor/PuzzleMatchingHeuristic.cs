using System;
using System.Collections.Generic;

namespace ColorMovePuzzle.Editor
{
    // Offline only. Minimum bipartite assignment, not labeled-tile or greedy Manhattan distance.
    public static class PuzzleMatchingHeuristic
    {
        public static int Evaluate(int size, TileColor[] cells, TileColor[] target)
        {
            new PuzzleBoard(size, cells);
            new PuzzleBoard(size, target);
            int tiles = 0, empty = 0;
            for (int color = 0; color <= (int)TileColor.Purple; color++)
            {
                var from = new List<int>(); var to = new List<int>();
                for (int i = 0; i < cells.Length; i++)
                { if ((int)cells[i] == color) from.Add(i); if ((int)target[i] == color) to.Add(i); }
                if (from.Count != to.Count || from.Count > 8) throw new ArgumentException("Mismatched inventory or unsupported assignment size.");
                int distance = Match(size, from, to);
                if (color == 0) empty = distance; else tiles += distance;
            }
            return Math.Max(tiles, empty);
        }

        static int Match(int size, List<int> from, List<int> to)
        {
            int count = from.Count, length = 1 << count;
            var dp = new int[length];
            for (int mask = 1; mask < length; mask++) dp[mask] = int.MaxValue;
            for (int mask = 0; mask < length; mask++)
            {
                int assigned = 0;
                for (int bits = mask; bits != 0; bits >>= 1) assigned += bits & 1;
                if (assigned == count) continue;
                for (int j = 0; j < count; j++) if ((mask & (1 << j)) == 0)
                {
                    int cost = Math.Abs(from[assigned] / size - to[j] / size) + Math.Abs(from[assigned] % size - to[j] % size);
                    int next = mask | (1 << j);
                    dp[next] = Math.Min(dp[next], dp[mask] + cost);
                }
            }
            return dp[length - 1];
        }
    }
}
