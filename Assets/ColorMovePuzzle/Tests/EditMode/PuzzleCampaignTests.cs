using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColorMovePuzzle.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ColorMovePuzzle.Tests
{
    public sealed class PuzzleDifficultyTests
    {
        PuzzleDifficultyCalculator bfs;
        PuzzleStageCatalog catalog;
        [OneTimeSetUp] public void Compute()
        {
            bfs = new PuzzleDifficultyCalculator(PuzzleCampaignGenerator.Target);
            catalog = AssetDatabase.LoadAssetAtPath<PuzzleStageCatalog>(PuzzleCampaignGenerator.CatalogPath);
            Assert.That(catalog, Is.Not.Null);
        }

        [Test] public void GraphContainsEveryDistinctMultisetPermutation()
        {
            // 9! / (2! * 2! * 2! * 2!) = 22,680; two empties and same-color tiles are indistinguishable.
            Assert.That(bfs.Count, Is.EqualTo(22680));
            Assert.That(bfs.MaximumDistance, Is.EqualTo(20));
            foreach (int key in bfs.Keys)
            {
                var cells = PuzzleDifficultyCalculator.Decode(key);
                CollectionAssert.AreEqual(new[] {2, 2, 2, 2, 1}, Enumerable.Range(0, 5).Select(c => cells.Count(x => (int)x == c)).ToArray());
            }
        }

        [Test] public void EveryDistanceSatisfiesShortestPathCertificateUsingIndependentBoardRules()
        {
            int zeroes = 0;
            foreach (int key in bfs.Keys)
            {
                int distance = bfs.Distance(key);
                var cells = PuzzleDifficultyCalculator.Decode(key);
                var board = new PuzzleBoard(3, cells);
                if (distance == 0) { zeroes++; Assert.That(board.Matches(PuzzleCampaignGenerator.Target), Is.True); }
                bool descendingNeighbor = distance == 0;
                // Generate edges with the production board rules, independently of the bit-packed BFS neighbor generator.
                for (int from = 0; from < 9; from++) foreach (int to in board.GetEmptyNeighbors(from))
                {
                    var copy = new PuzzleBoard(3, cells);
                    Assert.That(copy.TryMove(from, to), Is.True);
                    int neighborDistance = bfs.Distance(copy.CopyCells());
                    Assert.That(neighborDistance, Is.GreaterThanOrEqualTo(0));
                    Assert.That(Math.Abs(neighborDistance - distance), Is.LessThanOrEqualTo(1));
                    descendingNeighbor |= neighborDistance == distance - 1;
                }
                Assert.That(descendingNeighbor, Is.True);
            }
            Assert.That(zeroes, Is.EqualTo(1));
        }

        [Test] public void AllThirtyStagesHaveUniqueStartsAndExactOptimalSolutions()
        {
            Assert.That(catalog.Count, Is.EqualTo(30));
            var starts = new HashSet<int>();
            foreach (var stage in catalog.Stages)
            {
                var board = stage.CreateStartingBoard();
                int key = PuzzleDifficultyCalculator.Encode(board.CopyCells());
                Assert.That(starts.Add(key), Is.True);
                Assert.That(board.Matches(stage.Target), Is.False);
                Assert.That(stage.MinimumMoves, Is.EqualTo(bfs.Distance(key)));
                Assert.That(stage.GetSolution().Length, Is.EqualTo(stage.MinimumMoves));
                foreach (var move in stage.GetSolution()) Assert.That(board.TryMove(move.from, move.to), Is.True);
                Assert.That(board.Matches(stage.Target), Is.True);
            }
        }

        [Test] public void DifficultyIsNondecreasingAndLayoutsAreDiverse()
        {
            int previous = 0;
            var emptyPairs = new HashSet<string>();
            var blackCells = new HashSet<int>();
            for (int i = 0; i < 30; i++)
            {
                var stage = catalog.GetStage(i + 1);
                Assert.That(stage.StageNumber, Is.EqualTo(i + 1));
                Assert.That(stage.MinimumMoves, Is.EqualTo(2 + i * 18 / 29));
                Assert.That(stage.MinimumMoves, Is.GreaterThanOrEqualTo(previous));
                previous = stage.MinimumMoves;
                var start = stage.CreateStartingBoard().CopyCells();
                emptyPairs.Add(PuzzleCampaignGenerator.EmptyPair(start));
                blackCells.Add(Array.IndexOf(start, TileColor.Black));
            }
            Assert.That(emptyPairs.Count, Is.GreaterThanOrEqualTo(12));
            Assert.That(blackCells.Count, Is.GreaterThanOrEqualTo(5));
        }

        [Test] public void DifferentGoalHasItsOwnDistanceRoot()
        {
            var alternate = (TileColor[])PuzzleCampaignGenerator.Target.Clone();
            alternate[7] = alternate[4]; alternate[4] = TileColor.Empty;
            var other = new PuzzleDifficultyCalculator(alternate);
            Assert.That(other.Count, Is.EqualTo(22680));
            Assert.That(other.Distance(alternate), Is.Zero);
            Assert.That(other.Distance(PuzzleCampaignGenerator.Target), Is.EqualTo(1));
            Assert.That(bfs.Distance(alternate), Is.EqualTo(1));
        }
    }

    public sealed class PuzzleProgressTests
    {
        string path;
        [SetUp] public void Setup() => path = Path.Combine(Application.temporaryCachePath, "ColorMovePuzzleTests", Guid.NewGuid() + ".json");
        [TearDown] public void Cleanup()
        {
            foreach (string suffix in new[] {"", ".tmp", ".bak"}) if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }

        [Test] public void NewProgressUnlocksOnlyStageOne()
        {
            var p = new PuzzleProgress(30);
            Assert.That(p.IsUnlocked(1), Is.True);
            for (int i = 2; i <= 30; i++) Assert.That(p.IsUnlocked(i), Is.False);
            Assert.That(p.ClearedCount, Is.Zero);
        }

        [Test] public void ClearingUnlocksNextAndReplayNeverRelocksOrDuplicates()
        {
            var p = new PuzzleProgress(30);
            Assert.That(p.MarkCleared(2), Is.False);
            Assert.That(p.Select(2), Is.False);
            Assert.That(p.MarkCleared(1), Is.True);
            Assert.That(p.IsUnlocked(2), Is.True);
            Assert.That(p.MarkCleared(1), Is.False);
            Assert.That(p.ClearedCount, Is.EqualTo(1));
            Assert.That(p.IsUnlocked(1), Is.True);
            Assert.That(p.IsUnlocked(3), Is.False);
        }

        [Test] public void CompletionStopsAtThirty()
        {
            var p = new PuzzleProgress(30);
            for (int i = 1; i <= 30; i++) Assert.That(p.MarkCleared(i), Is.True);
            Assert.That(p.AllCleared, Is.True);
            Assert.That(p.NextStage, Is.EqualTo(30));
            Assert.That(p.IsUnlocked(31), Is.False);
            Assert.That(p.MarkCleared(31), Is.False);
        }

        [Test] public void FileRoundTripRestoresUnlocksClearsAndLastPlayed()
        {
            var p = new PuzzleProgress(30);
            p.MarkCleared(1); p.MarkCleared(2); p.Select(3);
            Assert.That(new PuzzleProgressStore(path, 30).Save(p), Is.True);
            var loaded = new PuzzleProgressStore(path, 30).Load();
            Assert.That(loaded.ClearedCount, Is.EqualTo(2));
            Assert.That(loaded.LastPlayed, Is.EqualTo(3));
            Assert.That(loaded.IsUnlocked(3), Is.True);
            Assert.That(loaded.IsUnlocked(4), Is.False);
        }

        [Test] public void InvalidPrimaryRecoversFromLastGoodBackup()
        {
            var store = new PuzzleProgressStore(path, 30);
            var p = new PuzzleProgress(30);
            p.MarkCleared(1); Assert.That(store.Save(p), Is.True);
            p.MarkCleared(2); Assert.That(store.Save(p), Is.True);
            File.WriteAllText(path, "{broken");
            Assert.That(new PuzzleProgressStore(path, 30).Load().ClearedCount, Is.EqualTo(1));
        }

        [Test] public void MissingOrMalformedSaveStartsSafely()
        {
            var store = new PuzzleProgressStore(path, 30);
            Assert.That(store.Load().ClearedCount, Is.Zero);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "{not json}");
            Assert.That(store.Load().NextStage, Is.EqualTo(1));
            File.WriteAllText(path, "{\"version\":99,\"clearedStages\":[1,2]}");
            Assert.That(store.Load().ClearedCount, Is.Zero);
        }

        [Test] public void UntrustedProgressCannotSkipLockedStages()
        {
            var p = PuzzleProgress.Restore(30, new[] {-1, 1, 1, 2, 4, 99}, 30);
            Assert.That(p.ClearedCount, Is.EqualTo(2));
            Assert.That(p.IsUnlocked(3), Is.True);
            Assert.That(p.IsUnlocked(4), Is.False);
            Assert.That(p.LastPlayed, Is.EqualTo(1));
        }
    }
}
