UnityEngine.Canvas.ForceUpdateCanvases();
var game = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleGameController>();
var flow = game.GetComponent<ColorMovePuzzle.PuzzleCampaignController>();
var tile = game.BoardView.Tiles[0];
var corners = new UnityEngine.Vector3[4];
tile.Rect.GetWorldCorners(corners);
float width = UnityEngine.Vector3.Distance(corners[0], corners[3]);
float height = UnityEngine.Vector3.Distance(corners[0], corners[1]);
game.BoardView.Rect.GetWorldCorners(corners);
var safe = UnityEngine.Screen.safeArea;
bool inside = true;
foreach (var p in corners) inside &= p.x >= safe.xMin - 1 && p.x <= safe.xMax + 1 && p.y >= safe.yMin - 1 && p.y <= safe.yMax + 1;
var snapshot = new {
    screenWidth = UnityEngine.Screen.width, screenHeight = UnityEngine.Screen.height,
    boardSize = game.Board.Size, slots = game.BoardView.transform.Find("Cells").childCount,
    tiles = game.BoardView.Tiles.Count, targetSlots = game.TargetView.transform.Find("Cells").childCount,
    tileWidth = width, tileHeight = height, square = UnityEngine.Mathf.Abs(width-height) < 0.1f,
    insideSafeArea = inside, exactMinimumClaimed = game.Stage.HasExactMinimum,
    rating = game.Stage.EstimatedRating, difficultyText = UnityEngine.GameObject.Find("TargetHint").GetComponent<TMPro.TMP_Text>().text,
    moveCount = game.MoveCount, savePath = flow.SavePath,
    inventory = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(System.Linq.Enumerable.Range(0,6), c => System.Linq.Enumerable.Count(game.Board.CopyCells(), x => (int)x == c)))
};
var json = Newtonsoft.Json.JsonConvert.SerializeObject(snapshot, Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText($"Artifacts/FourByFour/Layout-{UnityEngine.Screen.width}x{UnityEngine.Screen.height}.json", json);
if (!inside || game.Board.Size != 4 || game.BoardView.Tiles.Count != 14 || UnityEngine.Mathf.Abs(width-height) >= 0.1f || game.Stage.HasExactMinimum)
    throw new System.Exception("4x4 live layout/difficulty validation failed.");
return json;
