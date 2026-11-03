using System;
using System.IO;
using System.Linq;
using System.Collections;
using ColorMovePuzzle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class MobileTouchVerification
{
    const string Result="Artifacts/Android/MobileTouch.json";
    public static void Main()
    {
        var menu=UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>();
        string root=Path.GetFullPath("Artifacts/Android/Preview")+Path.DirectorySeparatorChar;
        if(!Path.GetFullPath(menu.ProfilePath).StartsWith(root,StringComparison.OrdinalIgnoreCase)||!Path.GetFullPath(menu.GetComponent<PuzzleCampaignController>().SavePath).StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new Exception("Use isolated AndroidPreview first.");
        File.WriteAllText(Result,"{\"passed\":false,\"running\":true}");menu.StartCoroutine(Verify(menu));
    }
    static IEnumerator Verify(PuzzleMenuController menu)
    {
        var flow=menu.GetComponent<PuzzleCampaignController>();var game=menu.GetComponent<PuzzleGameController>();
        string folder=Path.GetDirectoryName(menu.ProfilePath);
        var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;var oldBackground=InputSystem.settings.backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        var touch=InputSystem.AddDevice<Touchscreen>("AndroidEditorTouchVerification");var keyboard=InputSystem.AddDevice<Keyboard>("AndroidEditorBackVerification");
        var rows=new System.Collections.Generic.List<object>();int id=1,multiple=0;
        try
        {
            menu.ShowMainMenu();yield return Tap(touch,id++,(RectTransform)menu.MainMenu.SettingsButton.transform);
            if(menu.Screen!=PuzzleScreen.Settings)throw new Exception("Settings native tap failed.");
            yield return Slider(touch,id++,menu.Settings.BgmSlider,.2f);yield return Slider(touch,id++,menu.Settings.SfxSlider,.55f);
            if(Mathf.Abs(menu.Profile.BgmVolume-.2f)>.02f||Mathf.Abs(menu.Profile.SfxVolume-.55f)>.02f)throw new Exception("Volume native drag failed.");
            yield return Back(keyboard);if(menu.Screen!=PuzzleScreen.MainMenu)throw new Exception("Settings back failed.");
            for(int size=3;size<=5;size++)
            {
                yield return Tap(touch,id++,(RectTransform)menu.MainMenu.ModeButtons[size-3].transform);
                if(menu.Screen!=PuzzleScreen.StageSelection||flow.Catalog.GetStage(1).BoardSize!=size)throw new Exception("Mode native tap failed.");
                var scroll=flow.Selection.Scroll;yield return null;yield return null;
                var viewport=scroll.viewport;var corners=new Vector3[4];viewport.GetWorldCorners(corners);
                Vector2 scrollStart=RectTransformUtility.WorldToScreenPoint(null,viewport.position);
                float viewHeight=Vector3.Distance(corners[0],corners[1]);
                yield return Gesture(touch,id++,scrollStart+Vector2.down*viewHeight*.2f,scrollStart+Vector2.up*viewHeight*.3f);
                if(scroll.verticalNormalizedPosition>.98f)throw new Exception("Stage scrolling did not move.");
                scroll.StopMovement();flow.Selection.FocusStage(1);yield return null;yield return null;
                yield return Tap(touch,id++,(RectTransform)flow.Selection.Buttons[0].transform);
                if(game.Board.Size!=size||menu.Screen!=PuzzleScreen.Playing)throw new Exception("Stage native tap failed.");
                int single=Enumerable.Range(0,game.Board.Count).First(i=>game.Board.GetEmptyNeighbors(i).Length==1);
                yield return Tap(touch,id++,game.BoardView.FindTile(single).Rect);yield return Finished(game);
                if(game.MoveCount!=1)throw new Exception("Single empty tap failed.");
                yield return Tap(touch,id++,GameObject.Find("RestartButton").GetComponent<RectTransform>());
                if(game.MoveCount!=0)throw new Exception("Restart native tap failed.");
                var diagonalTile=game.BoardView.FindTile(single);Vector2 diagonalStart=RectTransformUtility.WorldToScreenPoint(null,diagonalTile.Rect.position);
                yield return Gesture(touch,id++,diagonalStart,diagonalStart+Vector2.one*diagonalTile.Rect.rect.width*diagonalTile.transform.lossyScale.x*.3f);
                if(game.MoveCount!=0)throw new Exception("Diagonal gesture was accepted.");
                if(size==5)
                {
                    yield return Tap(touch,id++,game.TargetView.Tiles[0].Rect);if(!game.GetComponent<PuzzleTargetZoom>().IsOpen)throw new Exception("Target zoom failed.");
                    yield return Back(keyboard);if(game.GetComponent<PuzzleTargetZoom>().IsOpen||menu.Screen!=PuzzleScreen.Playing)throw new Exception("Target zoom back failed.");
                }
                foreach(var move in game.Stage.GetSolution())
                {
                    if(game.Board.GetEmptyNeighbors(move.from).Length>1)
                    {
                        int before=game.MoveCount;yield return Tap(touch,id++,game.BoardView.FindTile(move.from).Rect);
                        if(game.MoveCount!=before)throw new Exception("Ambiguous tap moved without direction.");multiple++;
                    }
                    yield return Move(touch,id++,game,move);yield return Finished(game);
                    if(game.Board[move.from]!=TileColor.Empty)throw new Exception("Directional native solution failed.");
                }
                if(!game.IsCleared||!flow.Progress.IsUnlocked(2))throw new Exception("Clear/unlock failed.");
                rows.Add(new{size=size,nativeSolutionMoves=game.MoveCount,cleared=true,nextUnlocked=true,scrolled=true,diagonalRejected=true});
                yield return Tap(touch,id++,GameObject.Find("NextStageButton").GetComponent<RectTransform>());
                if(game.Stage.StageNumber!=2)throw new Exception("Next stage failed.");
                yield return Back(keyboard);if(menu.Screen!=PuzzleScreen.StageSelection)throw new Exception("Puzzle Android-style back failed.");
                yield return Back(keyboard);if(menu.Screen!=PuzzleScreen.MainMenu)throw new Exception("Selection Android-style back failed.");
            }
            if(multiple==0)throw new Exception("No ambiguous destination was exercised.");
            foreach(string file in new[]{"progress-v1.json","progress-4x4-v1.json","progress-5x5-v1.json"})
            {var p=new PuzzleProgressStore(Path.Combine(folder,file),30).Load();if(p.ClearedCount!=1||p.LastPlayed!=2)throw new Exception("Mode-independent saved progress failed.");}
            yield return Tap(touch,id++,(RectTransform)menu.MainMenu.ContinueButton.transform);
            if(game.Board.Size!=5||game.Stage.StageNumber!=2)throw new Exception("Continue last grid/stage failed.");
            int source=Enumerable.Range(0,25).First(i=>game.Board.GetEmptyNeighbors(i).Length>0);
            yield return Move(touch,id++,game,new PuzzleMove(source,game.Board.GetEmptyNeighbors(source)[0]));yield return Finished(game);
            if(game.MoveCount!=1)throw new Exception("Pending board move was not recorded for lifecycle test.");
            File.WriteAllText(Result,Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,running=false,environment="Unity Editor virtual Touchscreen and Keyboard, Android active target",modes=rows,
                ambiguousTapsChecked=multiple,sliders=true,bgm=menu.Profile.BgmVolume,sfx=menu.Profile.SfxVolume,backNavigation=true,targetZoom=true,continueGrid=5,continueStage=2,unfinishedMovesBeforeRestart=1,userSaveModified=false},Newtonsoft.Json.Formatting.Indented));
        }
        finally
        {
            if(File.ReadAllText(Result).Contains("\"running\":true"))File.WriteAllText(Result,"{\"passed\":false,\"running\":false}");
            InputSystem.RemoveDevice(touch);InputSystem.RemoveDevice(keyboard);InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;InputSystem.settings.backgroundBehavior=oldBackground;menu.ShowMainMenu();
        }
    }
    static IEnumerator Move(Touchscreen touch,int id,PuzzleGameController game,PuzzleMove move)
    {
        yield return null;yield return null;var tile=game.BoardView.FindTile(move.from);Vector2 start=RectTransformUtility.WorldToScreenPoint(null,tile.Rect.position);
        Vector2 direction=move.to==move.from+1?Vector2.right:move.to==move.from-1?Vector2.left:move.to>move.from?Vector2.down:Vector2.up;
        yield return Gesture(touch,id,start,start+direction*tile.Rect.rect.width*tile.transform.lossyScale.x*.45f);
    }
    static IEnumerator Finished(PuzzleGameController game){float end=Time.realtimeSinceStartup+2;while(game.IsMoving&&Time.realtimeSinceStartup<end)yield return null;if(game.IsMoving)throw new Exception("Move animation timed out.");}
    static IEnumerator Tap(Touchscreen touch,int id,RectTransform rect)
    {yield return null;yield return null;var p=RectTransformUtility.WorldToScreenPoint(null,rect.position);Send(touch,id,UnityEngine.InputSystem.TouchPhase.Began,p);yield return null;yield return null;Send(touch,id,UnityEngine.InputSystem.TouchPhase.Ended,p);yield return null;yield return null;}
    static IEnumerator Gesture(Touchscreen touch,int id,Vector2 start,Vector2 end)
    {Send(touch,id,UnityEngine.InputSystem.TouchPhase.Began,start);yield return null;yield return null;for(int i=1;i<=6;i++){Send(touch,id,UnityEngine.InputSystem.TouchPhase.Moved,Vector2.Lerp(start,end,i/6f));yield return null;yield return null;}Send(touch,id,UnityEngine.InputSystem.TouchPhase.Ended,end);yield return null;yield return null;}
    static IEnumerator Slider(Touchscreen touch,int id,UnityEngine.UI.Slider slider,float value)
    {yield return null;yield return null;Vector2 start=RectTransformUtility.WorldToScreenPoint(null,slider.handleRect.position);var area=(RectTransform)slider.handleRect.parent;
        Vector2 end=RectTransformUtility.WorldToScreenPoint(null,area.TransformPoint(new Vector3(Mathf.Lerp(area.rect.xMin,area.rect.xMax,value),0,0)));yield return Gesture(touch,id,start,end);}
    static IEnumerator Back(Keyboard keyboard){InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;}
    static void Send(Touchscreen d,int id,UnityEngine.InputSystem.TouchPhase phase,Vector2 p)=>InputSystem.QueueStateEvent(d,new TouchState{touchId=id,phase=phase,position=p});
}
