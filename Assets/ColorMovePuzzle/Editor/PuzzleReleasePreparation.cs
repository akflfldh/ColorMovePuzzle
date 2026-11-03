using System.Collections.Generic;
using System.IO;
using ColorMovePuzzle.Ads;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ColorMovePuzzle.Editor
{
    public static class PuzzleReleasePreparation
    {
        public static string[] Blockers()
        {
            var errors = new List<string>();
            var config = AssetDatabase.LoadAssetAtPath<AdConfig>(AdMobBuildValidation.ConfigPath);
            if (!config) errors.Add("AdConfig is missing.");
            else
            {
                var error = config.ValidationError(false); if (error != null) errors.Add(error);
                var ids = config.ProductionIdsError(); if (ids != null) errors.Add(ids);
            }
            if (!PlayerSettings.Android.useCustomKeystore || string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName)
                || !File.Exists(PlayerSettings.Android.keystoreName)) errors.Add("A user-owned upload keystore has not been configured.");
            if (string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName)) errors.Add("Upload key alias is not configured.");
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP) errors.Add("Release requires IL2CPP.");
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0) errors.Add("Release requires ARM64.");
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait) errors.Add("Portrait orientation must be preserved.");
            if ((int)PlayerSettings.Android.targetSdkVersion < 36) errors.Add("Select an explicit Android Target API 36 or higher before release.");
            if (PlayerSettings.Android.bundleVersionCode < 1) errors.Add("Version Code must be positive; verify it exceeds Play Console uploads.");
            return errors.ToArray();
        }

        [MenuItem("Color Move Puzzle/Release/Check Readiness")]
        public static void CheckReadiness()
        {
            var blockers = Blockers();
            Debug.Log(blockers.Length == 0 ? "Release settings checks passed. Account, policy and device review are still required."
                : "Release blocked:\n" + string.Join("\n", blockers));
        }
    }
}
