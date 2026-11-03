using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using ColorMovePuzzle;
using UnityEngine;

public static class MobileLayoutVerification
{
    public static void Main()
    {
        var menu=UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>();
        if(!Path.GetFullPath(menu.ProfilePath).StartsWith(Path.GetFullPath("Artifacts/Android/Preview")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Use isolated Android preview first.");
        File.WriteAllText("Artifacts/Android/MobileLayout.json","{\"passed\":false,\"running\":true}");menu.StartCoroutine(Verify(menu));
    }
    static IEnumerator Verify(PuzzleMenuController menu)
    {
        var flow=menu.GetComponent<PuzzleCampaignController>();var game=menu.GetComponent<PuzzleGameController>();var rows=new List<object>();
        try
        {
            foreach(var resolution in new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1080,2400)})
            {
                ColorMovePuzzle.Editor.PuzzleSceneBuilder.SetGameViewSize(resolution.x,resolution.y);yield return null;yield return null;yield return null;
                if(Screen.width!=resolution.x||Screen.height!=resolution.y)throw new Exception("Game View did not adopt requested resolution.");
                for(int size=3;size<=5;size++)
                {
                    menu.ShowMainMenu();if(!menu.SelectMode(size)||!flow.PlayStage(1))throw new Exception("Mode selection failed.");
                    yield return null;yield return null;Canvas.ForceUpdateCanvases();
                    var corners=new Vector3[4];var tile=game.BoardView.Tiles[0];tile.Rect.GetWorldCorners(corners);
                    float width=Vector3.Distance(corners[0],corners[3]),height=Vector3.Distance(corners[0],corners[1]);
                    bool square=Mathf.Abs(width-height)<.1f,inside=true;Rect safe=Screen.safeArea;
                    foreach(var view in new[]{game.BoardView,game.TargetView})
                    {view.Rect.GetWorldCorners(corners);foreach(var p in corners)inside&=p.x>=safe.xMin-1&&p.x<=safe.xMax+1&&p.y>=safe.yMin-1&&p.y<=safe.yMax+1;}
                    game.TargetView.Tiles[0].Rect.GetWorldCorners(corners);float targetCell=Vector3.Distance(corners[0],corners[3]);
                    int empties=game.Board.CopyCells().Count(c=>c==TileColor.Empty),expected=size==5?3:2;
                    bool correct=inside&&square&&game.Board.Count==size*size&&game.BoardView.Tiles.Count==size*size-expected&&empties==expected&&game.TargetView.Tiles.All(t=>!t.Interactive);
                    if(!correct)throw new Exception("Mobile board/target layout failed.");
                    rows.Add(new{width=Screen.width,height=Screen.height,grid=size,tileWidth=width,tileHeight=height,targetCellPixels=targetCell,square=square,insideReportedSafeArea=inside,cells=game.Board.Count,tiles=game.BoardView.Tiles.Count,empties=empties,targetReadOnly=true,zoomAvailable=size==5});
                }
            }
            File.WriteAllText("Artifacts/Android/MobileLayout.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,running=false,environment="Editor Game View; Screen.safeArea is the Editor-reported area, not a physical notch",cases=rows},Newtonsoft.Json.Formatting.Indented));
        }
        finally{ColorMovePuzzle.Editor.PuzzleSceneBuilder.SetGameViewSize(1080,1920);menu.ShowMainMenu();}
    }
}
