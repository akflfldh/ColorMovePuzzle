var target = UnityEditor.Build.NamedBuildTarget.Android;
var config = UnityEditor.AssetDatabase.LoadAssetAtPath<ColorMovePuzzle.Ads.AdConfig>(ColorMovePuzzle.Editor.AdMobBuildValidation.ConfigPath);
var sdk = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadMainAssetAtPath("Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset"));
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var result = new {
    unityVersion = UnityEngine.Application.unityVersion,
    activeScene = scene.path, sceneDirty = scene.isDirty,
    playing = UnityEditor.EditorApplication.isPlaying, compiling = UnityEditor.EditorApplication.isCompiling,
    packageName = UnityEditor.PlayerSettings.GetApplicationIdentifier(target),
    minApi = (int)UnityEditor.PlayerSettings.Android.minSdkVersion,
    targetApi = (int)UnityEditor.PlayerSettings.Android.targetSdkVersion,
    scriptingBackend = UnityEditor.PlayerSettings.GetScriptingBackend(target).ToString(),
    architecture = UnityEditor.PlayerSettings.Android.targetArchitectures.ToString(),
    orientation = UnityEditor.PlayerSettings.defaultInterfaceOrientation.ToString(),
    managedVariant = UnityEditor.PlayerSettings.GetManagedCodeVariant(target).ToString(),
    development = UnityEditor.EditorUserBuildSettings.development,
    buildAppBundle = UnityEditor.EditorUserBuildSettings.buildAppBundle,
    allowDebugging = UnityEditor.EditorUserBuildSettings.allowDebugging,
    connectProfiler = UnityEditor.EditorUserBuildSettings.connectProfiler,
    customKeystore = UnityEditor.PlayerSettings.Android.useCustomKeystore,
    keystorePasswordInSettings = !string.IsNullOrEmpty(UnityEditor.PlayerSettings.Android.keystorePass),
    aliasPasswordInSettings = !string.IsNullOrEmpty(UnityEditor.PlayerSettings.Android.keyaliasPass),
    defines = UnityEditor.PlayerSettings.GetScriptingDefineSymbols(target),
    editorAdChannel = ColorMovePuzzle.Ads.AdBuildPolicy.Channel,
    sdkSourceAppIdRestored = sdk.FindProperty("adMobAndroidAppId").stringValue == config.AppId,
    standardReleaseBlockReason = config.ValidationError(false),
    sourceDevelopmentConfigValid = config.ValidationError(true) == null,
    enabledScenes = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(
        System.Linq.Enumerable.Where(UnityEditor.EditorBuildSettings.scenes, s => s.enabled), s => s.path))
};
System.IO.File.WriteAllText("Artifacts/ClosedTest/EditorFinalState.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
return result;
