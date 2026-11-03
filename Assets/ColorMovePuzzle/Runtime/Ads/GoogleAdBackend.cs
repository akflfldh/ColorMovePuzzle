using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;

namespace ColorMovePuzzle.Ads
{
    // All callbacks cross the SDK's persistent Unity update executor before touching game objects.
    public sealed class GoogleAdBackend : IAdBackend
    {
        public GoogleAdBackend()
        {
            // UMP finishes before MobileAds.Initialize. Its Unity callback queue must already tick.
            // This creates only the dispatcher, and does not initialize ads or request consent.
            MobileAdsEventExecutor.Initialize();
        }
#if !COLORMOVE_CLOSED_TEST_NO_ADS
        static bool initializationStarted, initializationFinished, initialized;
        static readonly List<Action<bool>> initializationWaiters = new List<Action<bool>>();
#endif
        static void Main(Action action) => MobileAdsEventExecutor.ExecuteInUpdate(action);
        public bool CanRequestAds => ConsentInformation.CanRequestAds();
        public bool PrivacyOptionsRequired => ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
        public bool ConsentRequired => ConsentInformation.ConsentStatus == ConsentStatus.Required;
        public void UpdateConsent(Action<bool> updated)
        {
            UnityEngine.Debug.Log("[Ads] UMP consent update requested.");
            var request = new ConsentRequestParameters();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            request = UmpDevelopmentDiagnostics.CreateRequest();
#endif
            ConsentInformation.Update(request, error => Main(() => {
                if (error != null) UnityEngine.Debug.Log("[Ads] UMP update unavailable (code " + error.ErrorCode + ").");
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                UnityEngine.Debug.Log("[Ads QA] UMP status=" + ConsentInformation.ConsentStatus
                    + ", privacy=" + ConsentInformation.PrivacyOptionsRequirementStatus
                    + ", canRequest=" + ConsentInformation.CanRequestAds());
#endif
                updated(error == null);
            }));
        }
        public void ShowRequiredConsent(Action completed)
        {
            ConsentForm.LoadAndShowConsentFormIfRequired(error => Main(() => {
                if (error != null) UnityEngine.Debug.Log("[Ads] UMP form unavailable (code " + error.ErrorCode + ").");
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                UnityEngine.Debug.Log("[Ads QA] UMP form returned; status=" + ConsentInformation.ConsentStatus
                    + ", privacy=" + ConsentInformation.PrivacyOptionsRequirementStatus
                    + ", canRequest=" + ConsentInformation.CanRequestAds());
#endif
                completed();
            }));
        }
        public void ShowPrivacyOptions(Action completed)
        {
            ConsentForm.ShowPrivacyOptionsForm(error => Main(() => {
                if (error != null) UnityEngine.Debug.Log("[Ads] Privacy options unavailable (code " + error.ErrorCode + ").");
                completed();
            }));
        }
        public void Initialize(Action<bool> completed)
        {
#if COLORMOVE_CLOSED_TEST_NO_ADS
            completed(false);
#else
            if (initializationFinished) { completed(initialized); return; }
            initializationWaiters.Add(completed);
            if (initializationStarted) return;
            initializationStarted = true;
            try
            {
                MobileAds.Initialize(status => Main(() => {
                    initializationFinished = true; initialized = status != null;
                    var waiters = initializationWaiters.ToArray(); initializationWaiters.Clear();
                    foreach (var callback in waiters) callback(initialized);
                }));
            }
            catch
            {
                initializationFinished = true;
                var waiters = initializationWaiters.ToArray(); initializationWaiters.Clear();
                foreach (var callback in waiters) callback(false);
            }
#endif
        }
        public void LoadInterstitial(string unit, Action<IFullScreenAd> completed)
        {
#if COLORMOVE_CLOSED_TEST_NO_ADS
            completed(null);
#else
            InterstitialAd.Load(unit, new AdRequest(), (ad, error) => Main(() => {
                if (error != null || ad == null)
                {
                    ad?.Destroy(); UnityEngine.Debug.Log("[Ads] Interstitial load failed (code " + (error?.GetCode() ?? -1) + ").");
                    completed(null); return;
                }
                completed(new GoogleInterstitial(ad));
            }));
#endif
        }
        public void LoadRewarded(string unit, Action<IRewardedAd> completed)
        {
#if COLORMOVE_CLOSED_TEST_NO_ADS
            completed(null);
#else
            RewardedAd.Load(unit, new AdRequest(), (ad, error) => Main(() => {
                if (error != null || ad == null) { ad?.Destroy(); completed(null); return; }
                completed(new GoogleRewarded(ad));
            }));
#endif
        }
        sealed class GoogleInterstitial : IFullScreenAd
        {
            InterstitialAd ad;
            public GoogleInterstitial(InterstitialAd value) { ad = value; }
            public bool CanShow => ad != null && ad.CanShowAd();
            public void Show(Action opened, Action closed, Action failed)
            {
                ad.OnAdFullScreenContentOpened += () => Main(opened);
                ad.OnAdFullScreenContentClosed += () => Main(closed);
                ad.OnAdFullScreenContentFailed += error => Main(failed);
                ad.Show();
            }
            public void Dispose() { ad?.Destroy(); ad = null; }
        }
        sealed class GoogleRewarded : IRewardedAd
        {
            RewardedAd ad;
            public GoogleRewarded(RewardedAd value) { ad = value; }
            public bool CanShow => ad != null && ad.CanShowAd();
            public void Show(Action earned, Action closed, Action failed)
            {
                ad.OnAdFullScreenContentClosed += () => Main(closed);
                ad.OnAdFullScreenContentFailed += error => Main(failed);
                ad.Show(reward => Main(earned));
            }
            public void Dispose() { ad?.Destroy(); ad = null; }
        }
    }
}
