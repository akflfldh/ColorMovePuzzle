using System;
using UnityEngine;

namespace ColorMovePuzzle
{
    public enum PuzzleDifficultyKind { Exact, Estimated }
    [CreateAssetMenu(menuName = "Color Move Puzzle/Stage")]
    public sealed class PuzzleStageData : ScriptableObject
    {
        [SerializeField] int stageNumber = 1;
        [SerializeField, Min(2)] int boardSize = 3;
        [SerializeField] TileColor[] target;
        [SerializeField] PuzzleMove[] scramble;
        [SerializeField] TileColor[] start;
        [SerializeField, Min(0)] int minimumMoves;
        [SerializeField] PuzzleMove[] solution;
        [SerializeField] PuzzleDifficultyKind difficultyKind;
        [SerializeField] int lowerBound;
        [SerializeField] int estimatedRating;
        [SerializeField] int generationMoves;
        [SerializeField] string difficultyBand;
        [SerializeField] int evaluationNodes;
        [SerializeField] int evaluationResidual;
        public int GenerationMoves => generationMoves;
        public string DifficultyBand => difficultyBand;
        public int EvaluationNodes => evaluationNodes;
        public int EvaluationResidual => evaluationResidual;
        public void ConfigureGenerationMetrics(int moves, string band, int nodes, int residual)
        {
            if (moves < KnownSolutionLength || nodes < 0 || residual < 0 || string.IsNullOrEmpty(band))
                throw new ArgumentException("Invalid generation metrics.");
            generationMoves = moves; difficultyBand = band; evaluationNodes = nodes; evaluationResidual = residual;
        }

        public int StageNumber => stageNumber;
        public int BoardSize => boardSize;
        public TileColor[] Target => (TileColor[])target.Clone();
        public PuzzleMove[] Scramble => (PuzzleMove[])scramble.Clone();
        public int MinimumMoves => minimumMoves;
        public bool HasExactMinimum => difficultyKind == PuzzleDifficultyKind.Exact && minimumMoves > 0;
        public int LowerBound => HasExactMinimum ? minimumMoves : lowerBound;
        public int EstimatedRating => estimatedRating;
        public int KnownSolutionLength => solution != null && solution.Length > 0 ? solution.Length : scramble.Length;
        public string CardDifficulty => HasExactMinimum ? $"MIN. {minimumMoves:00} MOVES" : $"RATING {estimatedRating:00}";
        public string BoardDifficulty => HasExactMinimum ? $"MINIMUM {minimumMoves} MOVES" : $"ESTIMATED DIFFICULTY {estimatedRating}";

        public void Configure(int number, int size, TileColor[] goal, PuzzleMove[] moves)
        {
            stageNumber = number;
            boardSize = size;
            target = (TileColor[])goal.Clone();
            scramble = (PuzzleMove[])moves.Clone();
            start = null;
            solution = null;
            minimumMoves = 0;
            difficultyKind = PuzzleDifficultyKind.Exact; lowerBound = estimatedRating = 0;
            CreateStartingBoard(); // Reject invalid authoring data immediately.
        }

        public void ConfigureGenerated(int number, int size, TileColor[] initial, TileColor[] goal,
            int minimum, PuzzleMove[] shortestSolution)
        {
            if (minimum <= 0 || shortestSolution == null || shortestSolution.Length != minimum)
                throw new ArgumentException("Generated stage needs a distinct start and a positive certified distance.");
            ConfigureData(number, size, initial, goal, shortestSolution);
            minimumMoves = minimum; difficultyKind = PuzzleDifficultyKind.Exact;
            lowerBound = minimum; estimatedRating = minimum;
        }

        public void ConfigureEstimated(int number, int size, TileColor[] initial, TileColor[] goal,
            int matchingLowerBound, int rating, PuzzleMove[] knownSolution)
        {
            if (knownSolution == null || matchingLowerBound < 1 || matchingLowerBound > knownSolution.Length || rating < matchingLowerBound)
                throw new ArgumentException("Invalid estimated difficulty bounds.");
            ConfigureData(number, size, initial, goal, knownSolution);
            minimumMoves = 0; difficultyKind = PuzzleDifficultyKind.Estimated;
            lowerBound = matchingLowerBound; estimatedRating = rating;
        }

        void ConfigureData(int number, int size, TileColor[] initial, TileColor[] goal, PuzzleMove[] knownSolution)
        {
            var board = new PuzzleBoard(size, initial);
            if (board.Matches(goal)) throw new ArgumentException("Start must differ from goal.");
            var inventory = new int[(int)TileColor.Purple + 1];
            foreach (var color in initial) inventory[(int)color]++;
            foreach (var color in new PuzzleBoard(size, goal).CopyCells()) inventory[(int)color]--;
            foreach (int count in inventory) if (count != 0) throw new ArgumentException("Start/target inventories differ.");
            foreach (var move in knownSolution)
                if (!board.TryMove(move.from, move.to)) throw new ArgumentException("Invalid solution move.");
            if (!board.Matches(goal)) throw new ArgumentException("Solution does not reach target.");
            stageNumber = number; boardSize = size;
            start = (TileColor[])initial.Clone(); target = (TileColor[])goal.Clone();
            solution = (PuzzleMove[])knownSolution.Clone();
            scramble = new PuzzleMove[solution.Length];
            for (int i = 0; i < solution.Length; i++)
            {
                var move = solution[solution.Length - 1 - i];
                scramble[i] = new PuzzleMove(move.to, move.from);
            }
        }

        public PuzzleBoard CreateStartingBoard()
        {
            if (start != null && start.Length > 0) return new PuzzleBoard(boardSize, start);
            var board = new PuzzleBoard(boardSize, target);
            if (scramble == null) throw new InvalidOperationException("Stage has no scramble record.");
            foreach (var move in scramble)
                if (!board.TryMove(move.from, move.to))
                    throw new InvalidOperationException($"Invalid scramble step {move.from} -> {move.to}.");
            if (board.Matches(target)) throw new InvalidOperationException("Start must differ from target.");
            return board;
        }

        public PuzzleMove[] GetSolution()
        {
            if (solution != null && solution.Length > 0) return (PuzzleMove[])solution.Clone();
            var result = new PuzzleMove[scramble.Length];
            for (int i = 0; i < result.Length; i++)
            {
                var step = scramble[scramble.Length - 1 - i];
                result[i] = new PuzzleMove(step.to, step.from);
            }
            return result;
        }
    }
}
