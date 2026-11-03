using System;

namespace ColorMovePuzzle.Ads
{
    // No reward consumer exists in this game yet, so no rewarded ad is loaded or exposed by default.
    public sealed class RewardedAdController : IDisposable
    {
        readonly IAdBackend backend;
        readonly AdConfig config;
        IRewardedAd ad;
        bool loading, disposed, showing;
        int generation;
        public RewardedAdController(IAdBackend source, AdConfig settings) { backend = source; config = settings; }
        public bool Prepare(IAdRewardConsumer consumer, bool adsAllowed)
        {
            if (disposed || loading || showing || ad != null || !adsAllowed || consumer == null || !consumer.CanProvideReward) return false;
            loading = true; int request = ++generation;
            backend.LoadRewarded(config.RewardedId, result => {
                if (disposed || request != generation) { result?.Dispose(); return; }
                loading = false; ad = result;
            });
            return true;
        }
        public bool TryShow(IAdRewardConsumer consumer, bool userOptedIn, bool adsAllowed, Action<bool> completed)
        {
            if (disposed || showing || !userOptedIn || !adsAllowed || consumer == null || !consumer.CanProvideReward || ad == null || !ad.CanShow) return false;
            var current = ad; ad = null; showing = true;
            string transaction = Guid.NewGuid().ToString("N"); bool rewarded = false, ended = false;
            Action finish = () => { if (ended) return; ended = true; showing = false; current.Dispose(); completed?.Invoke(rewarded); };
            try
            {
                current.Show(() => {
                    if (ended || rewarded) return;
                    rewarded = true; consumer.GrantReward(transaction);
                }, finish, finish);
            }
            catch { finish(); }
            return true;
        }
        public void Dispose() { disposed = true; generation++; ad?.Dispose(); ad = null; }
    }
}
