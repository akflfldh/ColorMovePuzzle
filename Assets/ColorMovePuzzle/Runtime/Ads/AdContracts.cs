using System;

namespace ColorMovePuzzle.Ads
{
    public interface IAdClock { double Now { get; } }
    public interface IFullScreenAd : IDisposable
    {
        bool CanShow { get; }
        void Show(Action opened, Action closed, Action failed);
    }
    public interface IRewardedAd : IDisposable
    {
        bool CanShow { get; }
        void Show(Action earned, Action closed, Action failed);
    }
    public interface IAdBackend
    {
        bool CanRequestAds { get; }
        bool PrivacyOptionsRequired { get; }
        bool ConsentRequired { get; }
        void UpdateConsent(Action<bool> updated);
        void ShowRequiredConsent(Action completed);
        void ShowPrivacyOptions(Action completed);
        void Initialize(Action<bool> completed);
        void LoadInterstitial(string unit, Action<IFullScreenAd> completed);
        void LoadRewarded(string unit, Action<IRewardedAd> completed);
    }
    public interface IAdRewardConsumer
    {
        // Consumers must guarantee that a meaningful reward can actually be delivered before opt-in.
        bool CanProvideReward { get; }
        void GrantReward(string transactionId);
    }
    sealed class UnityAdClock : IAdClock { public double Now => UnityEngine.Time.realtimeSinceStartupAsDouble; }
    sealed class UnavailableAdBackend : IAdBackend
    {
        public bool CanRequestAds => false;
        public bool PrivacyOptionsRequired => false;
        public bool ConsentRequired => false;
        public void UpdateConsent(Action<bool> updated) => updated(false);
        public void ShowRequiredConsent(Action completed) => completed();
        public void ShowPrivacyOptions(Action completed) => completed();
        public void Initialize(Action<bool> completed) => completed(false);
        public void LoadInterstitial(string unit, Action<IFullScreenAd> completed) => completed(null);
        public void LoadRewarded(string unit, Action<IRewardedAd> completed) => completed(null);
    }
}
