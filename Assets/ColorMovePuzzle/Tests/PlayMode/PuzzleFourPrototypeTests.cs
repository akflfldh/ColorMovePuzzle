using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;

namespace ColorMovePuzzle.Tests
{
    public sealed class PuzzleFourPrototypeTests
    {
        PuzzleGameController game;
        PuzzleCampaignController campaign;
        string path;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity");
            yield return null;
            game = Object.FindAnyObjectByType<PuzzleGameController>();
            campaign = Object.FindAnyObjectByType<PuzzleCampaignController>();
            path = Path.Combine(Application.temporaryCachePath, "ColorMovePuzzleTests", System.Guid.NewGuid() + ".json");
            campaign.UseProgressStore(new PuzzleProgressStore(path, 30));
            Object.FindAnyObjectByType<PuzzleMenuController>().UseProfileStore(new PuzzleMenuProfileStore(path + ".menu"));
            campaign.PlayStage(1);
#if UNITY_EDITOR
            game.LoadStage(UnityEditor.AssetDatabase.LoadAssetAtPath<PuzzleStageData>("Assets/ColorMovePuzzle/Data/FourByFour/Stage01.asset"));
#endif
        }
        [TearDown] public void Cleanup()
        {
            foreach (string suffix in new[] {"", ".bak", ".tmp", ".menu", ".menu.bak", ".menu.tmp"}) if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }
        [Test] public void FourPrototypeHasSixteenCellsAndExactInventory()
        {
            Assert.That(game.Board.Size, Is.EqualTo(4));
            var cells = game.Board.CopyCells();
            Assert.That(cells.Count(c => c == TileColor.Red), Is.EqualTo(3));
            Assert.That(cells.Count(c => c == TileColor.Blue), Is.EqualTo(3));
            Assert.That(cells.Count(c => c == TileColor.Yellow), Is.EqualTo(3));
            Assert.That(cells.Count(c => c == TileColor.Green), Is.EqualTo(3));
            Assert.That(cells.Count(c => c == TileColor.Black), Is.EqualTo(2));
            Assert.That(cells.Count(c => c == TileColor.Empty), Is.EqualTo(2));
            Assert.That(game.BoardView.transform.Find("Cells").childCount, Is.EqualTo(16));
            Assert.That(game.BoardView.Tiles.Count, Is.EqualTo(14));
            Assert.That(game.TargetView.transform.Find("Cells").childCount, Is.EqualTo(16));
            Assert.That(game.TargetView.Tiles.All(t => !t.Interactive), Is.True);
            foreach (var tile in game.BoardView.Tiles) Assert.That(tile.Rect.rect.width, Is.EqualTo(tile.Rect.rect.height).Within(0.01));
        }
        [UnityTest] public IEnumerator FourPrototypeTapAnimationLockAndRestartWork()
        {
            var initial = game.Board.CopyCells();
            int source = Enumerable.Range(0, game.Board.Count).First(i => game.Board.GetEmptyNeighbors(i).Length == 1);
            int destination = game.Board.GetEmptyNeighbors(source)[0];
            game.RequestTap(source);
            Assert.That(game.IsMoving, Is.True);
            Assert.That(game.MoveCount, Is.EqualTo(1));
            Assert.That(game.TryMove(destination, source), Is.False);
            game.Restart();
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(game.IsMoving, Is.False);
            Assert.That(game.MoveCount, Is.Zero);
            CollectionAssert.AreEqual(initial, game.Board.CopyCells());
            Assert.That(game.TryMove(source, destination), Is.True);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(game.IsMoving, Is.False);
        }
        [UnityTest] public IEnumerator FourPrototypeRecordedMovesReachClearAndReplay()
        {
            foreach (var move in game.Stage.GetSolution())
            {
                Assert.That(game.TryMove(move.from, move.to), Is.True);
                float until = Time.realtimeSinceStartup + 2;
                while (game.IsMoving && Time.realtimeSinceStartup < until) yield return null;
                Assert.That(game.IsMoving, Is.False);
            }
            Assert.That(game.IsCleared, Is.True);
            Assert.That(game.Board.Matches(game.Stage.Target), Is.True);
            Assert.That(campaign.Progress.ClearedCount, Is.Zero, "A standalone prototype must not alter 3x3 progress.");
            game.Restart();
            Assert.That(game.IsCleared, Is.False);
            Assert.That(game.MoveCount, Is.Zero);
        }

        [UnityTest] public IEnumerator ShortSwipeInsideTileSurvivesClickBeforeEndDrag()
        {
            Assert.That(game.TryMove(7,11), Is.True);
            yield return new WaitForSecondsRealtime(0.2f);
            var tile = game.BoardView.FindTile(11);
            var data = new PointerEventData(EventSystem.current) {pointerId = 42,
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null,tile.Rect.position)};
            ExecuteEvents.Execute(tile.gameObject,data,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(tile.gameObject,data,ExecuteEvents.beginDragHandler);
            data.dragging = true;
            data.position += Vector2.down * tile.Rect.rect.width * tile.transform.lossyScale.x * 0.45f;
            ExecuteEvents.Execute(tile.gameObject,data,ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(tile.gameObject,data,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(tile.gameObject,data,ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(tile.gameObject,data,ExecuteEvents.endDragHandler);
            Assert.That(game.MoveCount, Is.EqualTo(2));
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(game.Board[15], Is.EqualTo(TileColor.Green));
            Assert.That(game.IsMoving, Is.False);
        }
    }
}
