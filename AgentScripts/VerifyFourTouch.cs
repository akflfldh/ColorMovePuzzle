using System;
using System.Collections;
using System.IO;
using ColorMovePuzzle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class FourTouchVerification
{
    public static void Main()
    {
        var menu = UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>();
        var flow = menu.GetComponent<PuzzleCampaignController>();
        string folder = Path.GetFullPath("Artifacts/FourByFour/Preview") + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(flow.SavePath).StartsWith(folder, StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(menu.ProfilePath).StartsWith(folder, StringComparison.OrdinalIgnoreCase))
            throw new Exception("Use isolated FourPreview first.");
        File.WriteAllText("Artifacts/FourByFour/NativeTouch.json", "{\"passed\":false,\"running\":true}");
        menu.SelectMode(4);
        menu.StartCoroutine(Verify(menu));
    }

    static IEnumerator Verify(PuzzleMenuController menu)
    {
        var flow = menu.GetComponent<PuzzleCampaignController>();
        var game = menu.GetComponent<PuzzleGameController>();
        string threePath = Path.Combine(Path.GetDirectoryName(flow.SavePath),"progress-v1.json");
        string originalThree = File.ReadAllText(threePath);
        var previousEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
        var previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var touch = InputSystem.AddDevice<Touchscreen>("FourByFourVerificationTouch");
        try
        {
            yield return Tap(touch,1,(RectTransform)flow.Selection.Buttons[0].transform);
            if (game.Board.Size != 4) throw new Exception("4x4 native selection failed.");
            yield return Tap(touch,2,game.TargetView.Tiles[0].Rect);
            if (game.MoveCount != 0) throw new Exception("Target grid is interactive.");
            yield return Tap(touch,3,game.BoardView.FindTile(7).Rect);
            yield return new WaitForSecondsRealtime(0.2f);
            if (game.MoveCount != 1 || game.Board[11] != TileColor.Green) throw new Exception("4x4 native tap failed.");
            var tile = game.BoardView.FindTile(11);
            Vector2 start = RectTransformUtility.WorldToScreenPoint(null,tile.Rect.position);
            Vector2 end = start + Vector2.down * tile.Rect.rect.width * tile.transform.lossyScale.x * 0.45f;
            Send(touch,4,UnityEngine.InputSystem.TouchPhase.Began,start); yield return null; yield return null;
            for (int step = 1; step <= 6; step++)
            {
                Send(touch,4,UnityEngine.InputSystem.TouchPhase.Moved,Vector2.Lerp(start,end,step/6f));
                yield return null; yield return null;
            }
            Send(touch,4,UnityEngine.InputSystem.TouchPhase.Ended,end); yield return null;
            yield return new WaitForSecondsRealtime(0.2f);
            if (game.MoveCount != 2 || game.Board[15] != TileColor.Green)
            {
                var binding = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                File.WriteAllText("Artifacts/FourByFour/NativeTouch-Diagnostic.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    moves = game.MoveCount, moving = game.IsMoving, from = start.ToString(), to = end.ToString(),
                    pointer = tile.GetType().GetField("pointerId",binding).GetValue(tile),
                    press = tile.GetType().GetField("pressPosition",binding).GetValue(tile).ToString(),
                    dragged = tile.GetType().GetField("dragged",binding).GetValue(tile),
                    cell = tile.CellIndex, board = game.Board.CopyCells()
                }));
                throw new Exception("4x4 native swipe failed.");
            }
            yield return Tap(touch,5,GameObject.Find("RestartButton").GetComponent<RectTransform>());
            if (game.MoveCount != 0 || game.IsMoving) throw new Exception("4x4 native restart failed.");
            foreach (var move in game.Stage.GetSolution())
            { if (!game.TryMove(move.from,move.to)) throw new Exception("Known 4x4 path failed."); while (game.IsMoving) yield return null; }
            if (!game.IsCleared || !flow.Progress.IsUnlocked(2)) throw new Exception("4x4 clear/unlock failed.");
            yield return Tap(touch,6,GameObject.Find("NextStageButton").GetComponent<RectTransform>());
            if (game.Stage.StageNumber != 2 || menu.Profile.LastGridSize != 4) throw new Exception("4x4 next/history failed.");
            yield return Tap(touch,7,GameObject.Find("StageSelectButton").GetComponent<RectTransform>());
            yield return Tap(touch,8,GameObject.Find("MainMenuBackButton").GetComponent<RectTransform>());
            yield return Tap(touch,9,(RectTransform)menu.MainMenu.ContinueButton.transform);
            if (game.Stage.BoardSize != 4 || game.Stage.StageNumber != 2) throw new Exception("4x4 Continue failed.");
            menu.ShowMainMenu();
            yield return Tap(touch,10,(RectTransform)menu.MainMenu.ModeButtons[0].transform);
            if (flow.Catalog.GetStage(1).BoardSize != 3 || flow.Progress.ClearedCount != 0 || flow.Progress.IsUnlocked(2))
                throw new Exception("3x3 state was affected by 4x4.");
            if (File.ReadAllText(threePath) != originalThree) throw new Exception("4x4 changed the 3x3 file.");
            File.WriteAllText("Artifacts/FourByFour/NativeTouch.json", "{\"passed\":true,\"selection\":true,\"targetReadOnly\":true,\"tap\":true,\"swipe\":true,\"restart\":true,\"clear\":true,\"next\":true,\"continue4\":true,\"threeProgressUnaffected\":true,\"userSaveModified\":false}");
            Debug.Log("4x4 native touchscreen input, unlock, Continue and 3x3 independence passed.");
        }
        finally
        {
            if (File.ReadAllText("Artifacts/FourByFour/NativeTouch.json").Contains("running"))
                File.WriteAllText("Artifacts/FourByFour/NativeTouch.json", "{\"passed\":false,\"running\":false}");
            InputSystem.RemoveDevice(touch);
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditor;
            InputSystem.settings.backgroundBehavior = previousBackground;
            menu.ShowMainMenu();
        }
    }
    static IEnumerator Tap(Touchscreen device,int id,RectTransform rect)
    {
        yield return null; yield return null;
        var p = RectTransformUtility.WorldToScreenPoint(null,rect.position);
        Send(device,id,UnityEngine.InputSystem.TouchPhase.Began,p); yield return null; yield return null;
        Send(device,id,UnityEngine.InputSystem.TouchPhase.Ended,p); yield return null; yield return null;
    }
    static void Send(Touchscreen d,int id,UnityEngine.InputSystem.TouchPhase phase,Vector2 position)
        => InputSystem.QueueStateEvent(d,new TouchState {touchId=id,phase=phase,position=position});
}
