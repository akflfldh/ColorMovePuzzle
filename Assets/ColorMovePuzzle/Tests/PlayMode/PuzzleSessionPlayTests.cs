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
    public sealed class PuzzleSessionPlayTests
    {
        PuzzleMenuController menu;
        PuzzleCampaignController campaign;
        PuzzleGameController game;
        string folder;
        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "ColorMovePuzzleTests", Guid.NewGuid().ToString());
            yield return Reload();
        }
        IEnumerator Reload()
        {
            yield return SceneManager.LoadSceneAsync("Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity");
            yield return null;
            menu = UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>();
            campaign = UnityEngine.Object.FindAnyObjectByType<PuzzleCampaignController>();
            game = UnityEngine.Object.FindAnyObjectByType<PuzzleGameController>();
            campaign.UseProgressDirectory(folder);
            menu.UseProfileStore(new PuzzleMenuProfileStore(Path.Combine(folder, "menu.json")));
            menu.ShowMainMenu();
        }
        [TearDown] public void Cleanup()
        {
            if (!Directory.Exists(folder)) return;
            foreach (string file in Directory.GetFiles(folder)) File.Delete(file);
            Directory.Delete(folder);
        }
        IEnumerator Finished()
        {
            float end = Time.realtimeSinceStartup + 2;
            while (game.IsMoving && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(game.IsMoving, Is.False);
        }
        [UnityTest] public IEnumerator ThreeByThreeRestoresAndCanStillClear() { yield return RoundTrip(3); }
        [UnityTest] public IEnumerator FourByFourRestoresAndCanStillClear() { yield return RoundTrip(4); }
        [UnityTest] public IEnumerator FiveByFiveRestoresAndCanStillClear() { yield return RoundTrip(5); }
        IEnumerator RoundTrip(int size)
        {
            Assert.That(menu.SelectMode(size), Is.True); Assert.That(campaign.PlayStage(1), Is.True);
            var solution = game.Stage.GetSolution(); int count = size == 3 ? 1 : 3;
            foreach (var move in solution.Take(count))
            {
                Assert.That(game.TryMove(move.from, move.to), Is.True);
                // Assert persistence while the tile is still animating, without a lifecycle callback.
                Assert.That(new PuzzleSessionStore(campaign.SessionPath).TryLoad(game.Stage, out var disk), Is.True);
                CollectionAssert.AreEqual(game.Board.CopyCells(), disk.Cells);
                Assert.That(disk.MoveCount, Is.EqualTo(game.MoveCount));
                yield return Finished();
            }
            var cells = game.Board.CopyCells();
            yield return Reload();
            Assert.That(menu.Continue(), Is.True);
            Assert.That(game.Stage.BoardSize, Is.EqualTo(size)); Assert.That(game.Stage.StageNumber, Is.EqualTo(1));
            Assert.That(game.MoveCount, Is.EqualTo(count)); Assert.That(game.IsMoving, Is.False);
            CollectionAssert.AreEqual(cells, game.Board.CopyCells());
            Assert.That(GameObject.Find("MoveCount").GetComponent<TMP_Text>().text, Is.EqualTo(count.ToString()));
            foreach (var tile in game.BoardView.Tiles)
                Assert.That(Vector2.Distance(tile.Rect.anchoredPosition, game.BoardView.CellPosition(tile.CellIndex)), Is.LessThan(.01f));
            foreach (var move in solution.Skip(count))
            { Assert.That(game.TryMove(move.from, move.to), Is.True); yield return Finished(); }
            Assert.That(game.IsCleared, Is.True); Assert.That(game.InputEnabled, Is.True);
            Assert.That(campaign.Progress.IsUnlocked(2), Is.True);
            Assert.That(new PuzzleSessionStore(campaign.SessionPath).TryLoad(game.Stage, out _), Is.False);
            game.Restart();
            Assert.That(game.IsCleared, Is.False); Assert.That(game.MoveCount, Is.Zero);
            Assert.That(new PuzzleSessionStore(campaign.SessionPath).TryLoad(game.Stage, out var restart), Is.True);
            CollectionAssert.AreEqual(game.Stage.CreateStartingBoard().CopyCells(), restart.Cells);
        }
        [UnityTest] public IEnumerator RestartDuringAnimationPersistsZeroAndInvalidInputDoesNotWrite()
        {
            menu.SelectMode(5); campaign.PlayStage(1); var first = game.Stage.GetSolution()[0];
            int writes = campaign.SessionWriteCount;
            Assert.That(game.TryMove(first.from, first.to), Is.True);
            Assert.That(campaign.SessionWriteCount, Is.EqualTo(writes + 1));
            Assert.That(game.TryMove(first.to, first.from), Is.False);
            Assert.That(campaign.SessionWriteCount, Is.EqualTo(writes + 1));
            game.Restart();
            var cells = game.Board.CopyCells();
            int cleanWrites = campaign.SessionWriteCount;
            campaign.SendMessage("OnApplicationPause", true); campaign.SendMessage("OnApplicationFocus", false);
            Assert.That(campaign.SessionWriteCount, Is.EqualTo(cleanWrites), "Clean lifecycle callbacks should not rewrite files.");
            yield return Reload(); menu.Continue();
            Assert.That(game.MoveCount, Is.Zero); CollectionAssert.AreEqual(cells, game.Board.CopyCells());
        }
        [UnityTest] public IEnumerator LeavingDuringAnimationPreservesCommittedStateForContinueButExplicitSelectionRestarts()
        {
            menu.SelectMode(5); campaign.PlayStage(1); var move = game.Stage.GetSolution()[0];
            game.TryMove(move.from, move.to); var cells = game.Board.CopyCells();
            Assert.That(menu.HandleBack(), Is.True); Assert.That(menu.HandleBack(), Is.True);
            Assert.That(menu.Continue(), Is.True);
            CollectionAssert.AreEqual(cells, game.Board.CopyCells()); Assert.That(game.MoveCount, Is.EqualTo(1));
            campaign.ShowSelection(); campaign.PlayStage(1);
            Assert.That(game.MoveCount, Is.Zero);
            CollectionAssert.AreEqual(game.Stage.CreateStartingBoard().CopyCells(), game.Board.CopyCells());
            yield return null;
        }
        [UnityTest] public IEnumerator ModesKeepIndependentSnapshotsAndLegacyUnlockSettingsSurvive()
        {
            foreach (int size in new[] {3, 4, 5})
            {
                menu.SelectMode(size); campaign.PlayStage(1);
                var move = game.Stage.GetSolution()[0]; game.TryMove(move.from, move.to); yield return Finished();
                menu.ShowMainMenu();
            }
            yield return Reload(); Assert.That(menu.Continue(), Is.True); Assert.That(game.Stage.BoardSize, Is.EqualTo(5));
            foreach (int size in new[] {3, 4, 5})
            {
                menu.SelectMode(size); Assert.That(campaign.PlayStage(1, resume: true), Is.True);
                Assert.That(game.MoveCount, Is.EqualTo(1)); Assert.That(campaign.Progress.ClearedCount, Is.Zero);
                Assert.That(new PuzzleProgressStore(campaign.SavePath, 30).Load().IsUnlocked(2), Is.False);
            }
        }
        [UnityTest] public IEnumerator LastMoveSavedBeforeAnimationCanRecoverCompletionThenNextStageStartsFresh()
        {
            menu.SelectMode(3); campaign.PlayStage(1);
            var solution = game.Stage.GetSolution();
            foreach (var move in solution.Take(solution.Length - 1))
            { game.TryMove(move.from, move.to); yield return Finished(); }
            var last = solution.Last(); game.TryMove(last.from, last.to);
            game.StopAllCoroutines(); // Simulate termination before animation completion/StageCleared.
            Assert.That(game.IsCleared, Is.False); Assert.That(campaign.Progress.ClearedCount, Is.Zero);
            yield return Reload(); Assert.That(menu.Continue(), Is.True);
            Assert.That(game.IsCleared, Is.True); Assert.That(game.MoveCount, Is.EqualTo(solution.Length));
            Assert.That(campaign.Progress.IsCleared(1), Is.True); Assert.That(game.TryMove(last.to, last.from), Is.False);
            Assert.That(new PuzzleSessionStore(campaign.SessionPath).TryLoad(game.Stage, out _), Is.False);
            campaign.PlayNext(); Assert.That(game.Stage.StageNumber, Is.EqualTo(2)); Assert.That(game.MoveCount, Is.Zero);
            yield return Reload(); menu.Continue(); Assert.That(game.Stage.StageNumber, Is.EqualTo(2)); Assert.That(game.MoveCount, Is.Zero);
        }
        [UnityTest] public IEnumerator CorruptSessionFallsBackToStartWithoutTouchingLegacyUnlocksOrVolumes()
        {
            menu.SelectMode(3); campaign.PlayStage(1);
            foreach (var move in game.Stage.GetSolution()) { game.TryMove(move.from, move.to); yield return Finished(); }
            campaign.PlayStage(2); string progress = File.ReadAllText(campaign.SavePath);
            menu.ShowMainMenu(); menu.OpenSettings(); menu.Settings.BgmSlider.value = .31f; menu.Settings.SfxSlider.value = .62f;
            string profile = File.ReadAllText(menu.ProfilePath);
            File.WriteAllText(campaign.SessionPath, "{bad"); File.WriteAllText(campaign.SessionPath + ".bak", "{bad");
            yield return Reload(); Assert.That(menu.Continue(), Is.True);
            Assert.That(game.Stage.StageNumber, Is.EqualTo(2)); Assert.That(game.MoveCount, Is.Zero);
            CollectionAssert.AreEqual(game.Stage.CreateStartingBoard().CopyCells(), game.Board.CopyCells());
            Assert.That(campaign.Progress.IsCleared(1), Is.True);
            Assert.That(File.ReadAllText(campaign.SavePath), Is.EqualTo(progress));
            Assert.That(File.ReadAllText(menu.ProfilePath), Is.EqualTo(profile));
            Assert.That(menu.Profile.BgmVolume, Is.EqualTo(.31f).Within(.001));
            Assert.That(menu.Profile.SfxVolume, Is.EqualTo(.62f).Within(.001));
        }
        [UnityTest] public IEnumerator FailedMoveSaveRetriesOnPauseWithoutCorruptingPreviousSnapshot()
        {
            menu.SelectMode(5); campaign.PlayStage(1);
            string good = File.ReadAllText(campaign.SessionPath);
            Directory.CreateDirectory(campaign.SessionPath + ".tmp");
            var move = game.Stage.GetSolution()[0]; game.TryMove(move.from, move.to);
            Assert.That(File.ReadAllText(campaign.SessionPath), Is.EqualTo(good));
            Assert.That(campaign.SessionError, Is.Not.Empty);
            Directory.Delete(campaign.SessionPath + ".tmp");
            campaign.SendMessage("OnApplicationPause", true);
            Assert.That(new PuzzleSessionStore(campaign.SessionPath).TryLoad(game.Stage, out var saved), Is.True);
            CollectionAssert.AreEqual(game.Board.CopyCells(), saved.Cells); Assert.That(saved.MoveCount, Is.EqualTo(1));
            yield return Finished();
        }
    }
}
