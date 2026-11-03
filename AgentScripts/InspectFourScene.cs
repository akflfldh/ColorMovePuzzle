var game = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleGameController>();
var menu = game.GetComponent<ColorMovePuzzle.PuzzleMenuController>();
var modes = UnityEditor.AssetDatabase.LoadAssetAtPath<ColorMovePuzzle.PuzzleModeCatalog>("Assets/ColorMovePuzzle/Data/Modes.asset");
var snapshot = new {
    scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
    dirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,
    scenes = UnityEngine.SceneManagement.SceneManager.sceneCount,
    controllers = UnityEngine.Object.FindObjectsByType<ColorMovePuzzle.PuzzleGameController>().Length,
    threeSupported = game.SupportsSize(3), fourSupported = game.SupportsSize(4),
    threeStages = modes.Find(3).Stages.Count, fourStages = modes.Find(4).Stages.Count,
    fourAvailable = modes.Find(4).Available, fiveAvailable = modes.Find(5).Available,
    menuVisible = menu.MainMenu.gameObject.activeSelf,
    meshes = UnityEngine.Object.FindObjectsByType<UnityEngine.MeshFilter>().Length,
    rigidbodies = UnityEngine.Object.FindObjectsByType<UnityEngine.Rigidbody>().Length
};
var json = Newtonsoft.Json.JsonConvert.SerializeObject(snapshot, Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("Artifacts/FourByFour/Scene.json", json);
if (!game.SupportsSize(4) || !modes.Find(4).Available || modes.Find(5).Available) throw new System.Exception("4x4 scene configuration failed.");
return json;
