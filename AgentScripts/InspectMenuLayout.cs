UnityEngine.Canvas.ForceUpdateCanvases();
var menu = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleMenuController>();
var design = (menu.Screen == ColorMovePuzzle.PuzzleScreen.Settings ? menu.Settings.transform : menu.MainMenu.transform)
    .GetChild(0).GetChild(0).GetComponent<UnityEngine.RectTransform>();
var corners = new UnityEngine.Vector3[4];
design.GetWorldCorners(corners);
var safe = UnityEngine.Screen.safeArea;
bool inside = true;
foreach (var p in corners) inside &= p.x >= safe.xMin - 1 && p.x <= safe.xMax + 1 && p.y >= safe.yMin - 1 && p.y <= safe.yMax + 1;
var snapshot = new {
    width = UnityEngine.Screen.width, height = UnityEngine.Screen.height, screen = menu.Screen.ToString(),
    insideSafeArea = inside, continueEnabled = menu.MainMenu.ContinueButton.interactable,
    threeEnabled = menu.MainMenu.ModeButtons[0].Button.interactable,
    fourEnabled = menu.MainMenu.ModeButtons[1].Button.interactable,
    fiveEnabled = menu.MainMenu.ModeButtons[2].Button.interactable,
    bgm = menu.Profile.BgmVolume, sfx = menu.Profile.SfxVolume,
    frame = UnityEngine.Time.frameCount
};
var json = Newtonsoft.Json.JsonConvert.SerializeObject(snapshot, Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText($"Artifacts/MainMenu/Layout-{menu.Screen}-{UnityEngine.Screen.width}x{UnityEngine.Screen.height}.json", json);
if (!inside || menu.MainMenu.ModeButtons[1].Button.interactable || menu.MainMenu.ModeButtons[2].Button.interactable)
    throw new System.Exception("Menu layout/availability validation failed.");
return json;
