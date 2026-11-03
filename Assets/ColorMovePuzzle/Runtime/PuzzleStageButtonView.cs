using System;
using TMPro;
using UnityEngine;

namespace ColorMovePuzzle
{
    public sealed class PuzzleStageButtonView : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button button;
        [SerializeField] UnityEngine.UI.Image background;
        [SerializeField] TMP_Text numberLabel;
        [SerializeField] TMP_Text stateLabel;
        [SerializeField] TMP_Text distanceLabel;
        [SerializeField] GameObject lockIcon;
        [SerializeField] GameObject checkIcon;
        Action<int> selected;
        public int StageNumber { get; private set; }
        public bool Locked => !button.interactable;
        public bool Completed => checkIcon.activeSelf;
        public bool LockVisible => lockIcon.activeSelf;
        public UnityEngine.UI.Button Button => button;

        public void Configure(UnityEngine.UI.Button uiButton, UnityEngine.UI.Image image, TMP_Text number,
            TMP_Text state, TMP_Text distance, GameObject padlock, GameObject check)
        {
            button = uiButton; background = image; numberLabel = number; stateLabel = state;
            distanceLabel = distance; lockIcon = padlock; checkIcon = check;
        }

        public void Initialize(PuzzleStageData stage, Action<int> onSelected)
        {
            StageNumber = stage.StageNumber;
            selected = onSelected;
            numberLabel.text = stage.StageNumber.ToString("00");
            distanceLabel.text = stage.CardDifficulty;
            button.onClick.RemoveListener(Click);
            button.onClick.AddListener(Click);
        }

        public void Refresh(PuzzleProgress progress)
        {
            bool completed = progress.IsCleared(StageNumber);
            bool locked = !progress.IsUnlocked(StageNumber);
            button.interactable = !locked;
            lockIcon.SetActive(locked);
            checkIcon.SetActive(completed);
            stateLabel.text = locked ? "LOCKED" : completed ? "COMPLETE" : "NEXT";
            stateLabel.color = locked ? PuzzlePalette.Hex("8291A8") : completed ? PuzzlePalette.Hex("83D9B9") : PuzzlePalette.Hex("F6CE64");
            numberLabel.color = locked ? PuzzlePalette.Hex("8291A8") : Color.white;
            background.color = locked ? PuzzlePalette.Hex("172338") : completed ? PuzzlePalette.Hex("1C3B3C") : PuzzlePalette.Hex("344662");
        }

        void Click() { if (button.interactable) selected?.Invoke(StageNumber); }
        void OnDestroy() { if (button) button.onClick.RemoveListener(Click); }
    }
}
