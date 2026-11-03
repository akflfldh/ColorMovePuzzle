using System;
using System.Collections.Generic;

namespace ColorMovePuzzle
{
    // Append new colors: existing serialized Black=4 and all 3x3 assets stay compatible.
    public enum TileColor { Empty, Red, Blue, Yellow, Black, Green, Purple }

    [Serializable]
    public struct PuzzleMove
    {
        public int from;
        public int to;
        public PuzzleMove(int from, int to) { this.from = from; this.to = to; }
    }

    // Pure data and rules. No Unity types, tile identities, or UI dependencies.
    public sealed class PuzzleBoard
    {
        readonly TileColor[] cells;
        public int Size { get; }
        public int Count => cells.Length;
        public TileColor this[int index] => cells[index];

        public PuzzleBoard(int size, IReadOnlyList<TileColor> initial)
        {
            if (size < 2 || initial == null || initial.Count != size * size)
                throw new ArgumentException("A board needs size * size cells and a size of at least two.");
            Size = size;
            cells = new TileColor[initial.Count];
            for (int i = 0; i < cells.Length; i++)
            {
                if ((int)initial[i] < 0 || (int)initial[i] > (int)TileColor.Purple)
                    throw new ArgumentException("Unknown tile color.");
                cells[i] = initial[i];
            }
        }

        public bool CanMove(int from, int to)
        {
            return from >= 0 && to >= 0 && from < Count && to < Count
                && cells[from] != TileColor.Empty && cells[to] == TileColor.Empty
                && Math.Abs(from / Size - to / Size) + Math.Abs(from % Size - to % Size) == 1;
        }

        public bool TryMove(int from, int to)
        {
            if (!CanMove(from, to)) return false;
            cells[to] = cells[from];
            cells[from] = TileColor.Empty;
            return true;
        }

        public int[] GetEmptyNeighbors(int index)
        {
            var result = new List<int>(4);
            if (index < 0 || index >= Count || cells[index] == TileColor.Empty) return result.ToArray();
            int[] candidates = { index - Size, index + Size, index - 1, index + 1 };
            foreach (int candidate in candidates)
                if (CanMove(index, candidate)) result.Add(candidate);
            return result.ToArray();
        }

        public TileColor[] CopyCells() => (TileColor[])cells.Clone();

        public bool Matches(IReadOnlyList<TileColor> target)
        {
            if (target == null || target.Count != Count) return false;
            for (int i = 0; i < Count; i++)
                if (cells[i] != target[i]) return false;
            return true;
        }
    }
}
