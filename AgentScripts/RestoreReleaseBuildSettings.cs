var data = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Artifacts/ReleasePreparation/BuildSettingsBeforeAttempt.json"));
UnityEditor.EditorUserBuildSettings.development = (bool)data["development"];
UnityEditor.EditorUserBuildSettings.buildAppBundle = (bool)data["appBundle"];
UnityEditor.EditorUserBuildSettings.allowDebugging = (bool)data["debugging"];
UnityEditor.EditorUserBuildSettings.connectProfiler = (bool)data["profiler"];
UnityEditor.EditorUserBuildSettings.exportAsGoogleAndroidProject = (bool)data["exportProject"];
UnityEditor.AssetDatabase.SaveAssets();
return new { restored = true, development = UnityEditor.EditorUserBuildSettings.development,
    appBundle = UnityEditor.EditorUserBuildSettings.buildAppBundle };
