using System;
using System.IO;
using System.Collections;
using System.Linq;
using ColorMovePuzzle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class FiveTouchVerification
{
    public static void Main()
    {
        var menu=UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>();var flow=menu.GetComponent<PuzzleCampaignController>();
        string prefix=Path.GetFullPath("Artifacts/FiveByFive/Preview")+Path.DirectorySeparatorChar;
        if(!Path.GetFullPath(flow.SavePath).StartsWith(prefix,StringComparison.OrdinalIgnoreCase)||!Path.GetFullPath(menu.ProfilePath).StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new Exception("Use isolated FivePreview first.");
        File.WriteAllText("Artifacts/FiveByFive/NativeTouch.json","{\"passed\":false,\"running\":true}");menu.StartCoroutine(Verify(menu));
    }
    static IEnumerator Verify(PuzzleMenuController menu)
    {
        var flow=menu.GetComponent<PuzzleCampaignController>();var game=menu.GetComponent<PuzzleGameController>();
        string folder=Path.GetDirectoryName(menu.ProfilePath),three=File.ReadAllText(Path.Combine(folder,"progress-v1.json")),four=File.ReadAllText(Path.Combine(folder,"progress-4x4-v1.json"));
        var previousEditor=InputSystem.settings.editorInputBehaviorInPlayMode;var previousBackground=InputSystem.settings.backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        var touch=InputSystem.AddDevice<Touchscreen>("FiveTouchVerification");
        try
        {
            menu.ShowMainMenu();yield return Tap(touch,1,(RectTransform)menu.MainMenu.ModeButtons[2].transform);
            if(menu.Screen!=PuzzleScreen.StageSelection||flow.Catalog.GetStage(1).BoardSize!=5)throw new Exception("Native 5x5 mode button failed.");
            yield return Tap(touch,2,(RectTransform)flow.Selection.Buttons[0].transform);
            if(game.Board.Size!=5)throw new Exception("Native stage card failed.");
            yield return Tap(touch,3,game.TargetView.Tiles[0].Rect);
            var zoom=game.GetComponent<PuzzleTargetZoom>();if(!zoom.IsOpen||game.MoveCount!=0)throw new Exception("Native read-only target zoom failed.");
            yield return Tap(touch,4,GameObject.Find("CloseTargetButton").GetComponent<RectTransform>());
            if(zoom.IsOpen)throw new Exception("Native target close failed.");
            int from=Enumerable.Range(0,25).First(i=>game.Board.GetEmptyNeighbors(i).Length==1),to=game.Board.GetEmptyNeighbors(from)[0];
            yield return Tap(touch,5,game.BoardView.FindTile(from).Rect);yield return Finished(game);
            if(game.MoveCount!=1||game.Board[from]!=TileColor.Empty)throw new Exception("Native single-choice tap failed.");
            yield return Tap(touch,6,GameObject.Find("RestartButton").GetComponent<RectTransform>());
            if(game.MoveCount!=0)throw new Exception("Native restart failed.");
            // Follow every recorded move via native touchscreen events, not controller calls.
            int id=10;
            foreach(var move in game.Stage.GetSolution())
            {
                var tile=game.BoardView.FindTile(move.from);yield return null;yield return null;
                Vector2 start=RectTransformUtility.WorldToScreenPoint(null,tile.Rect.position);
                Vector2 direction=move.to==move.from+1?Vector2.right:move.to==move.from-1?Vector2.left:move.to>move.from?Vector2.down:Vector2.up;
                Vector2 end=start+direction*tile.Rect.rect.width*tile.transform.lossyScale.x*.45f;
                Send(touch,id,UnityEngine.InputSystem.TouchPhase.Began,start);yield return null;yield return null;
                for(int step=1;step<=4;step++){Send(touch,id,UnityEngine.InputSystem.TouchPhase.Moved,Vector2.Lerp(start,end,step/4f));yield return null;yield return null;}
                Send(touch,id++,UnityEngine.InputSystem.TouchPhase.Ended,end);yield return null;yield return null;yield return Finished(game);
                if(game.Board[move.from]!=TileColor.Empty||tile.CellIndex!=move.to)throw new Exception("Native solution swipe failed.");
            }
            if(!game.IsCleared||!flow.Progress.IsUnlocked(2))throw new Exception("Native clear/unlock failed.");
            yield return Tap(touch,id++,GameObject.Find("NextStageButton").GetComponent<RectTransform>());
            if(game.Stage.StageNumber!=2||menu.Profile.LastGridSize!=5)throw new Exception("Native next/history failed.");
            yield return Tap(touch,id++,GameObject.Find("StageSelectButton").GetComponent<RectTransform>());
            yield return Tap(touch,id++,GameObject.Find("MainMenuBackButton").GetComponent<RectTransform>());
            yield return Tap(touch,id++,(RectTransform)menu.MainMenu.ContinueButton.transform);
            if(game.Stage.BoardSize!=5||game.Stage.StageNumber!=2)throw new Exception("Native Continue failed.");
            if(File.ReadAllText(Path.Combine(folder,"progress-v1.json"))!=three||File.ReadAllText(Path.Combine(folder,"progress-4x4-v1.json"))!=four)throw new Exception("Older progress changed.");
            File.WriteAllText("Artifacts/FiveByFive/NativeTouch.json","{\"passed\":true,\"running\":false,\"modeAndStageSelection\":true,\"targetZoom\":true,\"tap\":true,\"nativeSolutionSwipes\":true,\"restart\":true,\"clearUnlockNext\":true,\"continue5\":true,\"oldProgressUnchanged\":true,\"userSaveModified\":false}");
        }
        finally
        {
            if(File.ReadAllText("Artifacts/FiveByFive/NativeTouch.json").Contains("\"running\":true"))File.WriteAllText("Artifacts/FiveByFive/NativeTouch.json","{\"passed\":false,\"running\":false}");
            InputSystem.RemoveDevice(touch);InputSystem.settings.editorInputBehaviorInPlayMode=previousEditor;InputSystem.settings.backgroundBehavior=previousBackground;menu.ShowMainMenu();
        }
    }
    static IEnumerator Finished(PuzzleGameController game)
    {float end=Time.realtimeSinceStartup+2;while(game.IsMoving&&Time.realtimeSinceStartup<end)yield return null;if(game.IsMoving)throw new Exception("Animation timed out.");}
    static IEnumerator Tap(Touchscreen d,int id,RectTransform rect)
    {yield return null;yield return null;var p=RectTransformUtility.WorldToScreenPoint(null,rect.position);Send(d,id,UnityEngine.InputSystem.TouchPhase.Began,p);yield return null;yield return null;Send(d,id,UnityEngine.InputSystem.TouchPhase.Ended,p);yield return null;yield return null;}
    static void Send(Touchscreen d,int id,UnityEngine.InputSystem.TouchPhase phase,Vector2 p)=>InputSystem.QueueStateEvent(d,new TouchState{touchId=id,phase=phase,position=p});
}
