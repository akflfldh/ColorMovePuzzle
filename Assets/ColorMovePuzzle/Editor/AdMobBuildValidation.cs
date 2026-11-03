using System;
using System.Linq;
using ColorMovePuzzle.Ads;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ColorMovePuzzle.Editor
{
    public sealed class AdMobBuildValidation : IPreprocessBuildWithReport
    {
        public const string ConfigPath = "Assets/ColorMovePuzzle/Data/AdConfig.asset";
        public int callbackOrder => -10000;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            if (ClosedTestReleaseBuild.IsActiveFor(report))
            {
                ClosedTestReleaseBuild.ValidateBuild(report);
                return; // Dedicated compile-time no-ad-request channel; standard Release checks remain below.
            }
            var config = AssetDatabase.LoadAssetAtPath<AdConfig>(ConfigPath);
            if (!config) throw new BuildFailedException("Missing AdConfig asset.");
            string error = config.ValidationError((report.summary.options & BuildOptions.Development) != 0);
            if (error != null) throw new BuildFailedException(error);
            var sdk = AssetDatabase.LoadMainAssetAtPath("Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset");
            if (!sdk || new SerializedObject(sdk).FindProperty("adMobAndroidAppId").stringValue != config.AppId)
                throw new BuildFailedException("SDK manifest App ID and AdConfig App ID do not match.");
            if (!PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android).Split(';').Contains("APP_UI_EDITOR_ONLY"))
                throw new BuildFailedException("Android APP_UI_EDITOR_ONLY stability setting must be preserved.");
            if ((report.summary.options & BuildOptions.Development) == 0)
            {
                if (!EditorUserBuildSettings.buildAppBundle) throw new BuildFailedException("Google Play Release must be built as an AAB.");
                var blockers = PuzzleReleasePreparation.Blockers();
                if (blockers.Length > 0) throw new BuildFailedException(string.Join("\n", blockers));
                if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
                    throw new BuildFailedException("Enter signing passwords in this Editor session or a secure CI secret provider; never store them in the repository.");
            }
        }
    }
}
