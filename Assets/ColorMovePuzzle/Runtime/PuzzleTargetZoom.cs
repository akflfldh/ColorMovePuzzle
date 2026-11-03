using UnityEngine;
using UnityEngine.UI;

namespace ColorMovePuzzle
{
    // A read-only, enlarged target; the ordinary board/input system remains shared.
    public sealed class PuzzleTargetZoom : MonoBehaviour
    {
        [SerializeField] PuzzleGameController game;
        [SerializeField] GameObject panel;
        [SerializeField] PuzzleBoardView enlarged;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;
        public bool IsOpen => panel && panel.activeSelf;
        public PuzzleBoardView Enlarged => enlarged;
        public void Configure(PuzzleGameController controller, GameObject overlay, PuzzleBoardView view, Button open, Button close)
        {game=controller;panel=overlay;enlarged=view;openButton=open;closeButton=close;}
        void Start() {openButton.onClick.AddListener(Open);closeButton.onClick.AddListener(Close);Close();}
        public void Open()
        {
            if (!game.InputEnabled || game.IsCleared || game.Stage.BoardSize!=enlarged.Size) return;
            enlarged.Repaint(new PuzzleBoard(game.Stage.BoardSize,game.Stage.Target));panel.SetActive(true);
        }
        public void Close() {if(panel) panel.SetActive(false);}
        void OnDestroy()
        {if(openButton)openButton.onClick.RemoveListener(Open);if(closeButton)closeButton.onClick.RemoveListener(Close);}
    }
}
