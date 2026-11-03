using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ColorMovePuzzle
{
    public enum PuzzleScreen { MainMenu, StageSelection, Playing, Settings }

    public sealed class PuzzleMenuController : MonoBehaviour
    {
        [SerializeField] PuzzleCampaignController campaign;
        [SerializeField] PuzzleModeCatalog modes;
        [SerializeField] PuzzleMainMenuView mainMenu;
        [SerializeField] PuzzleSettingsView settings;
        [SerializeField] UnityEngine.UI.Button selectionBack;
        [SerializeField] PuzzleTargetZoom targetZoom;
        public void ConfigureTargetZoom(PuzzleTargetZoom zoom) { targetZoom = zoom; }
        PuzzleMenuProfileStore store;
        bool initialized;
        public event Action<float, float> VolumesChanged;
        public Func<bool> NavigationBlocked { get; set; }
        public PuzzleMenuProfile Profile { get; private set; }
        public PuzzleScreen Screen { get; private set; } = PuzzleScreen.MainMenu;
        public PuzzleMainMenuView MainMenu => mainMenu;
        public PuzzleSettingsView Settings => settings;
        public string ProfilePath => store.FilePath;
        public bool CanContinue => initialized && Profile.HasContinue && campaign.CanPlayMode(modes.Find(Profile.LastGridSize), Profile.LastStage);

        public void Configure(PuzzleCampaignController stages, PuzzleModeCatalog modeData,
            PuzzleMainMenuView menu, PuzzleSettingsView preferences, UnityEngine.UI.Button back)
        { campaign = stages; modes = modeData; mainMenu = menu; settings = preferences; selectionBack = back; }

        void Awake()
        {
            campaign.Ready += Initialize;
            campaign.SelectionShown += OnSelection;
            campaign.StageStarted += OnStage;
#if UNITY_ANDROID && !UNITY_EDITOR
            UnityEngine.Input.backButtonLeavesApp = false;
#endif
        }
        void Start() { if (campaign.IsReady) Initialize(); }
        void Initialize()
        {
            if (initialized) return;
            if (store == null) store = new PuzzleMenuProfileStore(Path.Combine(Application.persistentDataPath, "ColorMovePuzzle", "menu-profile-v1.json"));
            LoadProfile();
            mainMenu.Initialize(() => Continue(), OpenSettings, size => SelectMode(size));
            settings.Initialize(ChangeVolumes, ShowMainMenu);
            selectionBack.onClick.AddListener(ShowMainMenu);
            initialized = true;
            ShowMainMenu();
        }

        public void UseProfileStore(PuzzleMenuProfileStore storage)
        {
            store = storage;
            LoadProfile();
            if (initialized) Refresh();
        }
        void LoadProfile()
        {
            Profile = store.Load();
            // Old progress files were written only after entering a stage. Infer history without touching them.
            if (!store.HasAnyFile && campaign.IsReady && campaign.HasSavedHistory)
                Profile.RecordStage(campaign.Catalog.GetStage(1).BoardSize, campaign.Progress.LastPlayed);
        }

        public void ShowMainMenu()
        {
            if (NavigationBlocked?.Invoke() == true) return;
            if (targetZoom) targetZoom.Close();
            campaign.HideScreens();
            settings.gameObject.SetActive(false);
            mainMenu.gameObject.SetActive(true);
            Screen = PuzzleScreen.MainMenu;
            Refresh();
        }
        public bool SelectMode(int size)
        {
            if (NavigationBlocked?.Invoke() == true) return false;
            if (!initialized || !campaign.OpenMode(modes.Find(size))) return false;
            campaign.ShowSelection();
            return true;
        }
        public bool Continue()
        {
            if (NavigationBlocked?.Invoke() == true) return false;
            if (!CanContinue) return false;
            if (!campaign.OpenMode(modes.Find(Profile.LastGridSize))) return false;
            return campaign.PlayStage(Profile.LastStage, resume: true);
        }
        public void OpenSettings()
        {
            if (NavigationBlocked?.Invoke() == true) return;
            if (Screen != PuzzleScreen.MainMenu) return;
            mainMenu.gameObject.SetActive(false);
            settings.gameObject.SetActive(true);
            Screen = PuzzleScreen.Settings;
            settings.Refresh(Profile, store.LastError);
        }
        void OnSelection()
        {
            if (targetZoom) targetZoom.Close();
            mainMenu.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);
            Screen = PuzzleScreen.StageSelection;
        }
        void OnStage(PuzzleStageData stage)
        {
            if (targetZoom) targetZoom.Close();
            mainMenu.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);
            Screen = PuzzleScreen.Playing;
            if (!initialized) return;
            Profile.RecordStage(stage.BoardSize, stage.StageNumber);
            store.Save(Profile);
        }
        void ChangeVolumes(float bgm, float sfx)
        {
            Profile.SetVolumes(bgm, sfx);
            store.Save(Profile);
            settings.SetStorageError(store.LastError);
            VolumesChanged?.Invoke(Profile.BgmVolume, Profile.SfxVolume);
        }
        void Refresh()
        {
            mainMenu.Refresh(Profile, modes, CanContinue, mode => campaign.CanPresent(mode.Stages), store.LastError);
            settings.Refresh(Profile, store.LastError);
        }

        public bool HandleBack()
        {
            if (NavigationBlocked?.Invoke() == true) return true;
            if (!initialized) return false;
            if (targetZoom && targetZoom.IsOpen) { targetZoom.Close(); return true; }
            switch (Screen)
            {
                case PuzzleScreen.Settings: ShowMainMenu(); return true;
                case PuzzleScreen.StageSelection: ShowMainMenu(); return true;
                case PuzzleScreen.Playing: campaign.ShowSelection(); return true;
                default: return false;
            }
        }
        void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (HandleBack()) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Application.Quit();
#endif
        }
        void OnDestroy()
        {
            if (campaign) { campaign.Ready -= Initialize; campaign.SelectionShown -= OnSelection; campaign.StageStarted -= OnStage; }
            if (selectionBack) selectionBack.onClick.RemoveListener(ShowMainMenu);
        }
    }
}
