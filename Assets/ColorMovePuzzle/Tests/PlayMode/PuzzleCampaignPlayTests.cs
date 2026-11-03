using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColorMovePuzzle.Tests
{
    public sealed class PuzzleCampaignPlayTests
    {
        PuzzleCampaignController campaign;
        PuzzleGameController game;
        string path;

        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity");
            yield return null;
            campaign = UnityEngine.Object.FindAnyObjectByType<PuzzleCampaignController>();
            game = UnityEngine.Object.FindAnyObjectByType<PuzzleGameController>();
            path = Path.Combine(Application.temporaryCachePath, "ColorMovePuzzleTests", Guid.NewGuid() + ".json");
            campaign.UseProgressStore(new PuzzleProgressStore(path, 30));
            UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>().UseProfileStore(new PuzzleMenuProfileStore(path + ".menu"));
            campaign.ShowSelection();
        }

        [TearDown] public void Cleanup()
        {
            foreach (string suffix in new[] {"", ".bak", ".tmp", ".menu", ".menu.bak", ".menu.tmp"}) if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }

        [Test] public void SelectionHasThirtyCardsWithOnlyOneUnlockedAndRealScroll()
        {
            Assert.That(campaign.IsSelecting, Is.True);
            Assert.That(game.InputEnabled, Is.False);
            Assert.That(campaign.Selection.Buttons.Count, Is.EqualTo(30));
            Assert.That(campaign.Selection.Buttons[0].Locked, Is.False);
            for (int i = 1; i < 30; i++)
            {
                Assert.That(campaign.Selection.Buttons[i].Locked, Is.True);
                Assert.That(campaign.Selection.Buttons[i].LockVisible, Is.True);
            }
            Canvas.ForceUpdateCanvases();
            var scroll = campaign.Selection.Scroll;
            Assert.That(scroll.vertical, Is.True);
            Assert.That(scroll.horizontal, Is.False);
            Assert.That(scroll.viewport.GetComponent<UnityEngine.UI.Mask>(), Is.Not.Null);
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
            Assert.That(UnityEngine.Object.FindObjectsByType<MeshRenderer>(), Is.Empty);
            Assert.That(UnityEngine.Object.FindObjectsByType<EventSystem>().Length, Is.EqualTo(1));
        }

        [Test] public void LockedStageCannotBeEnteredEvenThroughDirectCallback()
        {
            Assert.That(campaign.PlayStage(2), Is.False);
            campaign.Selection.Buttons[1].Button.onClick.Invoke();
            Assert.That(campaign.IsSelecting, Is.True);
            Assert.That(campaign.Progress.LastPlayed, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator ClearPersistsUnlockAndNextButtonLoadsFollowingStage()
        {
            campaign.Selection.Buttons[0].Button.onClick.Invoke();
            Assert.That(campaign.IsSelecting, Is.False);
            Assert.That(game.Stage.StageNumber, Is.EqualTo(1));
            yield return Solve();
            Assert.That(campaign.Progress.IsCleared(1), Is.True);
            Assert.That(campaign.Progress.IsUnlocked(2), Is.True);
            Assert.That(new PuzzleProgressStore(path, 30).Load().IsUnlocked(2), Is.True);
            Assert.That(GameObject.Find("ClearEyebrow").GetComponent<TMP_Text>().text, Is.EqualTo("STAGE 01 COMPLETE"));
            GameObject.Find("NextStageButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(game.Stage.StageNumber, Is.EqualTo(2));
            Assert.That(game.MoveCount, Is.Zero);
            Assert.That(game.IsCleared, Is.False);
            Assert.That(GameObject.Find("StageLabel").GetComponent<TMP_Text>().text, Is.EqualTo("STAGE 02"));
        }

        [UnityTest] public IEnumerator BackDuringSlideDoesNotClearOrUnlockAndReplayResets()
        {
            Assert.That(campaign.PlayStage(1), Is.True);
            var start = game.Board.CopyCells();
            var move = game.Stage.GetSolution()[0];
            Assert.That(game.TryMove(move.from, move.to), Is.True);
            GameObject.Find("StageSelectButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(campaign.IsSelecting, Is.True);
            Assert.That(game.IsMoving, Is.False);
            Assert.That(campaign.Progress.ClearedCount, Is.Zero);
            Assert.That(campaign.Progress.IsUnlocked(2), Is.False);
            Assert.That(campaign.PlayStage(1), Is.True);
            CollectionAssert.AreEqual(start, game.Board.CopyCells());
            Assert.That(game.MoveCount, Is.Zero);
        }

        [UnityTest] public IEnumerator NewSceneSessionLoadsFileAndAllowsCompletedStageReplay()
        {
            campaign.PlayStage(1);
            yield return Solve();
            yield return SceneManager.LoadSceneAsync("Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity");
            yield return null;
            campaign = UnityEngine.Object.FindAnyObjectByType<PuzzleCampaignController>();
            game = UnityEngine.Object.FindAnyObjectByType<PuzzleGameController>();
            campaign.UseProgressStore(new PuzzleProgressStore(path, 30));
            UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>().UseProfileStore(new PuzzleMenuProfileStore(path + ".menu"));
            campaign.ShowSelection();
            Assert.That(campaign.Selection.Buttons[0].Completed, Is.True);
            Assert.That(campaign.Selection.Buttons[1].Locked, Is.False);
            Assert.That(campaign.Selection.Buttons[2].Locked, Is.True);
            Assert.That(campaign.PlayStage(1), Is.True);
            Assert.That(game.MoveCount, Is.Zero);
            yield return Solve();
            Assert.That(campaign.Progress.ClearedCount, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator EntireCampaignClearsInOrderAndFinalStageShowsCompletion()
        {
            int totalMoves = 0;
            Assert.That(campaign.PlayStage(1), Is.True);
            for (int number = 1; number <= 30; number++)
            {
                Assert.That(game.Stage.StageNumber, Is.EqualTo(number));
                yield return Solve();
                totalMoves += game.MoveCount;
                Assert.That(campaign.Progress.ClearedCount, Is.EqualTo(number));
                if (number < 30)
                {
                    Assert.That(campaign.Progress.IsUnlocked(number + 1), Is.True);
                    GameObject.Find("NextStageButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                }
            }
            Assert.That(totalMoves, Is.GreaterThan(300));
            Assert.That(campaign.Progress.AllCleared, Is.True);
            Assert.That(GameObject.Find("ClearTitle").GetComponent<TMP_Text>().text, Is.EqualTo("All stages complete!"));
            Assert.That(GameObject.Find("NextStageButton"), Is.Null);
            Assert.That(new PuzzleProgressStore(path, 30).Load().AllCleared, Is.True);
            GameObject.Find("ClearStageSelectButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(campaign.Selection.Buttons.All(b => b.Completed && !b.Locked), Is.True);
            Assert.That(GameObject.Find("NextStageLabel").GetComponent<TMP_Text>().text, Is.EqualTo("ALL STAGES COMPLETE"));
        }

        IEnumerator Solve()
        {
            int minimum = game.Stage.MinimumMoves;
            foreach (var move in game.Stage.GetSolution())
            {
                Assert.That(game.TryMove(move.from, move.to), Is.True);
                float limit = Time.realtimeSinceStartup + 2;
                while (game.IsMoving && Time.realtimeSinceStartup < limit) yield return null;
                Assert.That(game.IsMoving, Is.False);
            }
            Assert.That(game.IsCleared, Is.True);
            Assert.That(game.MoveCount, Is.EqualTo(minimum));
            Assert.That(game.Board.Matches(game.Stage.Target), Is.True);
        }
    }
}
