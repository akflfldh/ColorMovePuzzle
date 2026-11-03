using System;
using System.Collections.Generic;

namespace ColorMovePuzzle
{
    // Progress is independent of Unity and storage. Stable stage numbers are one-based.
    public sealed class PuzzleProgress
    {
        readonly bool[] cleared;
        public int StageCount => cleared.Length;
        public int LastPlayed { get; private set; } = 1;
        public int ClearedCount { get; private set; }
        public bool AllCleared => ClearedCount == StageCount;
        public int NextStage => Math.Min(StageCount, ClearedCount + 1);

        public PuzzleProgress(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            cleared = new bool[count];
        }

        public bool IsCleared(int number) => number >= 1 && number <= StageCount && cleared[number - 1];
        public bool IsUnlocked(int number) => number >= 1 && number <= StageCount && number <= ClearedCount + 1;

        public bool MarkCleared(int number)
        {
            if (!IsUnlocked(number) || IsCleared(number)) return false;
            cleared[number - 1] = true;
            ClearedCount++;
            return true;
        }

        public bool Select(int number)
        {
            if (!IsUnlocked(number)) return false;
            LastPlayed = number;
            return true;
        }

        public int[] CompletedStages()
        {
            var result = new int[ClearedCount];
            for (int i = 0; i < result.Length; i++) result[i] = i + 1;
            return result;
        }

        public static PuzzleProgress Restore(int count, IEnumerable<int> completed, int lastPlayed)
        {
            var result = new PuzzleProgress(count);
            // Never trust a saved unlock count. Restore only a contiguous, legal prefix.
            var stages = new HashSet<int>(completed ?? Array.Empty<int>());
            for (int i = 1; i <= count && stages.Contains(i); i++) result.MarkCleared(i);
            result.Select(lastPlayed);
            return result;
        }
    }
}
