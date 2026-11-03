using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColorMovePuzzle.Tests
{
    public sealed class PuzzleMenuPlayTests
    {
        PuzzleMenuController menu;
        PuzzleCampaignController campaign;
        PuzzleGameController game;
        string path;

        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity");
            yield return null;
            path = Path.Combine(Application.temporaryCachePath, "ColorMovePuzzleTests", Guid.NewGuid() + ".json");
            BindStores();
            menu.ShowMainMenu();
        }
        void BindStores()
        {
            menu = UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>();
            campaign = UnityEngine.Object.FindAnyObjectByType<PuzzleCampaignController>();
            game = UnityEngine.Object.FindAnyObjectByType<PuzzleGameController>();
            campaign.UseProgressStore(new PuzzleProgressStore(path, 30));
            menu.UseProfileStore(new PuzzleMenuProfileStore(path + ".menu"));
        }
        [TearDown] public void Cleanup()
        {
            foreach (string suffix in new[] {"", ".tmp", ".bak", ".menu", ".menu.tmp", ".menu.bak"})
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }

        [UnityTest] public IEnumerator NewSceneStartsAtMainMenuAndFreshProfileCannotContinue()
        {
            Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.MainMenu));
            Assert.That(menu.MainMenu.gameObject.activeSelf, Is.True);
            Assert.That(campaign.IsSelecting, Is.False);
            Assert.That(game.InputEnabled, Is.False);
            Assert.That(menu.CanContinue, Is.False);
            Assert.That(menu.MainMenu.ContinueButton.interactable, Is.False);
            yield return SceneManager.LoadSceneAsync("Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity");
            yield return null;
            BindStores();
            Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.MainMenu), "The scene itself must start at the menu.");
            Assert.That(menu.MainMenu.gameObject.activeSelf, Is.True);
            Assert.That(campaign.IsSelecting, Is.False);
        }

        [Test] public void AllImplementedCardsSelectTheirModesAndUnknownSizesAreRejected()
        {
            foreach (var button in menu.MainMenu.ModeButtons)
            {
                Assert.That(button.Button.interactable, Is.True);
                button.Button.onClick.Invoke();
                Assert.That(campaign.Catalog.GetStage(1).BoardSize, Is.EqualTo(button.GridSize));
                Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.StageSelection));
                menu.ShowMainMenu();
            }
            Assert.That(menu.SelectMode(6), Is.False);
            Assert.That(menu.Profile.HasContinue, Is.False);
        }

        [Test] public void ButtonsNavigateMenuSelectionAndPuzzleWithoutReplacingCampaign()
        {
            menu.MainMenu.ModeButtons[0].Button.onClick.Invoke();
            Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.StageSelection));
            Assert.That(campaign.Selection.Buttons.Count, Is.EqualTo(30));
            Assert.That(menu.Profile.HasContinue, Is.False, "Browsing stages is not a play record.");
            GameObject.Find("MainMenuBackButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.MainMenu));
            menu.SelectMode(3); campaign.PlayStage(1);
            Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.Playing));
            GameObject.Find("StageSelectButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.StageSelection));
            Assert.That(menu.HandleBack(), Is.True);
            Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.MainMenu));
            Assert.That(menu.HandleBack(), Is.False);
        }

        [UnityTest] public IEnumerator ContinueRestoresLastGridAndUnlockedStageAcrossSceneSessions()
        {
            menu.SelectMode(3); campaign.PlayStage(1);
            yield return Solve();
            campaign.PlayStage(2);
            menu.ShowMainMenu();
            Assert.That(menu.CanContinue, Is.True);
            Assert.That(menu.Profile.LastGridSize, Is.EqualTo(3));
            Assert.That(menu.Profile.LastStage, Is.EqualTo(2));
            string progressBefore = File.ReadAllText(path);
            yield return SceneManager.LoadSceneAsync("Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity");
            yield return null;
            BindStores();
            menu.ShowMainMenu();
            Assert.That(menu.MainMenu.ContinueButton.interactable, Is.True);
            menu.MainMenu.ContinueButton.onClick.Invoke();
            Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.Playing));
            Assert.That(game.Stage.StageNumber, Is.EqualTo(2));
            Assert.That(game.Stage.BoardSize, Is.EqualTo(3));
            Assert.That(game.MoveCount, Is.Zero);
            Assert.That(campaign.Progress.ClearedCount, Is.EqualTo(1));
            Assert.That(File.ReadAllText(path), Is.EqualTo(progressBefore));
        }

        [UnityTest] public IEnumerator SettingsPersistBothSlidersWithoutCreatingPlayHistory()
        {
            int notifications = 0;
            menu.VolumesChanged += (music, sound) => notifications++;
            menu.MainMenu.SettingsButton.onClick.Invoke();
            Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.Settings));
            menu.Settings.BgmSlider.value = 0.23f;
            menu.Settings.SfxSlider.value = 0.61f;
            Assert.That(notifications, Is.EqualTo(2));
            menu.Settings.CloseButton.onClick.Invoke();
            Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.MainMenu));
            Assert.That(menu.CanContinue, Is.False);
            Assert.That(File.Exists(path), Is.False, "Settings must not create stage progress.");
            yield return SceneManager.LoadSceneAsync("Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity");
            yield return null;
            BindStores();
            menu.ShowMainMenu(); menu.OpenSettings();
            Assert.That(menu.Settings.BgmSlider.value, Is.EqualTo(0.23f).Within(0.001));
            Assert.That(menu.Settings.SfxSlider.value, Is.EqualTo(0.61f).Within(0.001));
            Assert.That(menu.HandleBack(), Is.True);
            Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.MainMenu));
            Assert.That(menu.CanContinue, Is.False);
        }

        [Test] public void LegacyHistoryEnablesContinueWithoutChangingLegacyFile()
        {
            var progress = new PuzzleProgress(30);
            progress.MarkCleared(1); progress.Select(2);
            Assert.That(new PuzzleProgressStore(path, 30).Save(progress), Is.True);
            string bytes = File.ReadAllText(path);
            campaign.UseProgressStore(new PuzzleProgressStore(path, 30));
            menu.UseProfileStore(new PuzzleMenuProfileStore(path + ".menu"));
            menu.ShowMainMenu();
            Assert.That(menu.CanContinue, Is.True);
            Assert.That(menu.Profile.LastStage, Is.EqualTo(2));
            Assert.That(File.ReadAllText(path), Is.EqualTo(bytes));
            Assert.That(File.Exists(path + ".menu"), Is.False, "Migration should not write on startup.");
        }

        [Test] public void ContinueRejectsUnavailableModesAndLockedStages()
        {
            var profile = new PuzzleMenuProfile();
            profile.RecordStage(6, 1);
            var store = new PuzzleMenuProfileStore(path + ".menu");
            store.Save(profile); menu.UseProfileStore(store); menu.ShowMainMenu();
            Assert.That(menu.CanContinue, Is.False);
            Assert.That(menu.Continue(), Is.False);
            profile.RecordStage(3, 30); store.Save(profile); menu.UseProfileStore(store); menu.ShowMainMenu();
            Assert.That(menu.CanContinue, Is.False);
            Assert.That(menu.Continue(), Is.False);
            Assert.That(campaign.Progress.ClearedCount, Is.Zero);
        }

        [UnityTest] public IEnumerator AndroidEscapeInputReturnsOneScreenAtATime()
        {
            var previousEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            var previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard = InputSystem.AddDevice<Keyboard>("MenuBackVerification");
            try
            {
                yield return null; yield return null;
                menu.OpenSettings();
                yield return Back(keyboard);
                Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.MainMenu));
                menu.SelectMode(3); campaign.PlayStage(1);
                var first = game.Stage.GetSolution()[0];
                Assert.That(game.TryMove(first.from, first.to), Is.True);
                yield return Back(keyboard);
                Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.StageSelection));
                Assert.That(game.IsMoving, Is.False);
                yield return Back(keyboard);
                Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.MainMenu));
                Assert.That(campaign.Progress.ClearedCount, Is.Zero);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditor;
                InputSystem.settings.backgroundBehavior = previousBackground;
            }
        }

        IEnumerator Back(Keyboard keyboard)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null; yield return null;
        }
        IEnumerator Solve()
        {
            foreach (var move in game.Stage.GetSolution())
            {
                Assert.That(game.TryMove(move.from, move.to), Is.True);
                float timeout = Time.realtimeSinceStartup + 2;
                while (game.IsMoving && Time.realtimeSinceStartup < timeout) yield return null;
                Assert.That(game.IsMoving, Is.False);
            }
            Assert.That(game.IsCleared, Is.True);
        }
    }
}
