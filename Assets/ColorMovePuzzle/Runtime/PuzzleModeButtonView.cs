using System;
using TMPro;
using UnityEngine;

namespace ColorMovePuzzle
{
    public sealed class PuzzleModeButtonView : MonoBehaviour
    {
        [SerializeField] int gridSize;
        [SerializeField] UnityEngine.UI.Button button;
        [SerializeField] UnityEngine.UI.Image background;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text caption;
        Action<int> selected;
        public int GridSize => gridSize;
        public UnityEngine.UI.Button Button => button;

        public void Configure(int size, UnityEngine.UI.Button uiButton, UnityEngine.UI.Image image, TMP_Text label, TMP_Text detail)
        { gridSize = size; button = uiButton; background = image; title = label; caption = detail; }
        public void Initialize(Action<int> onSelected)
        {
            selected = onSelected;
            button.onClick.RemoveListener(Click);
            button.onClick.AddListener(Click);
        }
        public void Refresh(PuzzleModeDefinition mode, bool playable)
        {
            button.interactable = playable;
            title.text = $"{gridSize} × {gridSize}";
            caption.text = playable ? $"{mode.Stages.Count} STAGES" : "COMING SOON";
            background.color = playable ? PuzzlePalette.Hex("DFE7F4") : PuzzlePalette.Hex("19273B");
            title.color = caption.color = playable ? PuzzlePalette.Hex("142137") : PuzzlePalette.Hex("8190A6");
        }
        void Click() { if (button.interactable) selected?.Invoke(gridSize); }
        void OnDestroy() { if (button) button.onClick.RemoveListener(Click); }
    }
}
