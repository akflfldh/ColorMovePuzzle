using System;
using System.IO;
using System.Linq;
using ColorMovePuzzle.Ads;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ColorMovePuzzle.Editor
{
    public static class ClosedTestReleaseBuild
    {
        public const string SettingsPath = "Assets/ColorMovePuzzle/Editor/ClosedTestReleaseSettings.asset";
        public const string UnsignedPath = "Builds/Android/Intermediate/ColorMovePuzzle-ClosedTest-unsigned.aab";
        const string ActiveKey = "ColorMovePuzzle.ClosedTestBuild.Active";
        public static bool IsActiveFor(BuildReport report) => SessionState.GetBool(ActiveKey, false)
            && string.Equals(Path.GetFullPath(report.summary.outputPath), Path.GetFullPath(UnsignedPath), StringComparison.OrdinalIgnoreCase);
        public static ClosedTestReleaseSettings Settings => AssetDatabase.LoadAssetAtPath<ClosedTestReleaseSettings>(SettingsPath);

        public static string ValidateConsentAppId(string appId) => AdConfig.IsProductionId(appId, '~') ? null
            : "Closed test requires the owner's real UMP App ID; ad unit IDs are not used.";

        public static void ValidateBuild(BuildReport report)
        {
            if (!IsActiveFor(report)) throw new BuildFailedException("Closed-test build session is not active for this output.");
            if (!Settings || ValidateConsentAppId(Settings.consentAppId) != null) throw new BuildFailedException("Invalid closed-test UMP App ID.");
            if ((report.summary.options & (BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler)) != 0)
                throw new BuildFailedException("Closed test must be a non-Development Release without script debugging/profiler.");
            if (!EditorUserBuildSettings.buildAppBundle || EditorUserBuildSettings.exportAsGoogleAndroidProject)
                throw new BuildFailedException("Closed test must output an Android App Bundle.");
            var target = NamedBuildTarget.Android;
            if (PlayerSettings.GetApplicationIdentifier(target) != "com.colormovepuzzle.game"
                || PlayerSettings.Android.targetSdkVersion != (AndroidSdkVersions)36
                || PlayerSettings.Android.minSdkVersion != (AndroidSdkVersions)26
                || PlayerSettings.GetScriptingBackend(target) != ScriptingImplementation.IL2CPP
                || PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64
                || PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait
                || PlayerSettings.GetManagedCodeVariant(target) != ManagedCodeVariant.Release)
                throw new BuildFailedException("Closed test requires the reviewed package/API/IL2CPP/ARM64/Portrait/managed Release settings.");
            if (!PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Contains("APP_UI_EDITOR_ONLY"))
                throw new BuildFailedException("Android input stability define must be retained.");
            var sdk = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset"));
            if (sdk.FindProperty("adMobAndroidAppId").stringValue != Settings.consentAppId)
                throw new BuildFailedException("The closed-test manifest must use the owner's consent App ID.");
        }

        [MenuItem("Color Move Puzzle/Closed Test/Build Release AAB (No Ads)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Preserve the scene and stop Play/compilation before building.");
            if (!Settings || ValidateConsentAppId(Settings.consentAppId) != null || Settings.versionCode < 1)
                throw new InvalidOperationException("Configure the closed-test settings asset first.");
            var target = NamedBuildTarget.Android;
            bool development = EditorUserBuildSettings.development, bundle = EditorUserBuildSettings.buildAppBundle,
                debugging = EditorUserBuildSettings.allowDebugging, profiler = EditorUserBuildSettings.connectProfiler,
                export = EditorUserBuildSettings.exportAsGoogleAndroidProject, customKey = PlayerSettings.Android.useCustomKeystore;
            var variant = PlayerSettings.GetManagedCodeVariant(target);
            string version = PlayerSettings.bundleVersion; int code = PlayerSettings.Android.bundleVersionCode;
            var sdk = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset"));
            string originalApp = sdk.FindProperty("adMobAndroidAppId").stringValue;
            Directory.CreateDirectory(Path.GetDirectoryName(UnsignedPath));
            Directory.CreateDirectory("Artifacts/ClosedTest");
            try
            {
                EditorUserBuildSettings.development = EditorUserBuildSettings.allowDebugging = EditorUserBuildSettings.connectProfiler = false;
                EditorUserBuildSettings.buildAppBundle = true; EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                // Signing is a separate JDK step using an encrypted credential outside the project.
                // Unity/Gradle never receives the upload-key password or writes it into a settings file.
                PlayerSettings.Android.useCustomKeystore = false;
                PlayerSettings.SetManagedCodeVariant(target, ManagedCodeVariant.Release);
                PlayerSettings.bundleVersion = Settings.versionName; PlayerSettings.Android.bundleVersionCode = Settings.versionCode;
                sdk.FindProperty("adMobAndroidAppId").stringValue = Settings.consentAppId;
                sdk.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
                SessionState.SetBool(ActiveKey, true);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    target = BuildTarget.Android, locationPathName = UnsignedPath,
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    options = BuildOptions.DetailedBuildReport,
                    extraScriptingDefines = new[] { AdBuildPolicy.ClosedTestDefine }
                });
                var summary = new { result = report.summary.result.ToString(), milliseconds = report.summary.totalTime.TotalMilliseconds,
                    warnings = report.summary.totalWarnings, errors = report.summary.totalErrors,
                    path = report.summary.outputPath, development = false, managedVariant = "Release",
                    channel = "ClosedTestNoAds", defines = new[] { AdBuildPolicy.ClosedTestDefine },
                    buildMessages = report.steps.SelectMany(s => s.messages).Where(m => m.type == LogType.Error || m.type == LogType.Warning)
                        .Select(m => new { type = m.type.ToString(), message = m.content }).ToArray() };
                File.WriteAllText("Artifacts/ClosedTest/BuildReport.json", Newtonsoft.Json.JsonConvert.SerializeObject(summary, Newtonsoft.Json.Formatting.Indented));
                if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Closed-test AAB build failed; see Artifacts/ClosedTest/BuildReport.json.");
            }
            finally
            {
                SessionState.SetBool(ActiveKey, false);
                EditorUserBuildSettings.development = development; EditorUserBuildSettings.buildAppBundle = bundle;
                EditorUserBuildSettings.allowDebugging = debugging; EditorUserBuildSettings.connectProfiler = profiler;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = export; PlayerSettings.Android.useCustomKeystore = customKey;
                PlayerSettings.SetManagedCodeVariant(target, variant);
                PlayerSettings.bundleVersion = version; PlayerSettings.Android.bundleVersionCode = code;
                sdk.Update(); sdk.FindProperty("adMobAndroidAppId").stringValue = originalApp;
                sdk.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
            }
        }
    }
}
