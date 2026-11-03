using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorMovePuzzle.Editor
{
    // Editor assembly only: no BFS code or distance table is included in a player build.
    public sealed class PuzzleDifficultyCalculator
    {
        public readonly struct Edge
        {
            public readonly int Key;
            public readonly PuzzleMove Move;
            public Edge(int key, int from, int to) { Key = key; Move = new PuzzleMove(from, to); }
        }
        struct Node { public int distance, parent; public PuzzleMove move; }
        readonly Dictionary<int, Node> nodes = new Dictionary<int, Node>();
        readonly int goal;
        public int Count => nodes.Count;
        public int MaximumDistance { get; private set; }
        public IEnumerable<int> Keys => nodes.Keys;

        public PuzzleDifficultyCalculator(TileColor[] target)
        {
            if (target == null || target.Length != 9) throw new ArgumentException("This offline calculator supports 3x3.");
            var counts = target.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());
            int[] inventory = {2, 2, 2, 2, 1};
            for (int c = 0; c < 5; c++)
                if (!counts.TryGetValue((TileColor)c, out int count) || count != inventory[c])
                    throw new ArgumentException("Expected 2E, 2R, 2B, 2Y and 1K.");
            goal = Encode(target);
            nodes.Add(goal, new Node {distance = 0, parent = goal});
            var queue = new Queue<int>();
            queue.Enqueue(goal);
            while (queue.Count > 0)
            {
                int state = queue.Dequeue();
                int nextDistance = nodes[state].distance + 1;
                foreach (var edge in Neighbors(state))
                {
                    if (nodes.ContainsKey(edge.Key)) continue;
                    nodes.Add(edge.Key, new Node {distance = nextDistance, parent = state, move = edge.Move});
                    MaximumDistance = Math.Max(MaximumDistance, nextDistance);
                    queue.Enqueue(edge.Key);
                }
            }
        }

        public int Distance(int key) => nodes.TryGetValue(key, out var node) ? node.distance : -1;
        public int Distance(TileColor[] cells) => Distance(Encode(cells));
        public PuzzleMove[] Solve(int key)
        {
            if (!nodes.ContainsKey(key)) throw new ArgumentException("Unreachable state.");
            var result = new List<PuzzleMove>();
            while (key != goal)
            {
                var node = nodes[key];
                result.Add(new PuzzleMove(node.move.to, node.move.from));
                key = node.parent;
            }
            return result.ToArray();
        }

        public static int Encode(TileColor[] cells)
        {
            if (cells == null || cells.Length != 9) throw new ArgumentException("Expected nine cells.");
            int result = 0;
            for (int i = 0; i < 9; i++)
            {
                if ((int)cells[i] < 0 || (int)cells[i] > 4) throw new ArgumentException("Unknown color.");
                result |= (int)cells[i] << (i * 3);
            }
            return result;
        }

        public static TileColor[] Decode(int key)
        {
            var result = new TileColor[9];
            for (int i = 0; i < 9; i++) result[i] = (TileColor)((key >> (i * 3)) & 7);
            return result;
        }

        public static IEnumerable<Edge> Neighbors(int state)
        {
            for (int to = 0; to < 9; to++)
            {
                if (((state >> (to * 3)) & 7) != 0) continue;
                foreach (int from in new[] {to - 3, to + 3, to - 1, to + 1})
                {
                    if (from < 0 || from >= 9 || Math.Abs(from / 3 - to / 3) + Math.Abs(from % 3 - to % 3) != 1) continue;
                    int color = (state >> (from * 3)) & 7;
                    if (color != 0) yield return new Edge(state ^ (color << (from * 3)) ^ (color << (to * 3)), from, to);
                }
            }
        }
    }
}
