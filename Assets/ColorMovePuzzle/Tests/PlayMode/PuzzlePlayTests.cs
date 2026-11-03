using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColorMovePuzzle.Tests
{
    public sealed class PuzzlePlayTests
    {
        PuzzleGameController game;
        string testSave;

        [UnitySetUp] public IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync("Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity");
            yield return null;
            game = Object.FindAnyObjectByType<PuzzleGameController>();
            Assert.That(game, Is.Not.Null);
            Assert.That(game.Board, Is.Not.Null);
            var legacy = game.Stage;
            var campaign = Object.FindAnyObjectByType<PuzzleCampaignController>();
            testSave = System.IO.Path.Combine(Application.temporaryCachePath, "ColorMovePuzzleTests", System.Guid.NewGuid() + ".json");
            campaign.UseProgressStore(new PuzzleProgressStore(testSave, 30));
            Object.FindAnyObjectByType<PuzzleMenuController>().UseProfileStore(new PuzzleMenuProfileStore(testSave + ".menu"));
            Assert.That(campaign.PlayStage(1), Is.True);
            game.LoadStage(legacy); // Keep the original board as a regression fixture.
        }

        [TearDown] public void CleanSave()
        {
            foreach (string suffix in new[] {"", ".bak", ".tmp", ".menu", ".menu.bak", ".menu.tmp"})
                if (System.IO.File.Exists(testSave + suffix)) System.IO.File.Delete(testSave + suffix);
        }

        [Test] public void SceneHasTwoUIGridsAndNo3DComponents()
        {
            Assert.That(Object.FindObjectsByType<EventSystem>().Length, Is.EqualTo(1));
            var canvas = Object.FindAnyObjectByType<Canvas>();
            Assert.That(canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>(), Is.Not.Null);
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080, 1920)));
            Assert.That(scaler.uiScaleMode, Is.EqualTo(UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(game.BoardView.transform.Find("Cells").childCount, Is.EqualTo(9));
            Assert.That(game.BoardView.Tiles.Count, Is.EqualTo(7));
            Assert.That(Object.FindObjectsByType<MeshRenderer>(), Is.Empty);
            Assert.That(Object.FindObjectsByType<MeshFilter>(), Is.Empty);
            Assert.That(Object.FindObjectsByType<Rigidbody>(), Is.Empty);
            Assert.That(Object.FindObjectsByType<Collider>(), Is.Empty);
            foreach (var tile in game.BoardView.Tiles)
            {
                Assert.That(tile.Rect.rect.width, Is.EqualTo(tile.Rect.rect.height).Within(0.01));
                Assert.That(tile.Interactive, Is.True);
            }
            var preview = GameObject.Find("TargetGrid").GetComponent<PuzzleBoardView>();
            Assert.That(preview.transform.Find("Cells").childCount, Is.EqualTo(9));
            Assert.That(preview.Tiles.All(t => !t.Interactive), Is.True);
        }

        [UnityTest] public IEnumerator TapUsesRaycastTileAndAnimatesExactlyOneMove()
        {
            var move = game.Stage.GetSolution()[0];
            var tile = game.BoardView.FindTile(move.from);
            var pointer = Pointer(tile, 7);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Any(h => h.gameObject == tile.gameObject), Is.True, "Tile must be hit by GraphicRaycaster.");
            Vector2 start = tile.Rect.anchoredPosition;
            ExecuteEvents.Execute(tile.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(tile.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(tile.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(game.IsMoving, Is.True);
            Assert.That(game.MoveCount, Is.EqualTo(1));
            Assert.That(game.TryMove(move.to, move.from), Is.False, "Lock input during animation.");
            yield return null;
            Assert.That(tile.Rect.anchoredPosition, Is.Not.EqualTo(start));
            yield return Finished();
            Assert.That(Vector2.Distance(tile.Rect.anchoredPosition, game.BoardView.CellPosition(move.to)), Is.LessThan(0.01f));
            Assert.That(game.MoveCount, Is.EqualTo(1));
            Assert.That(game.TryMove(move.to, move.from), Is.True, "Input must unlock after animation.");
            yield return Finished();
        }

        [UnityTest] public IEnumerator AmbiguousTapWaitsForDirectionalDrag()
        {
            Assert.That(game.TryMove(4, 5), Is.True);
            yield return Finished();
            var tile = game.BoardView.FindTile(3);
            game.RequestTap(3);
            Assert.That(game.MoveCount, Is.EqualTo(1));
            var pointer = Pointer(tile, 9);
            ExecuteEvents.Execute(tile.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(tile.gameObject, pointer, ExecuteEvents.beginDragHandler);
            pointer.position += Vector2.down * (tile.Rect.rect.width * tile.transform.lossyScale.x * 0.4f);
            ExecuteEvents.Execute(tile.gameObject, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(tile.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(tile.gameObject, pointer, ExecuteEvents.endDragHandler);
            Assert.That(game.MoveCount, Is.EqualTo(2));
            yield return Finished();
            Assert.That(tile.CellIndex, Is.EqualTo(6));
            Assert.That(game.Board[3], Is.EqualTo(TileColor.Empty));
        }

        [Test] public void InvalidAndDiagonalInputDoNothing()
        {
            var initial = game.Board.CopyCells();
            Assert.That(game.TryMove(4, 6), Is.False);
            game.RequestSwipe(4, new Vector2(100, 100));
            game.RequestTap(0);
            game.RequestTap(5);
            CollectionAssert.AreEqual(initial, game.Board.CopyCells());
            Assert.That(game.MoveCount, Is.Zero);
        }

        [UnityTest] public IEnumerator RestartButtonCancelsAnimationAndRestoresStart()
        {
            var initial = game.Board.CopyCells();
            Assert.That(game.TryMove(4, 5), Is.True);
            Assert.That(game.IsMoving, Is.True);
            var button = GameObject.Find("RestartButton");
            ExecuteEvents.Execute(button, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            Assert.That(game.MoveCount, Is.Zero);
            Assert.That(game.IsMoving, Is.False);
            Assert.That(game.IsCleared, Is.False);
            yield return new WaitForSecondsRealtime(0.2f);
            CollectionAssert.AreEqual(initial, game.Board.CopyCells());
            foreach (var tile in game.BoardView.Tiles)
                Assert.That(Vector2.Distance(tile.Rect.anchoredPosition, game.BoardView.CellPosition(tile.CellIndex)), Is.LessThan(0.01f));
        }

        [UnityTest] public IEnumerator FullSolutionClearsLocksAndPlayAgainRestarts()
        {
            var initial = game.Board.CopyCells();
            foreach (var move in game.Stage.GetSolution())
            {
                Assert.That(game.TryMove(move.from, move.to), Is.True);
                yield return Finished();
            }
            Assert.That(game.IsCleared, Is.True);
            Assert.That(game.Board.Matches(game.Stage.Target), Is.True);
            Assert.That(game.MoveCount, Is.EqualTo(game.Stage.GetSolution().Length));
            Assert.That(GameObject.Find("ClearOverlay"), Is.Not.Null);
            Assert.That(GameObject.Find("ClearMoveCount").GetComponent<TMPro.TMP_Text>().text,
                Does.Contain(game.MoveCount.ToString()));
            Assert.That(game.TryMove(4, 7), Is.False);
            ExecuteEvents.Execute(GameObject.Find("PlayAgainButton"), new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            Assert.That(game.IsCleared, Is.False);
            Assert.That(game.MoveCount, Is.Zero);
            Assert.That(GameObject.Find("ClearOverlay"), Is.Null);
            CollectionAssert.AreEqual(initial, game.Board.CopyCells());
        }

        [UnityTest] public IEnumerator RapidSecondPointerCannotDoubleMove()
        {
            var first = game.BoardView.FindTile(4);
            var second = game.BoardView.FindTile(3);
            foreach (var pair in new[] {first, second})
            {
                var pointer = Pointer(pair, pair == first ? 1 : 2);
                ExecuteEvents.Execute(pair.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(pair.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            }
            Assert.That(game.MoveCount, Is.EqualTo(1));
            yield return Finished();
        }

        PointerEventData Pointer(PuzzleTileView tile, int id)
        {
            return new PointerEventData(EventSystem.current) {pointerId = id,
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, tile.Rect.position)};
        }

        IEnumerator Finished()
        {
            float until = Time.realtimeSinceStartup + 2;
            while (game.IsMoving && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(game.IsMoving, Is.False, "Animation did not finish within two seconds.");
        }
    }
}
