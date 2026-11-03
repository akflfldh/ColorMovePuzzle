var flow = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleCampaignController>();
var missing = new System.Collections.Generic.List<string>();
var scripts = UnityEngine.Object.FindObjectsByType<UnityEngine.MonoBehaviour>(UnityEngine.FindObjectsInactive.Include);
foreach (var component in scripts)
{
    if (component == null || component.GetType().Namespace != "ColorMovePuzzle") continue;
    foreach (var field in component.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
        if (field.IsDefined(typeof(UnityEngine.SerializeField), true)
            && typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)
            && (UnityEngine.Object)field.GetValue(component) == null)
            missing.Add(component.name + ":" + field.Name);
}
var snapshot = new {
    scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
    dirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,
    stages = flow.Catalog.Count, cards = flow.Selection.Buttons.Count,
    selecting = flow.IsSelecting, missingReferences = missing.ToArray(),
    cells = flow.GetComponent<ColorMovePuzzle.PuzzleGameController>().BoardView.transform.Find("Cells").childCount,
    meshes = UnityEngine.Object.FindObjectsByType<UnityEngine.MeshFilter>().Length,
    colliders = UnityEngine.Object.FindObjectsByType<UnityEngine.Collider>().Length,
    minDistance = flow.Catalog.GetStage(1).MinimumMoves,
    maxDistance = flow.Catalog.GetStage(30).MinimumMoves
};
var json = Newtonsoft.Json.JsonConvert.SerializeObject(snapshot, Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("Artifacts/StageSystem/Scene.json", json);
if (missing.Count > 0 || flow.Catalog.Count != 30 || flow.Selection.Buttons.Count != 30)
    throw new System.Exception("Campaign scene reference verification failed: " + string.Join(", ", missing));
return json;
