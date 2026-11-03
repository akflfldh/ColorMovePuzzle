UnityEngine.Canvas.ForceUpdateCanvases();
var flow = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleCampaignController>();
var scroll = flow.Selection.Scroll;
var bounds = new UnityEngine.Vector3[4];
var design = UnityEngine.GameObject.Find("SelectionContent").GetComponent<UnityEngine.RectTransform>();
design.GetWorldCorners(bounds);
var safe = UnityEngine.Screen.safeArea;
bool inside = true;
foreach (var p in bounds) inside &= p.x >= safe.xMin - 1 && p.x <= safe.xMax + 1 && p.y >= safe.yMin - 1 && p.y <= safe.yMax + 1;
var snapshot = new {
    width = UnityEngine.Screen.width, height = UnityEngine.Screen.height,
    contentInsideSafeArea = inside, cards = flow.Selection.Buttons.Count,
    locked = System.Linq.Enumerable.Count(flow.Selection.Buttons, b => b.Locked),
    completed = flow.Progress.ClearedCount,
    scrollContentHeight = scroll.content.rect.height,
    viewportHeight = scroll.viewport.rect.height,
    scrollPosition = scroll.verticalNormalizedPosition
};
if (!inside || scroll.content.rect.height <= scroll.viewport.rect.height) throw new System.Exception("Stage selection layout failed.");
var json = Newtonsoft.Json.JsonConvert.SerializeObject(snapshot, Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText($"Artifacts/StageSystem/Layout-{UnityEngine.Screen.width}x{UnityEngine.Screen.height}.json", json);
return json;
