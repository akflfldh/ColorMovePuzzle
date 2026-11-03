using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ColorMovePuzzle.Editor
{
    public static class PuzzleCampaignGenerator
    {
        public const string CatalogPath = "Assets/ColorMovePuzzle/Data/Campaign.asset";
        public static readonly TileColor[] Target = {TileColor.Red, TileColor.Red, TileColor.Blue,
            TileColor.Blue, TileColor.Yellow, TileColor.Yellow, TileColor.Black, TileColor.Empty, TileColor.Empty};

        [MenuItem("ColorMovePuzzle/Generate 30 Certified Stages")]
        public static PuzzleStageCatalog Generate()
        {
            var bfs = new PuzzleDifficultyCalculator(Target);
            if (bfs.Count != 22680) throw new InvalidOperationException("State graph is incomplete.");
            Directory.CreateDirectory("Assets/ColorMovePuzzle/Data/Campaign");
            AssetDatabase.Refresh();
            var selected = new List<TileColor[]>();
            var used = new HashSet<int>();
            var emptyPairs = new Dictionary<string, int>();
            var blackPositions = new int[9];
            var stages = new PuzzleStageData[30];
            var csv = new System.Text.StringBuilder("stage,minimum_moves,start,target,solution_length\n");
            for (int i = 0; i < stages.Length; i++)
            {
                int distance = 2 + i * (bfs.MaximumDistance - 2) / (stages.Length - 1);
                int chosen = -1, best = int.MinValue;
                foreach (int key in bfs.Keys.Where(k => bfs.Distance(k) == distance).OrderBy(k => k))
                {
                    if (used.Contains(key)) continue;
                    var cells = PuzzleDifficultyCalculator.Decode(key);
                    string pair = EmptyPair(cells);
                    int minDifference = selected.Count == 0 ? 9 : selected.Min(previous => Difference(previous, cells));
                    int recentDifference = selected.Count == 0 ? 9 : Difference(selected[selected.Count - 1], cells);
                    int pairUse = emptyPairs.TryGetValue(pair, out int value) ? value : 0;
                    int black = Array.IndexOf(cells, TileColor.Black);
                    int score = minDifference * 1000 + recentDifference * 30 - pairUse * 120
                        - blackPositions[black] * 8 + (int)((uint)key * 2654435761u % 17);
                    if (score > best) { best = score; chosen = key; }
                }
                if (chosen < 0) throw new InvalidOperationException("Not enough unique stage candidates.");
                used.Add(chosen);
                var start = PuzzleDifficultyCalculator.Decode(chosen);
                selected.Add(start);
                string signature = EmptyPair(start);
                emptyPairs[signature] = (emptyPairs.TryGetValue(signature, out int n) ? n : 0) + 1;
                blackPositions[Array.IndexOf(start, TileColor.Black)]++;
                string path = $"Assets/ColorMovePuzzle/Data/Campaign/Stage{i + 1:00}.asset";
                var stage = AssetDatabase.LoadAssetAtPath<PuzzleStageData>(path);
                if (!stage) { stage = ScriptableObject.CreateInstance<PuzzleStageData>(); AssetDatabase.CreateAsset(stage, path); }
                stage.ConfigureGenerated(i + 1, 3, start, Target, distance, bfs.Solve(chosen));
                EditorUtility.SetDirty(stage);
                stages[i] = stage;
                csv.AppendLine($"{i + 1},{distance},{Pattern(start)},{Pattern(Target)},{stage.GetSolution().Length}");
            }
            var catalog = AssetDatabase.LoadAssetAtPath<PuzzleStageCatalog>(CatalogPath);
            if (!catalog) { catalog = ScriptableObject.CreateInstance<PuzzleStageCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.Configure(stages);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Artifacts/StageSystem");
            File.WriteAllText("Artifacts/StageSystem/Stages.csv", csv.ToString());
            File.WriteAllText("Artifacts/StageSystem/BfsSummary.json", JsonUtility.ToJson(new Summary {
                reachableStates = bfs.Count, maximumDistance = bfs.MaximumDistance, stageCount = 30,
                distinctEmptyPairs = emptyPairs.Count, distinctBlackPositions = blackPositions.Count(n => n > 0),
                histogram = Enumerable.Range(0, bfs.MaximumDistance + 1).Select(d => bfs.Keys.Count(k => bfs.Distance(k) == d)).ToArray()}, true));
            Debug.Log($"Generated 30 unique stages from {bfs.Count} reachable states. Maximum distance: {bfs.MaximumDistance}.");
            return catalog;
        }

        public static string EmptyPair(TileColor[] cells) => string.Join("-", Enumerable.Range(0, cells.Length).Where(i => cells[i] == TileColor.Empty));
        static int Difference(TileColor[] a, TileColor[] b) => a.Where((color, i) => color != b[i]).Count();
        static string Pattern(TileColor[] cells) => string.Join("", cells.Select(c => c == TileColor.Empty ? "E" : PuzzlePalette.Symbol(c)));
        [Serializable] sealed class Summary
        {
            public int reachableStates, maximumDistance, stageCount, distinctEmptyPairs, distinctBlackPositions;
            public int[] histogram;
        }
    }
}
