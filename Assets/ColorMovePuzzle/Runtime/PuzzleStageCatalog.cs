using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorMovePuzzle
{
    [CreateAssetMenu(menuName = "Color Move Puzzle/Stage Catalog")]
    public sealed class PuzzleStageCatalog : ScriptableObject
    {
        [SerializeField] PuzzleStageData[] stages;
        public int Count => stages == null ? 0 : stages.Length;
        public IReadOnlyList<PuzzleStageData> Stages => stages;
        public PuzzleStageData GetStage(int number)
        {
            if (number < 1 || number > Count) throw new ArgumentOutOfRangeException(nameof(number));
            return stages[number - 1];
        }

        public void Configure(PuzzleStageData[] data)
        {
            if (data == null || data.Length == 0) throw new ArgumentException("Empty catalog.");
            for (int i = 0; i < data.Length; i++)
                if (!data[i] || data[i].StageNumber != i + 1) throw new ArgumentException("Stage numbers must be consecutive.");
            stages = (PuzzleStageData[])data.Clone();
        }
    }
}
