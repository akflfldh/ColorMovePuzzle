var game = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleGameController>();
var campaign = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleCampaignController>();
var menu = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleMenuController>();
var android = UnityEditor.Build.NamedBuildTarget.Android;
var data = new {
    scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
    gameController = game != null, campaignController = campaign != null, menuController = menu != null,
    boardSizes = new[] { game.SupportsSize(3), game.SupportsSize(4), game.SupportsSize(5) },
    target = UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString(),
    orientation = UnityEditor.PlayerSettings.defaultInterfaceOrientation.ToString(),
    scriptingBackend = UnityEditor.PlayerSettings.GetScriptingBackend(android).ToString(),
    architectures = UnityEditor.PlayerSettings.Android.targetArchitectures.ToString(),
    defines = UnityEditor.PlayerSettings.GetScriptingDefineSymbols(android),
    applicationId = UnityEditor.PlayerSettings.GetApplicationIdentifier(android),
    buildScenes = UnityEditor.EditorBuildSettings.scenes,
    editorCompiling = UnityEditor.EditorApplication.isCompiling
};
string json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("Artifacts/Autosave/SceneAndBuildConfiguration.json", json);
return json;
