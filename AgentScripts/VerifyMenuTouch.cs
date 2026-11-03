using System;
using System.Collections;
using System.IO;
using ColorMovePuzzle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class MenuTouchVerification
{
    public static void Main()
    {
        var menu = UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>();
        string folder = Path.GetFullPath("Artifacts/MainMenu") + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(menu.ProfilePath).StartsWith(folder, StringComparison.OrdinalIgnoreCase))
            throw new Exception("Use isolated MenuPreview first.");
        File.WriteAllText("Artifacts/MainMenu/NativeTouch.json", "{\"passed\":false,\"running\":true}");
        menu.ShowMainMenu();
        menu.StartCoroutine(Verify(menu));
    }

    static IEnumerator Verify(PuzzleMenuController menu)
    {
        var flow = menu.GetComponent<PuzzleCampaignController>();
        var game = menu.GetComponent<PuzzleGameController>();
        var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
        var background = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var touch = InputSystem.AddDevice<Touchscreen>("MainMenuVerificationTouch");
        try
        {
            yield return Tap(touch, 1, (RectTransform)menu.MainMenu.ModeButtons[2].transform);
            if (menu.Screen != PuzzleScreen.MainMenu) throw new Exception("Coming Soon accepted a touch.");
            yield return Tap(touch, 2, (RectTransform)menu.MainMenu.SettingsButton.transform);
            if (menu.Screen != PuzzleScreen.Settings) throw new Exception("Settings touch failed.");
            yield return DragSlider(touch, 3, menu.Settings.BgmSlider, 0.2f);
            yield return DragSlider(touch, 4, menu.Settings.SfxSlider, 0.55f);
            if (Mathf.Abs(menu.Profile.BgmVolume - 0.2f) > 0.02f || Mathf.Abs(menu.Profile.SfxVolume - 0.55f) > 0.02f)
                throw new Exception("Native slider touch failed.");
            var saved = new PuzzleMenuProfileStore(menu.ProfilePath).Load();
            if (Mathf.Abs(saved.BgmVolume - menu.Profile.BgmVolume) > 0.001f) throw new Exception("Slider save failed.");
            yield return Tap(touch, 5, (RectTransform)menu.Settings.CloseButton.transform);
            yield return Tap(touch, 6, (RectTransform)menu.MainMenu.ModeButtons[0].transform);
            if (menu.Screen != PuzzleScreen.StageSelection) throw new Exception("3x3 touch failed.");
            yield return Tap(touch, 7, (RectTransform)flow.Selection.Buttons[0].transform);
            if (menu.Screen != PuzzleScreen.Playing || game.Stage.StageNumber != 1) throw new Exception("Stage touch failed.");
            yield return Tap(touch, 8, GameObject.Find("StageSelectButton").GetComponent<RectTransform>());
            yield return Tap(touch, 9, GameObject.Find("MainMenuBackButton").GetComponent<RectTransform>());
            if (menu.Screen != PuzzleScreen.MainMenu || !menu.CanContinue) throw new Exception("Return or Continue state failed.");
            yield return Tap(touch, 10, (RectTransform)menu.MainMenu.ContinueButton.transform);
            if (menu.Screen != PuzzleScreen.Playing || game.Stage.StageNumber != 1) throw new Exception("Continue touch failed.");
            File.WriteAllText("Artifacts/MainMenu/NativeTouch.json", "{\"passed\":true,\"settings\":true,\"sliders\":true,\"comingSoonBlocked\":true,\"modeSelection\":true,\"backButtons\":true,\"continue\":true,\"userSaveModified\":false}");
            Debug.Log("Main menu native touch, sliders, navigation and Continue passed.");
        }
        finally
        {
            InputSystem.RemoveDevice(touch);
            InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            InputSystem.settings.backgroundBehavior = background;
            menu.ShowMainMenu();
        }
    }

    static IEnumerator Tap(Touchscreen screen, int id, RectTransform rect)
    {
        yield return null; yield return null;
        var position = RectTransformUtility.WorldToScreenPoint(null, rect.position);
        Send(screen, id, UnityEngine.InputSystem.TouchPhase.Began, position);
        yield return null; yield return null;
        Send(screen, id, UnityEngine.InputSystem.TouchPhase.Ended, position);
        yield return null; yield return null;
    }
    static IEnumerator DragSlider(Touchscreen screen, int id, UnityEngine.UI.Slider slider, float value)
    {
        yield return null; yield return null;
        Vector2 start = RectTransformUtility.WorldToScreenPoint(null, slider.handleRect.position);
        var area = (RectTransform)slider.handleRect.parent;
        Vector2 end = RectTransformUtility.WorldToScreenPoint(null, area.TransformPoint(new Vector3(Mathf.Lerp(area.rect.xMin, area.rect.xMax, value), 0, 0)));
        Send(screen, id, UnityEngine.InputSystem.TouchPhase.Began, start);
        yield return null; yield return null;
        for (int i = 1; i <= 6; i++)
        {
            Send(screen, id, UnityEngine.InputSystem.TouchPhase.Moved, Vector2.Lerp(start, end, i / 6f));
            yield return null; yield return null;
        }
        Send(screen, id, UnityEngine.InputSystem.TouchPhase.Ended, end);
        yield return null; yield return null;
    }
    static void Send(Touchscreen screen, int id, UnityEngine.InputSystem.TouchPhase phase, Vector2 position)
        => InputSystem.QueueStateEvent(screen, new TouchState {touchId = id, phase = phase, position = position});
}
