var path = ColorMovePuzzle.Editor.ClosedTestReleaseBuild.SettingsPath;
var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<ColorMovePuzzle.Editor.ClosedTestReleaseSettings>(path);
if (!settings) { settings = UnityEngine.ScriptableObject.CreateInstance<ColorMovePuzzle.Editor.ClosedTestReleaseSettings>(); UnityEditor.AssetDatabase.CreateAsset(settings, path); }
settings.consentAppId = "ca-app-pub-9556737105305150~6563007441";
settings.versionName = "1.0"; settings.versionCode = 1;
UnityEditor.EditorUtility.SetDirty(settings); UnityEditor.AssetDatabase.SaveAssets();
return new { channel = "ClosedTestNoAds", consentAppId = settings.consentAppId, versionName = settings.versionName,
    versionCode = settings.versionCode, advertising = false, ump = true, sourceSceneModified = false };
