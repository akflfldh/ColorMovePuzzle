UnityEngine.Canvas.ForceUpdateCanvases();
var game=UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleGameController>();
var corners=new UnityEngine.Vector3[4];var tile=game.BoardView.Tiles[0];tile.Rect.GetWorldCorners(corners);
float width=UnityEngine.Vector3.Distance(corners[0],corners[3]),height=UnityEngine.Vector3.Distance(corners[0],corners[1]);
var safe=UnityEngine.Screen.safeArea;bool inside=true;
foreach(var view in new[]{game.BoardView,game.TargetView})
{view.Rect.GetWorldCorners(corners);foreach(var p in corners)inside&=p.x>=safe.xMin-1&&p.x<=safe.xMax+1&&p.y>=safe.yMin-1&&p.y<=safe.yMax+1;}
var snapshot=new {screenWidth=UnityEngine.Screen.width,screenHeight=UnityEngine.Screen.height,size=game.Board.Size,
    cells=game.BoardView.transform.Find("Cells").childCount,tiles=game.BoardView.Tiles.Count,
    empties=System.Linq.Enumerable.Count(game.Board.CopyCells(),c=>c==ColorMovePuzzle.TileColor.Empty),
    tileWidth=width,tileHeight=height,square=UnityEngine.Mathf.Abs(width-height)<.1f,insideSafeArea=inside,
    targetReadOnly=System.Linq.Enumerable.All(game.TargetView.Tiles,t=>!t.Interactive),frame=UnityEngine.Time.frameCount};
string json=Newtonsoft.Json.JsonConvert.SerializeObject(snapshot,Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText($"Artifacts/FiveByFive/Layout-{UnityEngine.Screen.width}x{UnityEngine.Screen.height}.json",json);
if(!inside||snapshot.size!=5||snapshot.cells!=25||snapshot.tiles!=22||snapshot.empties!=3||!snapshot.square||!snapshot.targetReadOnly)throw new System.Exception("5x5 live layout failed.");
return json;
