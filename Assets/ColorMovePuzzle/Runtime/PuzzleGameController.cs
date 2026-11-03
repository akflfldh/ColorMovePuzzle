using System.Collections;
using TMPro;
using UnityEngine;

namespace ColorMovePuzzle
{
    public sealed class PuzzleGameController : MonoBehaviour
    {
        [SerializeField] PuzzleStageData stage;
        [SerializeField] PuzzleBoardView boardView;
        [SerializeField] PuzzleBoardView targetView;
        [SerializeField] PuzzleBoardView[] boardViews;
        [SerializeField] PuzzleBoardView[] targetViews;
        [SerializeField] TMP_Text stageLabel;
        [SerializeField] TMP_Text moveLabel;
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] GameObject clearPanel;
        [SerializeField] TMP_Text clearMovesLabel;
        [SerializeField] UnityEngine.UI.Button restartButton;
        [SerializeField] UnityEngine.UI.Button clearRestartButton;
        [SerializeField, Range(0.12f, 0.18f)] float moveDuration = 0.15f;
        TileColor[] target;
        public event System.Action<PuzzleStageData, int> StageCleared;
        public event System.Action StateCommitted;
        public bool InputEnabled { get; private set; } = true;

        public PuzzleBoard Board { get; private set; }
        public PuzzleStageData Stage => stage;
        public PuzzleBoardView BoardView => boardView;
        public PuzzleBoardView TargetView => targetView;
        public void ConfigureViewSet(PuzzleBoardView[] interactive, PuzzleBoardView[] previews)
        { boardViews = interactive; targetViews = previews; }
        public bool SupportsSize(int size)
        {
            if (boardViews != null && targetViews != null && boardViews.Length > 0 && targetViews.Length > 0)
            {
                bool board = false, preview = false;
                foreach (var view in boardViews) board |= view && view.Size == size;
                foreach (var view in targetViews) preview |= view && view.Size == size;
                return board && preview;
            }
            return boardView.Size == size && targetView.Size == size;
        }
        void SelectViews(int size)
        {
            if (!SupportsSize(size)) throw new System.ArgumentException("No registered UI for this board size.");
            if (boardViews == null || targetViews == null || boardViews.Length == 0 || targetViews.Length == 0) return;
            foreach (var view in boardViews)
            { view.gameObject.SetActive(view.Size == size); if (view.Size == size) boardView = view; }
            foreach (var view in targetViews)
            { view.gameObject.SetActive(view.Size == size); if (view.Size == size) targetView = view; }
        }
        public int MoveCount { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsCleared { get; private set; }
        public float MoveDuration => moveDuration;
        public void LoadStage(PuzzleStageData data)
        {
            if (!data) throw new System.ArgumentNullException(nameof(data));
            StopAllCoroutines();
            SelectViews(data.BoardSize);
            stage = data;
            InputEnabled = true;
            Restart();
        }

        public void Suspend()
        {
            StopAllCoroutines();
            IsMoving = false;
            EvaluateClear();
            InputEnabled = false;
            clearPanel.SetActive(false);
        }

        public void Configure(PuzzleStageData data, PuzzleBoardView board, PuzzleBoardView preview,
            TMP_Text stageText, TMP_Text movesText, TMP_Text statusText, GameObject panel,
            TMP_Text resultText, UnityEngine.UI.Button restart, UnityEngine.UI.Button clearRestart)
        {
            stage = data; boardView = board; targetView = preview;
            stageLabel = stageText; moveLabel = movesText; statusLabel = statusText;
            clearPanel = panel; clearMovesLabel = resultText;
            restartButton = restart; clearRestartButton = clearRestart;
        }

        void Start()
        {
            restartButton.onClick.AddListener(Restart);
            clearRestartButton.onClick.AddListener(Restart);
            if (Board == null) Restart();
        }

        void OnDestroy()
        {
            if (restartButton) restartButton.onClick.RemoveListener(Restart);
            if (clearRestartButton) clearRestartButton.onClick.RemoveListener(Restart);
        }

        public void Restart()
        {
            ApplyState(stage, stage.CreateStartingBoard().CopyCells(), 0);
            StateCommitted?.Invoke();
        }

        public void RestoreStage(PuzzleStageData data, PuzzleSessionState saved)
        {
            if (!data || saved == null) throw new System.ArgumentNullException();
            InputEnabled = true;
            ApplyState(data, saved.Cells, saved.MoveCount);
            EvaluateClear();
        }

        void ApplyState(PuzzleStageData data, TileColor[] cells, int moves)
        {
            StopAllCoroutines();
            stage = data;
            SelectViews(stage.BoardSize);
            IsMoving = false;
            IsCleared = false;
            MoveCount = moves;
            Board = new PuzzleBoard(stage.BoardSize, cells);
            target = stage.Target;
            boardView.Repaint(Board, RequestTap, RequestSwipe);
            targetView.Repaint(new PuzzleBoard(stage.BoardSize, target));
            clearPanel.SetActive(false);
            stageLabel.text = $"STAGE {stage.StageNumber:00}";
            moveLabel.text = moves.ToString();
            statusLabel.text = "Tap to slide. Swipe to choose a direction.";
        }

        public void RequestTap(int index)
        {
            if (Board == null || !InputEnabled || IsMoving || IsCleared) return;
            int[] neighbors = Board.GetEmptyNeighbors(index);
            if (neighbors.Length == 1) TryMove(index, neighbors[0]);
            else if (neighbors.Length > 1) statusLabel.text = "Multiple open paths. Swipe toward an empty cell.";
        }

        public void RequestSwipe(int index, Vector2 delta)
        {
            if (Board == null || !InputEnabled || IsMoving || IsCleared || delta.sqrMagnitude < 1) return;
            float x = Mathf.Abs(delta.x), y = Mathf.Abs(delta.y);
            // Reject near-diagonal gestures rather than choosing an arbitrary direction.
            if (Mathf.Max(x, y) < Mathf.Min(x, y) * 1.25f) return;
            int to = x > y ? index + (delta.x > 0 ? 1 : -1)
                : index + (delta.y > 0 ? -Board.Size : Board.Size);
            TryMove(index, to); // CanMove also rejects row wrapping.
        }

        public bool TryMove(int from, int to)
        {
            if (Board == null || !InputEnabled || IsMoving || IsCleared || !Board.CanMove(from, to)) return false;
            var tile = boardView.FindTile(from);
            if (tile == null) return false;
            IsMoving = true;
            Board.TryMove(from, to);
            tile.SetCellIndex(to);
            MoveCount++;
            moveLabel.text = MoveCount.ToString();
            statusLabel.text = "Match every color and every empty cell.";
            // The logical move is final now; persist before the first animation frame can yield.
            StateCommitted?.Invoke();
            StartCoroutine(CompleteMove(tile, to));
            return true;
        }

        IEnumerator CompleteMove(PuzzleTileView tile, int to)
        {
            yield return tile.SlideTo(boardView.CellPosition(to), moveDuration);
            IsMoving = false;
            EvaluateClear();
        }

        void EvaluateClear()
        {
            if (Board == null || IsCleared || !Board.Matches(target)) return;
            IsCleared = true;
            statusLabel.text = "Pattern complete.";
            clearMovesLabel.text = $"You matched the pattern in {MoveCount} moves.";
            clearPanel.SetActive(true);
            StageCleared?.Invoke(stage, MoveCount);
        }
    }
}
