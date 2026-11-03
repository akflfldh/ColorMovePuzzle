UnityEngine.Canvas.ForceUpdateCanvases();
var game = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleGameController>();
var board = game.BoardView;
var corners = new UnityEngine.Vector3[4];
board.Rect.GetWorldCorners(corners);
var safe = UnityEngine.Screen.safeArea;
bool inside = true;
foreach (var corner in corners)
    inside &= safe.Contains(new UnityEngine.Vector2(corner.x, corner.y));
var content = UnityEngine.GameObject.Find("Content").GetComponent<UnityEngine.RectTransform>();
content.GetWorldCorners(corners);
bool contentInside = true;
foreach (var corner in corners)
    contentInside &= corner.x >= safe.xMin - 1 && corner.x <= safe.xMax + 1
        && corner.y >= safe.yMin - 1 && corner.y <= safe.yMax + 1;
var tile = board.Tiles[0];
tile.Rect.GetWorldCorners(corners);
float width = UnityEngine.Vector3.Distance(corners[0], corners[3]);
float height = UnityEngine.Vector3.Distance(corners[0], corners[1]);
var snapshot = new {
    width = UnityEngine.Screen.width, height = UnityEngine.Screen.height,
    frame = UnityEngine.Time.frameCount, boardInsideSafeArea = inside, contentInsideSafeArea = contentInside,
    tileScreenWidth = width, tileScreenHeight = height, square = UnityEngine.Mathf.Abs(width - height) < 0.1f,
    moves = game.MoveCount, cleared = game.IsCleared, moving = game.IsMoving,
    slots = board.transform.Find("Cells").childCount, tiles = board.Tiles.Count,
    cells = System.Array.ConvertAll(game.Board.CopyCells(), color => color.ToString())
};
var json = Newtonsoft.Json.JsonConvert.SerializeObject(snapshot, Newtonsoft.Json.Formatting.Indented);
System.IO.Directory.CreateDirectory("Artifacts/Verification");
System.IO.File.WriteAllText($"Artifacts/Verification/Layout-{UnityEngine.Screen.width}x{UnityEngine.Screen.height}.json", json);
if (!inside || !contentInside || UnityEngine.Mathf.Abs(width - height) >= 0.1f)
    throw new System.Exception("Responsive layout validation failed.");
return json;
