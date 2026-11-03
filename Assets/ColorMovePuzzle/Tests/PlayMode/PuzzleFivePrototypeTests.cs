using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColorMovePuzzle.Tests
{
    public sealed class PuzzleFivePrototypeTests
    {
        PuzzleGameController game;
        PuzzleCampaignController campaign;
        string folder;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity");yield return null;
            game=UnityEngine.Object.FindAnyObjectByType<PuzzleGameController>();campaign=UnityEngine.Object.FindAnyObjectByType<PuzzleCampaignController>();
            folder=Path.Combine(Application.temporaryCachePath,"ColorMovePuzzleTests",Guid.NewGuid().ToString());
            campaign.UseProgressDirectory(folder);
            UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>().UseProfileStore(new PuzzleMenuProfileStore(Path.Combine(folder,"menu.json")));
            campaign.PlayStage(1);
#if UNITY_EDITOR
            game.LoadStage(UnityEditor.AssetDatabase.LoadAssetAtPath<PuzzleStageData>("Assets/ColorMovePuzzle/Data/FiveByFive/Stage01.asset"));
#endif
        }
        [TearDown] public void Cleanup()
        {
            if(!Directory.Exists(folder))return;
            foreach(string file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);
        }
        [Test] public void FivePrototypeHas25Cells22Tiles3EmptiesAndReadOnlyTarget()
        {
            Assert.That(game.Board.Size,Is.EqualTo(5));Assert.That(game.Board.Count,Is.EqualTo(25));
            foreach(var c in new[]{TileColor.Red,TileColor.Blue,TileColor.Yellow,TileColor.Green,TileColor.Purple})
                Assert.That(game.Board.CopyCells().Count(x=>x==c),Is.EqualTo(4));
            Assert.That(game.Board.CopyCells().Count(x=>x==TileColor.Black),Is.EqualTo(2));
            Assert.That(game.Board.CopyCells().Count(x=>x==TileColor.Empty),Is.EqualTo(3));
            Assert.That(game.BoardView.transform.Find("Cells").childCount,Is.EqualTo(25));Assert.That(game.BoardView.Tiles.Count,Is.EqualTo(22));
            Assert.That(game.TargetView.Tiles.All(t=>!t.Interactive),Is.True);
            foreach(var tile in game.BoardView.Tiles)Assert.That(tile.Rect.rect.width,Is.EqualTo(tile.Rect.rect.height).Within(.01));
        }
        [UnityTest] public IEnumerator FivePrototypeTapLockRestartAndRecordedSolutionClear()
        {
            var initial=game.Board.CopyCells();int source=Enumerable.Range(0,25).First(i=>game.Board.GetEmptyNeighbors(i).Length==1);
            int to=game.Board.GetEmptyNeighbors(source)[0];game.RequestTap(source);
            Assert.That(game.IsMoving,Is.True);Assert.That(game.MoveCount,Is.EqualTo(1));Assert.That(game.TryMove(to,source),Is.False);
            game.Restart();yield return new WaitForSecondsRealtime(.2f);
            CollectionAssert.AreEqual(initial,game.Board.CopyCells());Assert.That(game.MoveCount,Is.Zero);
            foreach(var move in game.Stage.GetSolution())
            {
                Assert.That(game.TryMove(move.from,move.to),Is.True);
                float end=Time.realtimeSinceStartup+2;while(game.IsMoving&&Time.realtimeSinceStartup<end)yield return null;
                Assert.That(game.IsMoving,Is.False);
            }
            Assert.That(game.IsCleared,Is.True);Assert.That(game.Board.Matches(game.Stage.Target),Is.True);
            Assert.That(campaign.Progress.ClearedCount,Is.Zero);
            Assert.That(game.TryMove(0,1),Is.False);
            game.Restart();Assert.That(game.IsCleared,Is.False);Assert.That(game.MoveCount,Is.Zero);
        }
    }
}
