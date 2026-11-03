using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace ColorMovePuzzle.Tests
{
    public sealed class PuzzleBoardTests
    {
        static readonly TileColor[] Goal = {TileColor.Red, TileColor.Red, TileColor.Blue,
            TileColor.Blue, TileColor.Yellow, TileColor.Yellow, TileColor.Black, TileColor.Empty, TileColor.Empty};

        [Test] public void StageHasExactGoalAndInventory()
        {
            var stage = Stage();
            Assert.That(stage.BoardSize, Is.EqualTo(3));
            CollectionAssert.AreEqual(Goal, stage.Target);
            var start = stage.CreateStartingBoard();
            Assert.That(start.Count, Is.EqualTo(9));
            Assert.That(start.Matches(stage.Target), Is.False);
            var colors = start.CopyCells();
            Assert.That(colors.Count(c => c == TileColor.Empty), Is.EqualTo(2));
            Assert.That(colors.Count(c => c == TileColor.Red), Is.EqualTo(2));
            Assert.That(colors.Count(c => c == TileColor.Blue), Is.EqualTo(2));
            Assert.That(colors.Count(c => c == TileColor.Yellow), Is.EqualTo(2));
            Assert.That(colors.Count(c => c == TileColor.Black), Is.EqualTo(1));
        }

        [Test] public void ScrambleAndInverseAreLegalAndReproducible()
        {
            var stage = Stage();
            var board = new PuzzleBoard(3, Goal);
            foreach (var move in stage.Scramble) Assert.That(board.TryMove(move.from, move.to), Is.True);
            CollectionAssert.AreEqual(stage.CreateStartingBoard().CopyCells(), board.CopyCells());
            foreach (var move in stage.GetSolution()) Assert.That(board.TryMove(move.from, move.to), Is.True);
            Assert.That(board.Matches(Goal), Is.True);
        }

        [TestCase(4, 7)] [TestCase(5, 8)] [TestCase(6, 7)]
        public void AdjacentTilesCanSlide(int from, int to)
        {
            var board = new PuzzleBoard(3, Goal);
            var color = board[from];
            Assert.That(board.TryMove(from, to), Is.True);
            Assert.That(board[from], Is.EqualTo(TileColor.Empty));
            Assert.That(board[to], Is.EqualTo(color));
        }

        [TestCase(4, 8)] // diagonal
        [TestCase(5, 7)] // diagonal
        [TestCase(0, 8)] // distant
        [TestCase(8, 7)] // empty source
        [TestCase(0, 1)] // occupied destination
        [TestCase(-1, 7)] [TestCase(0, 9)]
        public void InvalidMovesDoNotMutate(int from, int to)
        {
            var board = new PuzzleBoard(3, Goal);
            Assert.That(board.TryMove(from, to), Is.False);
            CollectionAssert.AreEqual(Goal, board.CopyCells());
        }

        [Test] public void LinearNeighborsCannotWrapRows()
        {
            var cells = (TileColor[])Goal.Clone();
            cells[3] = TileColor.Empty;
            var board = new PuzzleBoard(3, cells);
            Assert.That(board.TryMove(2, 3), Is.False);
        }

        [Test] public void TwoEmptyNeighborsAreBothReturned()
        {
            var board = Stage().CreateStartingBoard();
            Assert.That(board.TryMove(4, 5), Is.True);
            CollectionAssert.AreEquivalent(new[] {4, 6}, board.GetEmptyNeighbors(3));
        }

        [Test] public void SameColorTilesAndEmptyCellsHaveNoIdentity()
        {
            var same = (TileColor[])Goal.Clone();
            var first = same[0]; same[0] = same[1]; same[1] = first;
            var empty = same[7]; same[7] = same[8]; same[8] = empty;
            Assert.That(new PuzzleBoard(3, same).Matches(Goal), Is.True);
            same[0] = TileColor.Blue;
            Assert.That(new PuzzleBoard(3, same).Matches(Goal), Is.False);
        }

        [Test] public void ConstructorAndSnapshotsDoNotLeakMutableArrays()
        {
            var source = (TileColor[])Goal.Clone();
            var board = new PuzzleBoard(3, source);
            source[0] = TileColor.Black;
            var snapshot = board.CopyCells(); snapshot[1] = TileColor.Empty;
            Assert.That(board.Matches(Goal), Is.True);
        }

        [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void RulesUseDataDrivenSize(int size)
        {
            var cells = Enumerable.Repeat(TileColor.Red, size * size).ToArray();
            cells[cells.Length - 1] = TileColor.Empty;
            var board = new PuzzleBoard(size, cells);
            Assert.That(board.TryMove(cells.Length - size - 1, cells.Length - 1), Is.True);
        }

        [Test] public void FiveThousandLegalMovesPreserveAllColorCounts()
        {
            var board = Stage().CreateStartingBoard();
            var inventory = board.CopyCells().OrderBy(c => c).ToArray();
            var random = new Random(71);
            for (int i = 0; i < 5000; i++)
            {
                int from = random.Next(board.Count);
                var choices = board.GetEmptyNeighbors(from);
                if (choices.Length > 0) Assert.That(board.TryMove(from, choices[random.Next(choices.Length)]), Is.True);
            }
            CollectionAssert.AreEqual(inventory, board.CopyCells().OrderBy(c => c).ToArray());
        }

        [Test] public void InvalidBoardDataIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new PuzzleBoard(1, new[] {TileColor.Red}));
            Assert.Throws<ArgumentException>(() => new PuzzleBoard(3, new TileColor[8]));
        }

        static PuzzleStageData Stage()
        {
            var stage = AssetDatabase.LoadAssetAtPath<PuzzleStageData>("Assets/ColorMovePuzzle/Data/Stage01.asset");
            Assert.That(stage, Is.Not.Null);
            return stage;
        }
    }
}
