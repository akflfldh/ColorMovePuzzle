using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ColorMovePuzzle
{
    public sealed class PuzzleStageSelectionView : MonoBehaviour
    {
        [SerializeField] PuzzleStageButtonView[] buttons;
        [SerializeField] UnityEngine.UI.ScrollRect scroll;
        [SerializeField] TMP_Text summary;
        [SerializeField] TMP_Text nextStage;
        [SerializeField] TMP_Text saveStatus;
        [SerializeField] TMP_Text collectionLabel;
        public void ConfigureCollectionLabel(TMP_Text label) => collectionLabel = label;
        public IReadOnlyList<PuzzleStageButtonView> Buttons => buttons;
        public UnityEngine.UI.ScrollRect Scroll => scroll;

        public void Configure(PuzzleStageButtonView[] cards, UnityEngine.UI.ScrollRect scroller,
            TMP_Text progress, TMP_Text next, TMP_Text storageStatus)
        { buttons = cards; scroll = scroller; summary = progress; nextStage = next; saveStatus = storageStatus; }

        public void Initialize(PuzzleStageCatalog catalog, Action<int> onSelected)
        {
            if (buttons.Length != catalog.Count) throw new InvalidOperationException("Selection card count differs from catalog.");
            for (int i = 0; i < buttons.Length; i++) buttons[i].Initialize(catalog.GetStage(i + 1), onSelected);
            if (collectionLabel) collectionLabel.text = $"{catalog.GetStage(1).BoardSize} × {catalog.GetStage(1).BoardSize} COLLECTION";
        }

        public void Refresh(PuzzleProgress progress, string error)
        {
            foreach (var button in buttons) button.Refresh(progress);
            summary.text = $"{progress.ClearedCount:00} / {progress.StageCount:00} COMPLETE";
            nextStage.text = progress.AllCleared ? "ALL STAGES COMPLETE" : $"UP NEXT: STAGE {progress.NextStage:00}";
            saveStatus.text = string.IsNullOrEmpty(error) ? "PROGRESS IS SAVED ON THIS DEVICE" : "CHECK DEVICE STORAGE TO KEEP YOUR PROGRESS";
        }

        public void FocusStage(int number)
        {
            Canvas.ForceUpdateCanvases();
            float overflow = Mathf.Max(1, scroll.content.rect.height - scroll.viewport.rect.height);
            float rowHeight = (scroll.content.rect.height - 24) / 10f;
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01(((number - 1) / 3) * rowHeight / overflow);
        }
    }
}
