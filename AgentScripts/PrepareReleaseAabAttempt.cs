if (UnityEditor.EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
    throw new System.Exception("Stop Play mode and preserve the scene before building.");
var data = new { development = UnityEditor.EditorUserBuildSettings.development,
    appBundle = UnityEditor.EditorUserBuildSettings.buildAppBundle,
    debugging = UnityEditor.EditorUserBuildSettings.allowDebugging,
    profiler = UnityEditor.EditorUserBuildSettings.connectProfiler,
    exportProject = UnityEditor.EditorUserBuildSettings.exportAsGoogleAndroidProject };
System.IO.File.WriteAllText("Artifacts/ReleasePreparation/BuildSettingsBeforeAttempt.json", Newtonsoft.Json.JsonConvert.SerializeObject(data));
UnityEditor.EditorUserBuildSettings.development = false;
UnityEditor.EditorUserBuildSettings.buildAppBundle = true;
UnityEditor.EditorUserBuildSettings.allowDebugging = false;
UnityEditor.EditorUserBuildSettings.connectProfiler = false;
UnityEditor.EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
return new { development = false, appBundle = true, operatingAdsEnabled = false, expectedBlockers = ColorMovePuzzle.Editor.PuzzleReleasePreparation.Blockers() };
