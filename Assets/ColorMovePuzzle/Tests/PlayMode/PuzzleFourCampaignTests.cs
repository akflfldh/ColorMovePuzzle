using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColorMovePuzzle.Tests
{
    public sealed class PuzzleFourCampaignTests
    {
        PuzzleMenuController menu;
        PuzzleCampaignController campaign;
        PuzzleGameController game;
        string folder;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity");
            yield return null;
            folder = Path.Combine(Application.temporaryCachePath, "ColorMovePuzzleTests", Guid.NewGuid().ToString());
            Bind(); menu.ShowMainMenu();
        }
        void Bind()
        {
            menu = UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>();
            campaign = UnityEngine.Object.FindAnyObjectByType<PuzzleCampaignController>();
            game = UnityEngine.Object.FindAnyObjectByType<PuzzleGameController>();
            campaign.UseProgressDirectory(folder);
            menu.UseProfileStore(new PuzzleMenuProfileStore(Path.Combine(folder,"menu.json")));
        }
        [TearDown] public void Cleanup()
        {
            if (!Directory.Exists(folder)) return;
            foreach (string file in Directory.GetFiles(folder)) File.Delete(file);
            Directory.Delete(folder);
        }

        [Test] public void FourModeUsesExistingControllerAndSelectionWithHonestLabels()
        {
            Assert.That(menu.MainMenu.ModeButtons[1].Button.interactable, Is.True);
            Assert.That(menu.MainMenu.ModeButtons[2].Button.interactable, Is.True);
            Assert.That(menu.SelectMode(4), Is.True);
            Assert.That(campaign.Catalog.Count, Is.EqualTo(30));
            Assert.That(GameObject.Find("SelectionEyebrow").GetComponent<TMP_Text>().text, Is.EqualTo("4 × 4 COLLECTION"));
            Assert.That(campaign.Selection.Buttons[0].transform.Find("MinimumMoves").GetComponent<TMP_Text>().text, Does.StartWith("RATING"));
            Assert.That(campaign.Progress.IsUnlocked(2), Is.False);
            Assert.That(campaign.PlayStage(1), Is.True);
            Assert.That(game.Board.Size, Is.EqualTo(4));
            Assert.That(game.BoardView.Tiles.Count, Is.EqualTo(14));
            Assert.That(game, Is.SameAs(campaign.GetComponent<PuzzleGameController>()));
            Assert.That(GameObject.Find("TargetHint").GetComponent<TMP_Text>().text, Does.StartWith("ESTIMATED"));
            Assert.That(UnityEngine.Object.FindObjectsByType<MeshRenderer>(), Is.Empty);
        }

        [UnityTest] public IEnumerator FourClearUnlocksNextAndNativeRuleDirectionsHandleTwoEmptyChoices()
        {
            menu.SelectMode(4); campaign.PlayStage(1);
            Assert.That(game.TryMove(7,11), Is.True);
            yield return Finished();
            game.RequestTap(11);
            Assert.That(game.MoveCount, Is.EqualTo(1), "Ambiguous taps must wait for direction.");
            game.RequestSwipe(11, Vector2.down * 100);
            yield return Finished();
            Assert.That(game.Board[15], Is.EqualTo(TileColor.Green));
            Assert.That(game.MoveCount, Is.EqualTo(2));
            game.Restart();
            yield return Solve();
            Assert.That(campaign.Progress.IsUnlocked(2), Is.True);
            Assert.That(new PuzzleProgressStore(Path.Combine(folder,"progress-4x4-v1.json"),30).Load().IsCleared(1), Is.True);
            Assert.That(File.Exists(Path.Combine(folder,"progress-v1.json")), Is.False);
            GameObject.Find("NextStageButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(game.Stage.StageNumber, Is.EqualTo(2));
            Assert.That(game.Board.Size, Is.EqualTo(4));
            Assert.That(game.MoveCount, Is.Zero);
        }

        [UnityTest] public IEnumerator BothProgressFilesAndFourContinueSurviveNewSessionIndependently()
        {
            menu.SelectMode(3); campaign.PlayStage(1); yield return Solve();
            string three = File.ReadAllText(Path.Combine(folder,"progress-v1.json"));
            menu.ShowMainMenu(); menu.SelectMode(4); campaign.PlayStage(1); yield return Solve();
            campaign.PlayStage(2); menu.ShowMainMenu();
            Assert.That(File.ReadAllText(Path.Combine(folder,"progress-v1.json")), Is.EqualTo(three));
            Assert.That(menu.Profile.LastGridSize, Is.EqualTo(4));
            menu.SelectMode(3);
            Assert.That(campaign.Progress.ClearedCount, Is.EqualTo(1));
            Assert.That(campaign.Progress.LastPlayed, Is.EqualTo(1));
            menu.ShowMainMenu();
            yield return SceneManager.LoadSceneAsync("Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity"); yield return null;
            Bind(); menu.ShowMainMenu();
            Assert.That(menu.CanContinue, Is.True);
            Assert.That(menu.Continue(), Is.True);
            Assert.That(game.Stage.BoardSize, Is.EqualTo(4));
            Assert.That(game.Stage.StageNumber, Is.EqualTo(2));
            Assert.That(campaign.Progress.ClearedCount, Is.EqualTo(1));
            Assert.That(File.ReadAllText(Path.Combine(folder,"progress-v1.json")), Is.EqualTo(three));
        }

        [UnityTest] public IEnumerator SwitchingDuringAnimationRestoresThreeByThreeViewsAndRules()
        {
            menu.SelectMode(4); campaign.PlayStage(1);
            Assert.That(game.TryMove(7,11), Is.True);
            menu.ShowMainMenu(); menu.SelectMode(3); campaign.PlayStage(1);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(game.IsMoving, Is.False);
            Assert.That(game.Board.Size, Is.EqualTo(3));
            Assert.That(game.BoardView.Tiles.Count, Is.EqualTo(7));
            Assert.That(game.TargetView.Size, Is.EqualTo(3));
            Assert.That(GameObject.Find("CurrentBoard4"), Is.Null);
            yield return Solve();
            Assert.That(game.IsCleared, Is.True);
        }

        [UnityTest] public IEnumerator FirstMiddleAndLastFourStagesClearWithSharedAnimations()
        {
            // Fixture unlocks the samples; all 30 legal solutions are independently verified in EditMode.
            var progress = new PuzzleProgress(30);
            for (int i = 1; i <= 29; i++) progress.MarkCleared(i);
            new PuzzleProgressStore(Path.Combine(folder,"progress-4x4-v1.json"),30).Save(progress);
            menu.SelectMode(4);
            foreach (int stage in new[] {1,15,30})
            {
                Assert.That(campaign.PlayStage(stage), Is.True);
                yield return Solve();
            }
            Assert.That(campaign.Progress.AllCleared, Is.True);
            Assert.That(GameObject.Find("NextStageButton"), Is.Null);
            Assert.That(GameObject.Find("ClearTitle").GetComponent<TMP_Text>().text, Is.EqualTo("All stages complete!"));
        }

        IEnumerator Solve()
        {
            foreach (var move in game.Stage.GetSolution())
            { Assert.That(game.TryMove(move.from,move.to), Is.True); yield return Finished(); }
            Assert.That(game.IsCleared, Is.True);
            Assert.That(game.Board.Matches(game.Stage.Target), Is.True);
            Assert.That(game.MoveCount, Is.EqualTo(game.Stage.KnownSolutionLength));
        }
        IEnumerator Finished()
        {
            float until = Time.realtimeSinceStartup + 2;
            while (game.IsMoving && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(game.IsMoving, Is.False);
        }
    }
}
