using System;
using System.Collections;
using System.IO;
using ColorMovePuzzle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class PuzzleTouchVerification
{
    public static void Main()
    {
        var game = UnityEngine.Object.FindAnyObjectByType<PuzzleGameController>();
        game.Restart();
        game.StartCoroutine(Verify(game));
    }

    static IEnumerator Verify(PuzzleGameController game)
    {
        var previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        var previousBackground = InputSystem.settings.backgroundBehavior;
        // The automated Editor is unfocused. Temporarily route synthetic input to the game.
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var screen = InputSystem.AddDevice<Touchscreen>("PuzzleVerificationTouch");
        try
        {
            // Feed the actual InputSystem -> EventSystem -> GraphicRaycaster -> tile path.
            var tile = game.BoardView.FindTile(4);
            var position = RectTransformUtility.WorldToScreenPoint(null, tile.Rect.position);
            InputSystem.QueueStateEvent(screen, new TouchState {touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = position});
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(screen, new TouchState {touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = position});
            yield return null;
            yield return new WaitForSecondsRealtime(0.2f);
            if (game.MoveCount != 1 || game.Board[4] != TileColor.Empty || game.Board[5] != TileColor.Yellow)
                throw new Exception("Virtual touch tap did not traverse the real input pipeline.");

            // Tile 3 now borders empty 4 and 6. Drag down to choose 6 explicitly.
            tile = game.BoardView.FindTile(3);
            position = RectTransformUtility.WorldToScreenPoint(null, tile.Rect.position);
            var end = position + Vector2.down * tile.Rect.rect.width * tile.transform.lossyScale.x * 0.5f;
            InputSystem.QueueStateEvent(screen, new TouchState {touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Began, position = position});
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(screen, new TouchState {touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = end});
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(screen, new TouchState {touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = end});
            yield return null;
            yield return new WaitForSecondsRealtime(0.2f);
            if (game.MoveCount != 2 || game.Board[3] != TileColor.Empty || game.Board[6] != TileColor.Blue)
                throw new Exception("Virtual touch drag did not select the downward empty cell.");
            File.WriteAllText("Artifacts/Verification/TouchInput.json", "{\"passed\":true,\"tapMoves\":1,\"dragMoves\":2,\"path\":\"InputSystem.Touchscreen > EventSystem > GraphicRaycaster > PuzzleTileView\"}");
            Debug.Log("ColorMovePuzzle virtual touchscreen tap and directional drag passed.");
        }
        finally
        {
            InputSystem.RemoveDevice(screen);
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
            InputSystem.settings.backgroundBehavior = previousBackground;
            game.Restart();
        }
    }
}
