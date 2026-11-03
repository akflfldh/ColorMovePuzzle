using System.IO;
using TMPro;
using UnityEngine;

namespace ColorMovePuzzle
{
    public sealed class PuzzleCampaignController : MonoBehaviour
    {
        [SerializeField] PuzzleStageCatalog catalog;
        [SerializeField] PuzzleGameController game;
        [SerializeField] PuzzleStageSelectionView selection;
        [SerializeField] GameObject gameplayContent;
        [SerializeField] UnityEngine.UI.Button backButton;
        [SerializeField] UnityEngine.UI.Button nextButton;
        [SerializeField] UnityEngine.UI.Button clearBackButton;
        [SerializeField] TMP_Text clearHeading;
        [SerializeField] TMP_Text clearTitle;
        [SerializeField] TMP_Text minimumLabel;
        [SerializeField] bool startAtSelection = true;
        PuzzleProgressStore store;
        PuzzleSessionStore sessionStore;
        bool sessionActive;
        bool sessionDirty;
        bool pendingClear;
        string reportedSessionError;
        public string SessionPath => sessionStore?.FilePath;
        public string SessionError => sessionStore?.LastError;
        public int SessionWriteCount => sessionStore?.WriteCount ?? 0;
        readonly System.Collections.Generic.Dictionary<int, PuzzleProgressStore> modeStores = new System.Collections.Generic.Dictionary<int, PuzzleProgressStore>();
        string progressDirectory;
        public event System.Action Ready;
        public event System.Action SelectionShown;
        public event System.Action<PuzzleStageData> StageStarted;
        public event System.Action<string> PlayCompleted;
        public System.Func<System.Action, bool> NextStageTransition { get; set; }
        public System.Func<bool> NavigationBlocked { get; set; }
        public bool IsRestoring { get; private set; }
        public bool IsTransitionPending { get; private set; }
        public string CurrentRunId { get; private set; }
        bool completionSent;
        public bool IsReady { get; private set; }
        public PuzzleProgress Progress { get; private set; }
        public PuzzleStageCatalog Catalog => catalog;
        public PuzzleStageSelectionView Selection => selection;
        public bool IsSelecting => selection.gameObject.activeSelf;
        public string SavePath => store.FilePath;
        public bool HasSavedHistory => Progress != null && (Progress.ClearedCount > 0
            || (File.Exists(SavePath) && string.IsNullOrEmpty(store.LastError)));
        public void SetMenuManagedStartup() => startAtSelection = false;

        public bool CanPresent(PuzzleStageCatalog stages)
            => stages && stages.Count == selection.Buttons.Count && game.SupportsSize(stages.GetStage(1).BoardSize);

        public bool CanPlayMode(PuzzleModeDefinition mode, int stage)
        {
            if (mode == null || !mode.Available || !CanPresent(mode.Stages)) return false;
            var progress = mode.Stages == catalog ? Progress : CreateModeStore(mode).Load();
            return progress != null && progress.IsUnlocked(stage);
        }

        public bool OpenMode(PuzzleModeDefinition mode)
        {
            if (IsTransitionPending || NavigationBlocked?.Invoke() == true) return false;
            if (mode == null || !mode.Available || !CanPresent(mode.Stages)) return false;
            if (mode.Stages != catalog)
            {
                FlushSession();
                sessionActive = false;
                catalog = mode.Stages;
                UseProgressStore(CreateModeStore(mode));
                selection.Initialize(catalog, number => PlayStage(number));
            }
            return true;
        }

        PuzzleProgressStore CreateModeStore(PuzzleModeDefinition mode)
        {
            if (modeStores.TryGetValue(mode.GridSize, out var existing)) return existing;
            var result = new PuzzleProgressStore(Path.Combine(progressDirectory ?? Path.Combine(Application.persistentDataPath, "ColorMovePuzzle"), mode.ProgressFileName), mode.Stages.Count);
            modeStores[mode.GridSize] = result;
            return result;
        }
        public void UseProgressDirectory(string directory)
        {
            progressDirectory = Path.GetFullPath(directory);
            modeStores.Clear();
            int size = catalog.GetStage(1).BoardSize;
            UseProgressStore(new PuzzleProgressStore(Path.Combine(progressDirectory,
                size == 3 ? "progress-v1.json" : $"progress-{size}x{size}-v1.json"), catalog.Count));
        }

        public void Configure(PuzzleStageCatalog stages, PuzzleGameController controller,
            PuzzleStageSelectionView stageSelection, GameObject content,
            UnityEngine.UI.Button back, UnityEngine.UI.Button next, UnityEngine.UI.Button clearBack,
            TMP_Text heading, TMP_Text title, TMP_Text minimum)
        {
            catalog = stages; game = controller; selection = stageSelection; gameplayContent = content;
            backButton = back; nextButton = next; clearBackButton = clearBack;
            clearHeading = heading; clearTitle = title; minimumLabel = minimum;
        }

        void Start()
        {
            if (store == null) UseProgressStore(new PuzzleProgressStore(
                Path.Combine(Application.persistentDataPath, "ColorMovePuzzle", "progress-v1.json"), catalog.Count));
            selection.Initialize(catalog, number => PlayStage(number));
            backButton.onClick.AddListener(ShowSelection);
            clearBackButton.onClick.AddListener(ShowSelection);
            nextButton.onClick.AddListener(PlayNext);
            game.StageCleared += OnCleared;
            game.StateCommitted += OnStateCommitted;
            IsReady = true;
            if (startAtSelection) ShowSelection();
            Ready?.Invoke();
        }

        public void UseProgressStore(PuzzleProgressStore storage)
        {
            store = storage;
            sessionStore = new PuzzleSessionStore(storage.FilePath + ".session-v1.json");
            sessionActive = sessionDirty = pendingClear = false;
            modeStores[catalog.GetStage(1).BoardSize] = storage;
            Progress = store.Load();
        }

        public bool PlayStage(int number, bool resume = false)
        {
            if (IsTransitionPending || NavigationBlocked?.Invoke() == true) return false;
            if (Progress == null || !Progress.IsUnlocked(number)) return false;
            FlushSession();
            sessionActive = true;
            sessionDirty = pendingClear = false;
            selection.gameObject.SetActive(false);
            gameplayContent.SetActive(true);
            Progress.Select(number);
            store.Save(Progress);
            var stage = catalog.GetStage(number);
            CurrentRunId = System.Guid.NewGuid().ToString("N");
            completionSent = false;
            IsRestoring = resume;
            try
            {
                if (resume && sessionStore.TryLoad(stage, out var saved)) game.RestoreStage(stage, saved);
                else game.LoadStage(stage);
            }
            finally { IsRestoring = false; }
            minimumLabel.text = game.Stage.BoardDifficulty;
            StageStarted?.Invoke(game.Stage);
            return true;
        }

        public void ShowSelection()
        {
            if (IsTransitionPending || NavigationBlocked?.Invoke() == true) return;
            game.Suspend();
            FlushSession();
            gameplayContent.SetActive(false);
            selection.gameObject.SetActive(true);
            selection.Refresh(Progress, store.LastError);
            selection.FocusStage(Progress.NextStage);
            SelectionShown?.Invoke();
        }

        public void HideScreens()
        {
            if (IsTransitionPending || NavigationBlocked?.Invoke() == true) return;
            game.Suspend();
            FlushSession();
            gameplayContent.SetActive(false);
            selection.gameObject.SetActive(false);
        }

        void OnCleared(PuzzleStageData stage, int moves)
        {
            // A standalone legacy/test stage can still be played without changing campaign progress.
            if (stage.StageNumber < 1 || stage.StageNumber > catalog.Count || catalog.GetStage(stage.StageNumber) != stage) return;
            Progress.MarkCleared(stage.StageNumber);
            pendingClear = true;
            sessionDirty = true;
            FlushSession();
            clearHeading.text = $"STAGE {stage.StageNumber:00} COMPLETE";
            clearTitle.text = Progress.AllCleared ? "All stages complete!" : "Perfect match!";
            bool hasNext = stage.StageNumber < catalog.Count;
            nextButton.gameObject.SetActive(hasNext);
            nextButton.interactable = hasNext && Progress.IsUnlocked(stage.StageNumber + 1);
            if (!IsRestoring && !completionSent)
            {
                completionSent = true;
                PlayCompleted?.Invoke(CurrentRunId);
            }
        }

        public void PlayNext()
        {
            if (IsTransitionPending || NavigationBlocked?.Invoke() == true || !game.IsCleared || game.Stage.StageNumber >= catalog.Count) return;
            int next = game.Stage.StageNumber + 1;
            var currentCatalog = catalog;
            IsTransitionPending = true;
            bool completed = false;
            System.Action proceed = () => {
                if (completed || !this) return; completed = true; IsTransitionPending = false;
                if (catalog == currentCatalog && game.IsCleared) PlayStage(next);
            };
            if (NextStageTransition?.Invoke(proceed) != true) proceed();
        }

        bool OwnsStage() => game && game.Stage && game.Stage.StageNumber >= 1
            && game.Stage.StageNumber <= catalog.Count && catalog.GetStage(game.Stage.StageNumber) == game.Stage;

        void OnStateCommitted()
        {
            if (!sessionActive || !OwnsStage()) return;
            if (game.MoveCount == 0)
            { CurrentRunId = System.Guid.NewGuid().ToString("N"); completionSent = false; }
            pendingClear = false;
            sessionDirty = true;
            FlushSession();
        }

        public void FlushSession()
        {
            if (!sessionActive || !sessionDirty || sessionStore == null || !OwnsStage()) return;
            bool saved;
            if (pendingClear)
            {
                // Keep the solved snapshot until completion/unlock data has reached disk.
                saved = store.Save(Progress) && sessionStore.Clear(game.Stage);
            }
            else saved = sessionStore.Save(game.Stage, game.Board, game.MoveCount);
            if (saved) { sessionDirty = false; reportedSessionError = null; }
            else
            {
                string error = sessionStore.LastError ?? store.LastError;
                if (reportedSessionError != error)
                    Debug.LogWarning("Puzzle autosave failed; will retry on pause/focus loss: " + error);
                reportedSessionError = error;
            }
        }

        void OnApplicationPause(bool paused) { if (paused) FlushSession(); }
        void OnApplicationFocus(bool focused) { if (!focused) FlushSession(); }
        void OnApplicationQuit() { FlushSession(); }

        void OnDestroy()
        {
            if (game) { game.StageCleared -= OnCleared; game.StateCommitted -= OnStateCommitted; }
            if (backButton) backButton.onClick.RemoveListener(ShowSelection);
            if (clearBackButton) clearBackButton.onClick.RemoveListener(ShowSelection);
            if (nextButton) nextButton.onClick.RemoveListener(PlayNext);
        }
    }
}
