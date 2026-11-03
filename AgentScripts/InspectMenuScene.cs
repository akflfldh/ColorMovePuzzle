var menu = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleMenuController>();
var missing = new System.Collections.Generic.List<string>();
foreach (var component in UnityEngine.Object.FindObjectsByType<UnityEngine.MonoBehaviour>(UnityEngine.FindObjectsInactive.Include))
{
    if (component == null || component.GetType().Namespace != "ColorMovePuzzle") continue;
    foreach (var field in component.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
        if (field.IsDefined(typeof(UnityEngine.SerializeField), true) && typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)
            && (UnityEngine.Object)field.GetValue(component) == null) missing.Add(component.name + ":" + field.Name);
}
var flow = menu.GetComponent<ColorMovePuzzle.PuzzleCampaignController>();
var modes = UnityEditor.AssetDatabase.LoadAssetAtPath<ColorMovePuzzle.PuzzleModeCatalog>("Assets/ColorMovePuzzle/Data/Modes.asset");
var snapshot = new {
    scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
    dirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,
    rootCount = UnityEngine.SceneManagement.SceneManager.GetActiveScene().rootCount,
    menuVisible = menu.MainMenu.gameObject.activeSelf,
    settingsVisible = menu.Settings.gameObject.activeSelf,
    stageSelectionVisible = flow.IsSelecting,
    stages = flow.Catalog.Count, modeCards = menu.MainMenu.ModeButtons.Count,
    threeAvailable = modes.Find(3).Available, fourAvailable = modes.Find(4).Available, fiveAvailable = modes.Find(5).Available,
    missingReferences = missing.ToArray(),
    meshes = UnityEngine.Object.FindObjectsByType<UnityEngine.MeshFilter>().Length,
    colliders = UnityEngine.Object.FindObjectsByType<UnityEngine.Collider>().Length
};
System.IO.Directory.CreateDirectory("Artifacts/MainMenu");
var json = Newtonsoft.Json.JsonConvert.SerializeObject(snapshot, Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("Artifacts/MainMenu/Scene.json", json);
if (missing.Count > 0) throw new System.Exception("Missing menu references: " + string.Join(", ", missing));
return json;
