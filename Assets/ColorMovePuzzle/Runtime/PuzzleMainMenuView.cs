using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ColorMovePuzzle
{
    public sealed class PuzzleMainMenuView : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button continueButton;
        [SerializeField] UnityEngine.UI.Button settingsButton;
        [SerializeField] TMP_Text continueDetail;
        [SerializeField] TMP_Text storageStatus;
        [SerializeField] PuzzleModeButtonView[] modeButtons;
        Action resume, settings;
        public UnityEngine.UI.Button ContinueButton => continueButton;
        public UnityEngine.UI.Button SettingsButton => settingsButton;
        public IReadOnlyList<PuzzleModeButtonView> ModeButtons => modeButtons;

        public void Configure(UnityEngine.UI.Button recent, UnityEngine.UI.Button options, TMP_Text detail,
            TMP_Text storage, PuzzleModeButtonView[] cards)
        { continueButton = recent; settingsButton = options; continueDetail = detail; storageStatus = storage; modeButtons = cards; }
        public void Initialize(Action onContinue, Action onSettings, Action<int> onMode)
        {
            resume = onContinue; settings = onSettings;
            continueButton.onClick.AddListener(Continue);
            settingsButton.onClick.AddListener(Settings);
            foreach (var button in modeButtons) button.Initialize(onMode);
        }
        public void Refresh(PuzzleMenuProfile profile, PuzzleModeCatalog modes, bool canContinue,
            Func<PuzzleModeDefinition, bool> canPresent, string saveError)
        {
            continueButton.interactable = canContinue;
            continueDetail.text = canContinue ? $"{profile.LastGridSize} × {profile.LastGridSize}  /  STAGE {profile.LastStage:00}"
                : profile.HasContinue ? "RECENT PUZZLE IS UNAVAILABLE" : "SELECT A PUZZLE TO BEGIN";
            foreach (var button in modeButtons)
            {
                var mode = modes.Find(button.GridSize);
                button.Refresh(mode, mode != null && mode.Available && canPresent(mode));
            }
            storageStatus.text = string.IsNullOrEmpty(saveError) ? "TAKE YOUR TIME. FIND YOUR MATCH." : "CHECK DEVICE STORAGE TO KEEP YOUR SETTINGS";
        }
        void Continue() { if (continueButton.interactable) resume?.Invoke(); }
        void Settings() => settings?.Invoke();
        void OnDestroy()
        {
            if (continueButton) continueButton.onClick.RemoveListener(Continue);
            if (settingsButton) settingsButton.onClick.RemoveListener(Settings);
        }
    }
}
