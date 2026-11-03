using System;
using UnityEngine;

namespace ColorMovePuzzle.Ads
{
    [CreateAssetMenu(menuName = "Color Move Puzzle/Ad Configuration")]
    public sealed class AdConfig : ScriptableObject
    {
        public const string TestAppId = "ca-app-pub-3940256099942544~3347511713";
        public const string TestInterstitialId = "ca-app-pub-3940256099942544/1033173712";
        public const string TestRewardedId = "ca-app-pub-3940256099942544/5224354917";
        [SerializeField] bool testAds = true;
        [Tooltip("Optional owner-provided App ID for testing a published UMP message. Ad unit IDs remain official test units. Leave empty until provided.")]
        [SerializeField] string developmentConsentAppId = "";
        [SerializeField] string productionAppId = "";
        [SerializeField] string productionInterstitialId = "";
        [SerializeField] string productionRewardedId = "";
        [SerializeField] bool releasePrivacyAndAudienceReviewed;
        [SerializeField, Min(1)] int firstClears = 5;
        [SerializeField, Min(1)] int clearsBetweenAds = 4;
        [SerializeField, Min(0)] float secondsBetweenAds = 180;
        [SerializeField, Min(10)] float initialRetrySeconds = 30;
        [SerializeField, Min(30)] float maximumRetrySeconds = 300;
        [SerializeField, Range(0, 5)] int maximumLoadRetries = 3;
        public bool TestAds => testAds;
        public string AppId => testAds ? (string.IsNullOrEmpty(developmentConsentAppId) ? TestAppId : developmentConsentAppId) : productionAppId;
        public string InterstitialId => testAds ? TestInterstitialId : productionInterstitialId;
        public string RewardedId => testAds ? TestRewardedId : productionRewardedId;
        public int FirstClears => firstClears;
        public int ClearsBetweenAds => clearsBetweenAds;
        public double SecondsBetweenAds => secondsBetweenAds;
        public double InitialRetrySeconds => initialRetrySeconds;
        public double MaximumRetrySeconds => maximumRetrySeconds;
        public int MaximumLoadRetries => maximumLoadRetries;
        public string ValidationError(bool development)
        {
            if (firstClears < 1 || clearsBetweenAds < 1 || secondsBetweenAds < 0 || initialRetrySeconds < 10
                || maximumRetrySeconds < initialRetrySeconds || maximumLoadRetries < 0) return "Invalid ad frequency/retry configuration.";
            if (testAds)
            {
                if (!development) return "Release build blocked: official TEST ads are still selected.";
                if (!string.IsNullOrEmpty(developmentConsentAppId) && !IsProductionId(developmentConsentAppId, '~'))
                    return "Development consent App ID must be an owner-provided App ID, or empty for the official sample App ID.";
                return null;
            }
            // Production stays explicitly locked until the owner supplies IDs and reviews privacy/audience.
            if (development) return "Development builds must use official Google test ads.";
            if (!releasePrivacyAndAudienceReviewed) return "Release privacy/audience review is incomplete.";
            return ProductionIdsError();
        }
        public string ProductionIdsError()
        {
            if (!IsProductionId(productionAppId, '~')) return "Release requires a real AdMob App ID (publisher~app).";
            if (!IsProductionId(productionInterstitialId, '/')) return "Release requires a real interstitial unit ID (publisher/unit).";
            if (!string.IsNullOrEmpty(productionRewardedId) && !IsProductionId(productionRewardedId, '/'))
                return "Optional production rewarded unit ID is invalid or is a test ID.";
            return null;
        }
        public static bool IsProductionId(string value, char separator) => !string.IsNullOrEmpty(value)
            && (separator == '~' || separator == '/')
            && System.Text.RegularExpressions.Regex.IsMatch(value, @"\Aca-app-pub-[0-9]{16}" + System.Text.RegularExpressions.Regex.Escape(separator.ToString()) + @"[0-9]{10}\z")
            && !value.Contains("3940256099942544");
    }
}
