using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ColorMovePuzzle.Editor
{
    public static class PuzzleFourByFourGenerator
    {
        public const string PrototypePath = "Assets/ColorMovePuzzle/Data/FourByFour/Stage01.asset";
        public const string CatalogPath = "Assets/ColorMovePuzzle/Data/FourByFour.asset";
        public static readonly TileColor[] Target = {
            TileColor.Red, TileColor.Red, TileColor.Red, TileColor.Blue,
            TileColor.Blue, TileColor.Blue, TileColor.Yellow, TileColor.Yellow,
            TileColor.Yellow, TileColor.Green, TileColor.Green, TileColor.Green,
            TileColor.Black, TileColor.Black, TileColor.Empty, TileColor.Empty};

        public static PuzzleStageData GeneratePrototype()
        {
            Directory.CreateDirectory("Assets/ColorMovePuzzle/Data/FourByFour");
            AssetDatabase.Refresh();
            var board = new PuzzleBoard(4, Target);
            var moves = new[] {new PuzzleMove(10,14), new PuzzleMove(6,10), new PuzzleMove(7,6), new PuzzleMove(11,7)};
            foreach (var move in moves) if (!board.TryMove(move.from, move.to)) throw new InvalidOperationException("Invalid prototype scramble.");
            var solution = Reverse(moves);
            var stage = AssetDatabase.LoadAssetAtPath<PuzzleStageData>(PrototypePath);
            if (!stage) { stage = ScriptableObject.CreateInstance<PuzzleStageData>(); AssetDatabase.CreateAsset(stage, PrototypePath); }
            int lower = PuzzleMatchingHeuristic.Evaluate(4, board.CopyCells(), Target);
            stage.ConfigureEstimated(1, 4, board.CopyCells(), Target, lower, lower, solution);
            EditorUtility.SetDirty(stage);
            AssetDatabase.SaveAssets();
            return stage;
        }

        internal static PuzzleMove[] Reverse(PuzzleMove[] moves)
        {
            var result = new PuzzleMove[moves.Length];
            for (int i = 0; i < result.Length; i++)
            { var move = moves[moves.Length - 1 - i]; result[i] = new PuzzleMove(move.to, move.from); }
            return result;
        }

        sealed class Candidate
        {
            public ulong key;
            public TileColor[] cells;
            public PuzzleMove[] solution;
            public int rating;
        }

        [MenuItem("ColorMovePuzzle/Generate 30 Estimated 4x4 Stages")]
        public static PuzzleStageCatalog GenerateCampaign()
        {
            var prototype = AssetDatabase.LoadAssetAtPath<PuzzleStageData>(PrototypePath);
            if (!prototype) throw new InvalidOperationException("Create and verify the single-stage prototype first.");
            const int maxWalks = 512, maxSteps = 96, maxCandidates = 6000;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var random = new System.Random(44004);
            var pool = new Dictionary<ulong, Candidate>();
            // Each candidate is generated along an actual legal, self-avoiding walk. No state-space BFS.
            for (int walk = 0; walk < maxWalks && pool.Count < maxCandidates; walk++)
            {
                var board = new PuzzleBoard(4, Target);
                var visited = new HashSet<ulong> {Encode(Target)};
                var moves = new List<PuzzleMove>();
                for (int step = 0; step < maxSteps && pool.Count < maxCandidates; step++)
                {
                    if (clock.ElapsedMilliseconds > 5000) throw new InvalidOperationException("Offline generation exceeded its five-second search budget.");
                    var options = new List<PuzzleMove>();
                    var cells = board.CopyCells();
                    for (int from = 0; from < 16; from++) foreach (int to in board.GetEmptyNeighbors(from))
                    {
                        Swap(cells, from, to);
                        bool fresh = !visited.Contains(Encode(cells));
                        Swap(cells, from, to);
                        if (fresh) options.Add(new PuzzleMove(from, to));
                    }
                    if (options.Count == 0) break;
                    var move = options[random.Next(options.Count)];
                    board.TryMove(move.from, move.to); moves.Add(move);
                    cells = board.CopyCells();
                    ulong key = Encode(cells); visited.Add(key);
                    if (moves.Count < 4 || (moves.Count != 4 && moves.Count % 3 != 0) || pool.ContainsKey(key)) continue;
                    int lower = PuzzleMatchingHeuristic.Evaluate(4, cells, Target);
                    if (lower > 0) pool.Add(key, new Candidate {key = key, cells = cells, rating = lower, solution = Reverse(moves.ToArray())});
                }
            }
            // Do not spend the rarest extreme state early and leave no unique candidate for stage 30.
            int maximum = pool.Values.GroupBy(c => c.rating).Where(g => g.Key <= 36 && g.Count() >= 8).Max(g => g.Key);
            long searchMilliseconds = clock.ElapsedMilliseconds;
            var ordered = pool.Values.OrderBy(c => c.key).ToArray();
            if (maximum < 28) throw new InvalidOperationException("Insufficient difficulty spread in the bounded pool.");
            var selected = new List<Candidate> {new Candidate {key = Encode(prototype.CreateStartingBoard().CopyCells()),
                cells = prototype.CreateStartingBoard().CopyCells(), rating = prototype.EstimatedRating, solution = prototype.GetSolution()}};
            var used = new HashSet<ulong> {selected[0].key};
            var stages = new PuzzleStageData[30]; stages[0] = prototype;
            for (int i = 1; i < 30; i++)
            {
                int desired = 4 + i * (maximum - 4) / 29;
                Candidate best = null;
                int bestScore = int.MinValue;
                foreach (var candidate in ordered)
                {
                    if (used.Contains(candidate.key) || candidate.rating > maximum || candidate.rating < Math.Max(desired, selected[i - 1].rating)) continue;
                    int difference = 16;
                    foreach (var previous in selected)
                    {
                        int changed = 0;
                        for (int p = 0; p < 16; p++) if (previous.cells[p] != candidate.cells[p]) changed++;
                        difference = Math.Min(difference, changed);
                    }
                    int score = -(candidate.rating - desired) * 100000 + difference * 12 - candidate.solution.Length * 4;
                    if (score > bestScore) { bestScore = score; best = candidate; }
                }
                if (best == null) throw new InvalidOperationException($"No distinct candidate for stage {i + 1}, requested rating {desired}, ceiling {maximum}.");
                selected.Add(best); used.Add(best.key);
                string path = $"Assets/ColorMovePuzzle/Data/FourByFour/Stage{i + 1:00}.asset";
                var stage = AssetDatabase.LoadAssetAtPath<PuzzleStageData>(path);
                if (!stage) { stage = ScriptableObject.CreateInstance<PuzzleStageData>(); AssetDatabase.CreateAsset(stage, path); }
                stage.ConfigureEstimated(i + 1, 4, best.cells, Target, best.rating, best.rating, best.solution);
                EditorUtility.SetDirty(stage); stages[i] = stage;
            }
            var catalog = AssetDatabase.LoadAssetAtPath<PuzzleStageCatalog>(CatalogPath);
            if (!catalog) { catalog = ScriptableObject.CreateInstance<PuzzleStageCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.Configure(stages); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Artifacts/FourByFour");
            var csv = new System.Text.StringBuilder("stage,estimated_rating,admissible_lower_bound,known_solution_upper_bound,start,target,exact_minimum_claimed\n");
            foreach (var stage in stages) csv.AppendLine($"{stage.StageNumber},{stage.EstimatedRating},{stage.LowerBound},{stage.KnownSolutionLength},{Pattern(stage.CreateStartingBoard().CopyCells())},{Pattern(stage.Target)},false");
            File.WriteAllText("Artifacts/FourByFour/Stages.csv", csv.ToString());
            File.WriteAllText("Artifacts/FourByFour/Generation.json", JsonUtility.ToJson(new Report {
                method = "bounded legal self-avoiding walks + per-color minimum assignment Manhattan",
                stages = 30, candidates = pool.Count, maximumRating = stages[29].EstimatedRating,
                maxWalks = maxWalks, maxSteps = maxSteps, maxCandidates = maxCandidates,
                milliseconds = clock.ElapsedMilliseconds, searchMilliseconds = searchMilliseconds, searchBudgetMilliseconds = 5000}, true));
            return catalog;
        }

        public static ulong Encode(TileColor[] cells)
        {
            if (cells.Length != 16) throw new ArgumentException("Expected 4x4 data.");
            ulong key = 0;
            for (int i = 0; i < 16; i++) key |= (ulong)cells[i] << (i * 3);
            return key;
        }
        static void Swap(TileColor[] cells, int a, int b) { var c = cells[a]; cells[a] = cells[b]; cells[b] = c; }
        static string Pattern(TileColor[] cells) => string.Join("", cells.Select(c => c == TileColor.Empty ? "E" : PuzzlePalette.Symbol(c)));
        [Serializable] sealed class Report
        {
            public string method;
            public int stages, candidates, maximumRating, maxWalks, maxSteps, maxCandidates, searchBudgetMilliseconds;
            public long milliseconds, searchMilliseconds;
        }
    }
}
