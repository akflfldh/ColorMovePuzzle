using System;

namespace ColorMovePuzzle.Ads
{
    public sealed class InterstitialAdController : IDisposable
    {
        readonly IAdBackend backend;
        readonly IAdClock clock;
        readonly AdConfig config;
        IFullScreenAd loaded;
        int generation;
        int failures;
        double nextLoad;
        double loadedAt;
        double loadingAt;
        bool loading;
        bool disposed;
        public bool IsShowing { get; private set; }
        public bool IsReady => !disposed && !IsShowing && loaded != null && loaded.CanShow && clock.Now - loadedAt < 3300;
        public InterstitialAdController(IAdBackend source, IAdClock time, AdConfig settings)
        { backend = source; clock = time; config = settings; }
        public void Tick(bool allowed)
        {
            if (disposed || IsShowing) return;
            if (!allowed) { Invalidate(); return; }
            if (loaded != null && !IsReady) { loaded.Dispose(); loaded = null; }
            if (loading && clock.Now - loadingAt > 30)
            { generation++; loading = false; FailedLoad(); }
            if (loading || IsReady || failures > config.MaximumLoadRetries || clock.Now < nextLoad) return;
            loading = true; loadingAt = clock.Now; int request = ++generation;
            try
            {
                backend.LoadInterstitial(config.InterstitialId, ad => {
                    if (disposed || request != generation) { ad?.Dispose(); return; }
                    loading = false;
                    if (ad == null || !ad.CanShow) { ad?.Dispose(); FailedLoad(); return; }
                    loaded = ad; loadedAt = clock.Now; failures = 0;
                    UnityEngine.Debug.Log("[Ads] Test interstitial loaded.");
                });
            }
            catch { loading = false; FailedLoad(); }
        }
        void FailedLoad()
        {
            failures++;
            nextLoad = clock.Now + Math.Min(config.MaximumRetrySeconds, config.InitialRetrySeconds * Math.Pow(2, failures - 1));
            UnityEngine.Debug.Log("[Ads] Interstitial unavailable; gameplay continues. Retry is delayed and bounded.");
        }
        public bool TryShow(Action opened, Action finished)
        {
            if (!IsReady) return false;
            var ad = loaded; loaded = null; IsShowing = true;
            bool done = false, didOpen = false;
            Action complete = () => {
                if (done) return; done = true; IsShowing = false;
                ad.Dispose(); nextLoad = clock.Now + config.InitialRetrySeconds;
                UnityEngine.Debug.Log("[Ads] Interstitial ended; resuming the stage transition.");
                finished?.Invoke();
            };
            try
            {
                ad.Show(() => { if (done || didOpen) return; didOpen = true; opened?.Invoke();
                    UnityEngine.Debug.Log("[Ads] Test interstitial opened."); }, complete, complete);
            }
            catch { complete(); }
            return true;
        }
        public void Invalidate()
        {
            if (loaded == null && !loading) return;
            generation++; loaded?.Dispose(); loaded = null; loading = false;
        }
        public void Dispose() { disposed = true; Invalidate(); }
    }
}
