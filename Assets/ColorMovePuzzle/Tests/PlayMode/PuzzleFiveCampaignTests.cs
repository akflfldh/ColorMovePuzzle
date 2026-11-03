using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColorMovePuzzle.Tests
{
    public sealed class PuzzleFiveCampaignTests
    {
        PuzzleMenuController menu;PuzzleCampaignController campaign;PuzzleGameController game;string folder;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity");yield return null;
            folder=Path.Combine(Application.temporaryCachePath,"ColorMovePuzzleTests",Guid.NewGuid().ToString());Bind();menu.ShowMainMenu();
        }
        void Bind()
        {
            menu=UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>();campaign=menu.GetComponent<PuzzleCampaignController>();game=menu.GetComponent<PuzzleGameController>();
            campaign.UseProgressDirectory(folder);menu.UseProfileStore(new PuzzleMenuProfileStore(Path.Combine(folder,"menu.json")));
        }
        [TearDown] public void Cleanup()
        {if(Directory.Exists(folder)){foreach(string file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);}}
        [Test] public void FiveModeReusesSelectionControllerAndRejectsLockedStages()
        {
            Assert.That(menu.Screen,Is.EqualTo(PuzzleScreen.MainMenu));Assert.That(menu.MainMenu.ModeButtons.All(b=>b.Button.interactable),Is.True);
            menu.MainMenu.ModeButtons[2].Button.onClick.Invoke();Assert.That(menu.Screen,Is.EqualTo(PuzzleScreen.StageSelection));
            Assert.That(campaign.Catalog.Count,Is.EqualTo(30));Assert.That(campaign.Catalog.GetStage(1).BoardSize,Is.EqualTo(5));
            Assert.That(campaign.Selection.Buttons.Count,Is.EqualTo(30));Assert.That(campaign.Progress.IsUnlocked(1),Is.True);Assert.That(campaign.Progress.IsUnlocked(2),Is.False);
            campaign.Selection.Buttons[1].GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Assert.That(menu.Screen,Is.EqualTo(PuzzleScreen.StageSelection));
            Assert.That(campaign.PlayStage(30),Is.False);Assert.That(campaign.PlayStage(1),Is.True);
            Assert.That(game.BoardView.Size,Is.EqualTo(5));Assert.That(game.TargetView.Size,Is.EqualTo(5));Assert.That(game.BoardView.Tiles.Count,Is.EqualTo(22));
            Assert.That(game.BoardView.Tiles.All(t=>t.Interactive),Is.True);Assert.That(game.TargetView.Tiles.All(t=>!t.Interactive),Is.True);
            Assert.That(UnityEngine.Object.FindObjectsByType<MeshRenderer>(),Is.Empty);
        }
        [UnityTest] public IEnumerator AllThirtyFiveStagesClearThroughTileUIAndUnlockSequentially()
        {
            menu.SelectMode(5);float normal=game.MoveDuration;
            var duration=typeof(PuzzleGameController).GetField("moveDuration",BindingFlags.Instance|BindingFlags.NonPublic);
            var report=new System.Text.StringBuilder("stage,ui_moves,animation_seconds,cleared,next_unlocked\n");
            try
            {
                for(int number=1;number<=30;number++)
                {
                    Assert.That(campaign.PlayStage(number),Is.True);yield return null;
                    bool sample=number==1||number==15||number==30;float seconds=sample?normal:.001f;
                    duration.SetValue(game,seconds);
                    foreach(var move in game.Stage.GetSolution())
                    {
                        var tile=game.BoardView.FindTile(move.from);Assert.That(tile,Is.Not.Null);Assert.That(tile.Interactive,Is.True);
                        var data=new PointerEventData(EventSystem.current){pointerId=505,button=PointerEventData.InputButton.Left,
                            position=RectTransformUtility.WorldToScreenPoint(null,tile.Rect.position)};
                        ExecuteEvents.Execute(tile.gameObject,data,ExecuteEvents.pointerDownHandler);
                        ExecuteEvents.Execute(tile.gameObject,data,ExecuteEvents.beginDragHandler);data.dragging=true;
                        Vector2 direction=move.to==move.from+1?Vector2.right:move.to==move.from-1?Vector2.left:move.to>move.from?Vector2.down:Vector2.up;
                        data.position+=direction*tile.Rect.rect.width*tile.transform.lossyScale.x*.4f;
                        ExecuteEvents.Execute(tile.gameObject,data,ExecuteEvents.dragHandler);
                        ExecuteEvents.Execute(tile.gameObject,data,ExecuteEvents.pointerUpHandler);
                        ExecuteEvents.Execute(tile.gameObject,data,ExecuteEvents.pointerClickHandler);
                        ExecuteEvents.Execute(tile.gameObject,data,ExecuteEvents.endDragHandler);
                        Assert.That(game.IsMoving,Is.True,"Actual tile UI must dispatch its directional move.");
                        yield return Finished();Assert.That(game.Board[move.from],Is.EqualTo(TileColor.Empty));
                        Assert.That(Vector2.Distance(tile.Rect.anchoredPosition,game.BoardView.CellPosition(move.to)),Is.LessThan(.01f));
                    }
                    Assert.That(game.IsCleared,Is.True);Assert.That(game.Board.Matches(game.Stage.Target),Is.True);
                    Assert.That(game.MoveCount,Is.EqualTo(game.Stage.KnownSolutionLength));Assert.That(campaign.Progress.IsCleared(number),Is.True);
                    if(number<30)Assert.That(campaign.Progress.IsUnlocked(number+1),Is.True);
                    report.AppendLine($"{number},{game.MoveCount},{seconds},true,true");
                }
                Assert.That(campaign.Progress.AllCleared,Is.True);Assert.That(GameObject.Find("NextStageButton"),Is.Null);
                Assert.That(GameObject.Find("ClearTitle").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo("All stages complete!"));
#if UNITY_EDITOR
                Directory.CreateDirectory("Artifacts/FiveByFive");File.WriteAllText("Artifacts/FiveByFive/AllStagesUIPlay.csv",report.ToString());
#endif
            }
            finally{duration.SetValue(game,normal);}
        }
        [UnityTest] public IEnumerator FiveSaveContinueAndThreeIndependentProgressFilesSurviveNewScene()
        {
            foreach(string name in new[]{"progress-v1.json","progress-4x4-v1.json"})
            {var p=new PuzzleProgress(30);p.MarkCleared(1);p.MarkCleared(2);p.Select(3);new PuzzleProgressStore(Path.Combine(folder,name),30).Save(p);}
            string three=File.ReadAllText(Path.Combine(folder,"progress-v1.json")),four=File.ReadAllText(Path.Combine(folder,"progress-4x4-v1.json"));
            menu.SelectMode(5);campaign.PlayStage(1);yield return Solve();Assert.That(campaign.Progress.IsUnlocked(2),Is.True);
            GameObject.Find("NextStageButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Assert.That(game.Stage.StageNumber,Is.EqualTo(2));
            Assert.That(menu.Profile.LastGridSize,Is.EqualTo(5));menu.ShowMainMenu();Assert.That(menu.CanContinue,Is.True);
            yield return SceneManager.LoadSceneAsync("Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity");yield return null;Bind();menu.ShowMainMenu();
            Assert.That(menu.Continue(),Is.True);Assert.That(game.Stage.BoardSize,Is.EqualTo(5));Assert.That(game.Stage.StageNumber,Is.EqualTo(2));Assert.That(campaign.Progress.ClearedCount,Is.EqualTo(1));
            Assert.That(File.ReadAllText(Path.Combine(folder,"progress-v1.json")),Is.EqualTo(three));Assert.That(File.ReadAllText(Path.Combine(folder,"progress-4x4-v1.json")),Is.EqualTo(four));
            foreach(int size in new[]{3,4}){menu.ShowMainMenu();menu.SelectMode(size);Assert.That(campaign.Progress.ClearedCount,Is.EqualTo(2));Assert.That(campaign.Progress.LastPlayed,Is.EqualTo(3));}
        }
        [UnityTest] public IEnumerator ThreeAdjacentEmptyDirectionsAndDiagonalRejectionUseSameTileInput()
        {
            menu.SelectMode(5);campaign.PlayStage(1);var cells=game.Stage.Target;
            foreach(var pair in new[]{(7,22),(11,23),(13,24)}){var c=cells[pair.Item1];cells[pair.Item1]=cells[pair.Item2];cells[pair.Item2]=c;}
            var goal=new PuzzleBoard(5,cells);goal.TryMove(12,7);var custom=ScriptableObject.CreateInstance<PuzzleStageData>();
            custom.ConfigureGenerated(1,5,cells,goal.CopyCells(),1,new[]{new PuzzleMove(12,7)});
            try
            {
                foreach(var direction in new[]{(Vector2.up,7),(Vector2.left,11),(Vector2.right,13)})
                {
                    game.LoadStage(custom);game.RequestTap(12);Assert.That(game.MoveCount,Is.Zero);
                    game.RequestSwipe(12,Vector2.one*100);Assert.That(game.MoveCount,Is.Zero);
                    game.RequestSwipe(12,direction.Item1*100);Assert.That(game.MoveCount,Is.EqualTo(1));yield return Finished();
                    Assert.That(game.Board[direction.Item2],Is.EqualTo(cells[12]));Assert.That(game.Board[12],Is.EqualTo(TileColor.Empty));
                }
            }
            finally{UnityEngine.Object.Destroy(custom);}
        }
        [UnityTest] public IEnumerator AndroidEscapeClosesTargetBeforeReturningToSelectionAndMenu()
        {
            menu.SelectMode(5);campaign.PlayStage(1);var zoom=game.GetComponent<PuzzleTargetZoom>();
            var previousEditor=InputSystem.settings.editorInputBehaviorInPlayMode;var previousBackground=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;var keyboard=InputSystem.AddDevice<Keyboard>("FiveBackTest");
            try
            {
                yield return null;game.TargetView.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;
                Assert.That(zoom.IsOpen,Is.True);Assert.That(zoom.Enlarged.Tiles.Count,Is.EqualTo(22));Assert.That(zoom.Enlarged.Tiles.All(t=>!t.Interactive),Is.True);
                Assert.That(zoom.Enlarged.CellSize,Is.GreaterThan(game.TargetView.CellSize*2));
                yield return Back(keyboard);Assert.That(zoom.IsOpen,Is.False);Assert.That(menu.Screen,Is.EqualTo(PuzzleScreen.Playing));
                yield return Back(keyboard);Assert.That(menu.Screen,Is.EqualTo(PuzzleScreen.StageSelection));
                yield return Back(keyboard);Assert.That(menu.Screen,Is.EqualTo(PuzzleScreen.MainMenu));
            }
            finally{InputSystem.RemoveDevice(keyboard);InputSystem.settings.editorInputBehaviorInPlayMode=previousEditor;InputSystem.settings.backgroundBehavior=previousBackground;}
        }
        IEnumerator Back(Keyboard keyboard)
        {InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;}
        IEnumerator Solve(){foreach(var move in game.Stage.GetSolution()){Assert.That(game.TryMove(move.from,move.to),Is.True);yield return Finished();}Assert.That(game.IsCleared,Is.True);}
        IEnumerator Finished(){float end=Time.realtimeSinceStartup+2;while(game.IsMoving&&Time.realtimeSinceStartup<end)yield return null;Assert.That(game.IsMoving,Is.False);}
    }
}
