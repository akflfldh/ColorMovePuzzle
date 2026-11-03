using System;
using System.Linq;
using ColorMovePuzzle.Editor;
using NUnit.Framework;
using UnityEditor;

namespace ColorMovePuzzle.Tests
{
    public sealed class PuzzleFourTests
    {
        [Test] public void AllThirtyFourByFourStagesAreUniqueSolvableAndHonestlyRated()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PuzzleStageCatalog>(PuzzleFourByFourGenerator.CatalogPath);
            Assert.That(catalog.Count, Is.EqualTo(30));
            var starts = new System.Collections.Generic.HashSet<ulong>();
            int previous = 0;
            foreach (var stage in catalog.Stages)
            {
                var board = stage.CreateStartingBoard();
                var cells = board.CopyCells();
                Assert.That(stage.BoardSize, Is.EqualTo(4));
                CollectionAssert.AreEqual(new[] {2,3,3,3,2,3}, Enumerable.Range(0, 6).Select(c => cells.Count(x => (int)x == c)).ToArray());
                Assert.That(starts.Add(PuzzleFourByFourGenerator.Encode(cells)), Is.True);
                Assert.That(board.Matches(stage.Target), Is.False);
                Assert.That(stage.MinimumMoves, Is.Zero, "A shuffle/path length must never be labeled as an exact minimum.");
                Assert.That(stage.HasExactMinimum, Is.False);
                Assert.That(stage.CardDifficulty, Does.StartWith("RATING"));
                Assert.That(stage.BoardDifficulty, Does.StartWith("ESTIMATED"));
                Assert.That(stage.LowerBound, Is.EqualTo(PuzzleMatchingHeuristic.Evaluate(4, cells, stage.Target)));
                Assert.That(stage.EstimatedRating, Is.GreaterThanOrEqualTo(previous));
                previous = stage.EstimatedRating;
                Assert.That(stage.KnownSolutionLength, Is.InRange(stage.LowerBound, 96));
                foreach (var move in stage.GetSolution()) Assert.That(board.TryMove(move.from, move.to), Is.True);
                Assert.That(board.Matches(stage.Target), Is.True);
                var fromGoal = new PuzzleBoard(4, stage.Target);
                foreach (var move in stage.Scramble) Assert.That(fromGoal.TryMove(move.from, move.to), Is.True);
                CollectionAssert.AreEqual(cells, fromGoal.CopyCells());
            }
            Assert.That(catalog.GetStage(30).EstimatedRating, Is.GreaterThanOrEqualTo(28));
        }

        [TestCase(10,14,true)] [TestCase(11,15,true)] [TestCase(13,14,true)]
        [TestCase(10,15,false)] [TestCase(0,15,false)] [TestCase(15,14,false)]
        public void FourByFourUsesTheSameOrthogonalRules(int from, int to, bool expected)
            => Assert.That(new PuzzleBoard(4, PuzzleFourByFourGenerator.Target).TryMove(from,to), Is.EqualTo(expected));

        [Test] public void FourByFourRejectsRowWrappingAndGreenHasNoIdentity()
        {
            var goal = (TileColor[])PuzzleFourByFourGenerator.Target.Clone();
            goal[4] = TileColor.Empty;
            Assert.That(new PuzzleBoard(4, goal).TryMove(3,4), Is.False);
            goal = (TileColor[])PuzzleFourByFourGenerator.Target.Clone();
            var first = goal[9]; goal[9] = goal[11]; goal[11] = first;
            Assert.That(new PuzzleBoard(4, goal).Matches(PuzzleFourByFourGenerator.Target), Is.True);
            Assert.That((int)TileColor.Black, Is.EqualTo(4), "Existing asset color IDs must stay stable.");
            Assert.That(PuzzlePalette.Tile(TileColor.Green).a, Is.EqualTo(1));
            Assert.That(PuzzlePalette.Symbol(TileColor.Green), Is.EqualTo("G"));
        }

        [Test] public void MatchingHeuristicNeverExceedsKnownExactThreeByThreeDistances()
        {
            var bfs = new PuzzleDifficultyCalculator(PuzzleCampaignGenerator.Target);
            int i = 0;
            foreach (int key in bfs.Keys)
                if (i++ % 17 == 0)
                    Assert.That(PuzzleMatchingHeuristic.Evaluate(3, PuzzleDifficultyCalculator.Decode(key), PuzzleCampaignGenerator.Target), Is.LessThanOrEqualTo(bfs.Distance(key)));
        }

        [Test] public void MatchingIsOptimalForIndistinguishableCopiesAndConsistentAcrossMoves()
        {
            var board = new PuzzleBoard(4, PuzzleFourByFourGenerator.Target);
            var random = new Random(409);
            for (int sample = 0; sample < 250; sample++)
            {
                int from = random.Next(16);
                var choices = board.GetEmptyNeighbors(from);
                if (choices.Length == 0) continue;
                int before = PuzzleMatchingHeuristic.Evaluate(4, board.CopyCells(), PuzzleFourByFourGenerator.Target);
                board.TryMove(from, choices[random.Next(choices.Length)]);
                var cells = board.CopyCells();
                int after = PuzzleMatchingHeuristic.Evaluate(4, cells, PuzzleFourByFourGenerator.Target);
                Assert.That(Math.Abs(before - after), Is.LessThanOrEqualTo(1));
                int colored = 0, empty = 0;
                for (int color = 0; color < 6; color++)
                {
                    int[] sources = Enumerable.Range(0,16).Where(p => (int)cells[p] == color).ToArray();
                    int[] targets = Enumerable.Range(0,16).Where(p => (int)PuzzleFourByFourGenerator.Target[p] == color).ToArray();
                    int cost = BruteMatch(sources, targets, 0, 0);
                    if (color == 0) empty = cost; else colored += cost;
                }
                Assert.That(after, Is.EqualTo(Math.Max(colored, empty)));
            }
        }
        static int BruteMatch(int[] from, int[] to, int i, int used)
        {
            if (i == from.Length) return 0;
            int best = int.MaxValue;
            for (int j = 0; j < to.Length; j++) if ((used & (1 << j)) == 0)
                best = Math.Min(best, Math.Abs(from[i]/4-to[j]/4)+Math.Abs(from[i]%4-to[j]%4)+BruteMatch(from,to,i+1,used | (1 << j)));
            return best;
        }
    }
}
