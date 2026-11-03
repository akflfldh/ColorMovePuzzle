using System;
using ColorMovePuzzle.Ads;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorMovePuzzle.Editor
{
    public static class PuzzleAdMobSceneSetup
    {
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != PuzzleSceneBuilder.ScenePath)
                throw new InvalidOperationException("Open the existing game scene outside Play mode.");
            var campaign = UnityEngine.Object.FindAnyObjectByType<PuzzleCampaignController>();
            var game = UnityEngine.Object.FindAnyObjectByType<PuzzleGameController>();
            var menu = UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>();
            var canvas = GameObject.Find("PuzzleCanvas");
            var settings = canvas.transform.Find("SettingsPanel/SettingsPanelSafeArea/SettingsPanelContent");
            if (!campaign || !game || !menu || !settings) throw new InvalidOperationException("Existing scene references are missing.");
            var config = AssetDatabase.LoadAssetAtPath<AdConfig>(AdMobBuildValidation.ConfigPath);
            if (!config)
            {
                config = ScriptableObject.CreateInstance<AdConfig>();
                AssetDatabase.CreateAsset(config, AdMobBuildValidation.ConfigPath);
            }
            PuzzleSceneBuilder.LoadUIAssets();
            var privacyTransform = settings.Find("PrivacyOptionsButton");
            UnityEngine.UI.Button privacy;
            if (!privacyTransform)
            {
                privacy = PuzzleSceneBuilder.Button("PrivacyOptionsButton", settings, "PRIVACY OPTIONS", 0, 1380, 600, 68);
                privacy.GetComponentInChildren<TMP_Text>().fontSize = 23;
            }
            else privacy = privacyTransform.GetComponent<UnityEngine.UI.Button>();
            privacy.gameObject.SetActive(false);
            var blockerTransform = canvas.transform.Find("AdInputBlocker");
            GameObject blocker;
            if (!blockerTransform)
            {
                blocker = new GameObject("AdInputBlocker", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                blocker.transform.SetParent(canvas.transform, false);
                var rect = blocker.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                var image = blocker.GetComponent<UnityEngine.UI.Image>(); image.color = Color.clear; image.raycastTarget = true;
            }
            else blocker = blockerTransform.gameObject;
            blocker.SetActive(false);
            var ads = campaign.GetComponent<AdManager>();
            if (!ads) ads = campaign.gameObject.AddComponent<AdManager>();
            ads.Configure(config, campaign, game, menu, blocker, privacy);
            EditorUtility.SetDirty(ads);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
            AssetDatabase.SaveAssets();
        }
    }
}
