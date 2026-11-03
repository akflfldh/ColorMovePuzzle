var named = UnityEditor.Build.NamedBuildTarget.Android;
var data = new {
    unity = UnityEngine.Application.unityVersion,
    scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
    sceneDirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,
    packageName = UnityEditor.PlayerSettings.GetApplicationIdentifier(named),
    versionName = UnityEditor.PlayerSettings.bundleVersion,
    versionCode = UnityEditor.PlayerSettings.Android.bundleVersionCode,
    minimumApi = (int)UnityEditor.PlayerSettings.Android.minSdkVersion,
    targetApi = (int)UnityEditor.PlayerSettings.Android.targetSdkVersion,
    architecture = UnityEditor.PlayerSettings.Android.targetArchitectures.ToString(),
    backend = UnityEditor.PlayerSettings.GetScriptingBackend(named).ToString(),
    orientation = UnityEditor.PlayerSettings.defaultInterfaceOrientation.ToString(),
    input = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]).FindProperty("activeInputHandler").intValue,
    development = UnityEditor.EditorUserBuildSettings.development,
    appBundle = UnityEditor.EditorUserBuildSettings.buildAppBundle,
    customSigningConfigured = UnityEditor.PlayerSettings.Android.useCustomKeystore,
    uploadKeystoreExists = System.IO.File.Exists(UnityEditor.PlayerSettings.Android.keystoreName),
    runInBackground = UnityEditor.PlayerSettings.runInBackground,
    debugGeographies = System.Enum.GetNames(typeof(GoogleMobileAds.Ump.Api.DebugGeography)),
    blockers = ColorMovePuzzle.Editor.PuzzleReleasePreparation.Blockers(),
    firstScene = System.Linq.Enumerable.First(UnityEditor.EditorBuildSettings.scenes, s => s.enabled).path,
    defaultIcons = System.Linq.Enumerable.Select(UnityEditor.PlayerSettings.GetIcons(UnityEditor.Build.NamedBuildTarget.Android, UnityEditor.IconKind.Any), t => t ? UnityEditor.AssetDatabase.GetAssetPath(t) : null).ToArray()
};
System.IO.File.WriteAllText("Artifacts/ReleasePreparation/Settings.json", Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
return data;
