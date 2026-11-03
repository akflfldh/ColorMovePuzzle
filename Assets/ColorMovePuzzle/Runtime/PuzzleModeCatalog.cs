using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorMovePuzzle
{
    [Serializable]
    public sealed class PuzzleModeDefinition
    {
        [SerializeField] int gridSize;
        [SerializeField] bool implemented;
        [SerializeField] PuzzleStageCatalog stages;
        [SerializeField] string progressFileName;
        public int GridSize => gridSize;
        public PuzzleStageCatalog Stages => stages;
        public string ProgressFileName => progressFileName;
        public bool Available => implemented && stages && stages.Count > 0 && stages.GetStage(1).BoardSize == gridSize;
        public PuzzleModeDefinition(int size, bool ready, PuzzleStageCatalog catalog, string saveName)
        { gridSize = size; implemented = ready; stages = catalog; progressFileName = saveName; }
    }

    [CreateAssetMenu(menuName = "Color Move Puzzle/Mode Catalog")]
    public sealed class PuzzleModeCatalog : ScriptableObject
    {
        [SerializeField] PuzzleModeDefinition[] modes;
        public IReadOnlyList<PuzzleModeDefinition> Modes => modes;
        public PuzzleModeDefinition Find(int gridSize)
        {
            if (modes != null) foreach (var mode in modes) if (mode.GridSize == gridSize) return mode;
            return null;
        }
        public void Configure(PuzzleModeDefinition[] definitions)
        {
            if (definitions == null || definitions.Length == 0) throw new ArgumentException("No modes.");
            var sizes = new HashSet<int>();
            foreach (var mode in definitions)
                if (mode == null || mode.GridSize < 2 || !sizes.Add(mode.GridSize)) throw new ArgumentException("Invalid/duplicate mode.");
            modes = (PuzzleModeDefinition[])definitions.Clone();
        }
    }
}
