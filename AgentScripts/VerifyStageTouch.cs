using System;
using System.Collections;
using System.IO;
using ColorMovePuzzle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class CampaignTouchVerification
{
    public static void Main()
    {
        var flow = UnityEngine.Object.FindAnyObjectByType<PuzzleCampaignController>();
        string previewFolder = Path.GetFullPath("Artifacts/StageSystem") + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(flow.SavePath).StartsWith(previewFolder, StringComparison.OrdinalIgnoreCase))
            throw new Exception("Use the isolated CampaignPreview first.");
        var store = new PuzzleProgressStore(flow.SavePath, 30);
        store.Save(new PuzzleProgress(30));
        flow.UseProgressStore(store);
        flow.ShowSelection();
        flow.StartCoroutine(Verify(flow));
    }

    static IEnumerator Verify(PuzzleCampaignController flow)
    {
        var game = flow.GetComponent<PuzzleGameController>();
        var editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        var background = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var touch = InputSystem.AddDevice<Touchscreen>("CampaignVerificationTouch");
        try
        {
            // Let newly enabled layout and device bindings settle before sampling a card position.
            yield return null;
            yield return null;
            yield return Tap(touch, 1, (RectTransform)flow.Selection.Buttons[0].transform);
            if (flow.IsSelecting || game.Stage.StageNumber != 1)
            {
                var module = UnityEngine.Object.FindAnyObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                File.WriteAllText("Artifacts/StageSystem/NativeTouch-Diagnostic.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    click = module.leftClick.action.ToString(), point = module.point.action.ToString(),
                    touchEnabled = touch.enabled, position = touch.primaryTouch.position.ReadValue().ToString(),
                    gameFocused = Application.isFocused, screenWidth = Screen.width,
                    stage = game.Stage.StageNumber, selecting = flow.IsSelecting
                }));
                throw new Exception("Native stage-card touch failed.");
            }
            foreach (var move in game.Stage.GetSolution())
            {
                if (!game.TryMove(move.from, move.to)) throw new Exception("Preview solution failed.");
                while (game.IsMoving) yield return null;
            }
            yield return Tap(touch, 2, GameObject.Find("NextStageButton").GetComponent<RectTransform>());
            if (game.Stage.StageNumber != 2 || !flow.Progress.IsUnlocked(2)) throw new Exception("Native next-stage touch failed.");
            yield return Tap(touch, 3, GameObject.Find("StageSelectButton").GetComponent<RectTransform>());
            if (!flow.IsSelecting || !flow.Selection.Buttons[0].Completed) throw new Exception("Native back-to-selection touch failed.");
            var scroll = flow.Selection.Scroll;
            Vector2 start = RectTransformUtility.WorldToScreenPoint(null, scroll.viewport.position);
            float before = scroll.verticalNormalizedPosition;
            Send(touch, 4, UnityEngine.InputSystem.TouchPhase.Began, start);
            yield return null; yield return null;
            for (int i = 1; i <= 8; i++)
            {
                Send(touch, 4, UnityEngine.InputSystem.TouchPhase.Moved, start + Vector2.up * (i * 50));
                yield return null; yield return null;
            }
            Send(touch, 4, UnityEngine.InputSystem.TouchPhase.Ended, start + Vector2.up * 400);
            yield return null; yield return null;
            float after = scroll.verticalNormalizedPosition;
            scroll.StopMovement();
            if (after >= before - 0.05f) throw new Exception("Native touch drag did not scroll the stage list.");
            File.WriteAllText("Artifacts/StageSystem/NativeTouch.json", "{\"passed\":true,\"stageCard\":true,\"nextStage\":true,\"backButton\":true,\"scroll\":true,\"userSaveModified\":false}");
            Debug.Log("Campaign native touchscreen selection, next, back and scrolling passed.");
        }
        finally
        {
            InputSystem.RemoveDevice(touch);
            InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
            InputSystem.settings.backgroundBehavior = background;
            flow.ShowSelection();
        }
    }

    static IEnumerator Tap(Touchscreen screen, int id, RectTransform rect)
    {
        var position = RectTransformUtility.WorldToScreenPoint(null, rect.position);
        Send(screen, id, UnityEngine.InputSystem.TouchPhase.Began, position);
        yield return null; yield return null;
        Send(screen, id, UnityEngine.InputSystem.TouchPhase.Ended, position);
        yield return null; yield return null;
    }

    static void Send(Touchscreen screen, int id, UnityEngine.InputSystem.TouchPhase phase, Vector2 position)
        => InputSystem.QueueStateEvent(screen, new TouchState {touchId = id, phase = phase, position = position});
}
