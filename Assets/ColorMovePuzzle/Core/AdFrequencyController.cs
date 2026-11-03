using System;
using System.Collections.Generic;

namespace ColorMovePuzzle.Ads
{
    [Serializable]
    public sealed class AdFrequencyState
    {
        public int version = 1;
        public int totalClears;
        public int clearsSinceAd;
        public double gameplaySeconds;
        public double lastAdGameplaySeconds;
        public bool hasShown;
        public bool pendingShow;
        public double pendingShowGameplaySeconds;
        public string[] recentClearIds = Array.Empty<string>();
    }

    // Counts and active gameplay time are shared across modes. No Unity/SDK dependency.
    public sealed class AdFrequencyController
    {
        readonly HashSet<string> seen;
        readonly Queue<string> history;
        public AdFrequencyState State { get; }
        public int FirstClearCount { get; }
        public int ClearsBetweenAds { get; }
        public double SecondsBetweenAds { get; }
        public AdFrequencyController(AdFrequencyState saved, int first = 5, int clears = 4, double seconds = 180)
        {
            if (first < 1 || clears < 1 || seconds < 0) throw new ArgumentException("Invalid frequency limits.");
            State = IsValid(saved) ? saved : new AdFrequencyState();
            if (State.pendingShow)
            {
                // The process may have died after opening native UI but before Unity received Opened.
                // This is a conservative frequency fence, not a financial impression report.
                State.hasShown = true; State.clearsSinceAd = 0;
                State.lastAdGameplaySeconds = State.pendingShowGameplaySeconds;
                State.pendingShow = false; State.pendingShowGameplaySeconds = 0;
            }
            FirstClearCount = first; ClearsBetweenAds = clears; SecondsBetweenAds = seconds;
            history = new Queue<string>(State.recentClearIds ?? Array.Empty<string>());
            seen = new HashSet<string>(history);
        }
        public static bool IsValid(AdFrequencyState s) => s != null && s.version == 1 && s.totalClears >= 0
            && s.clearsSinceAd >= 0 && s.clearsSinceAd <= s.totalClears && s.gameplaySeconds >= 0
            && !double.IsNaN(s.gameplaySeconds) && !double.IsInfinity(s.gameplaySeconds)
            && s.lastAdGameplaySeconds >= 0 && s.lastAdGameplaySeconds <= s.gameplaySeconds
            && s.pendingShowGameplaySeconds >= 0 && s.pendingShowGameplaySeconds <= s.gameplaySeconds
            && s.recentClearIds != null && s.recentClearIds.Length <= 128;
        public void SeedLegacyClears(int count)
        {
            if (count <= State.totalClears) return;
            int difference = count - State.totalClears;
            State.totalClears = count;
            if (!State.hasShown) State.clearsSinceAd += difference;
        }
        public void Advance(double seconds, bool activelyPlaying)
        {
            if (activelyPlaying && seconds > 0 && !double.IsNaN(seconds) && !double.IsInfinity(seconds))
                State.gameplaySeconds += seconds;
        }
        public bool RecordClear(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || !seen.Add(eventId)) return false;
            history.Enqueue(eventId);
            while (history.Count > 128) seen.Remove(history.Dequeue());
            State.recentClearIds = history.ToArray();
            State.totalClears = Math.Min(State.totalClears, int.MaxValue - 1) + 1;
            State.clearsSinceAd = Math.Min(State.clearsSinceAd, int.MaxValue - 1) + 1;
            return true;
        }
        public bool CanShow => !State.pendingShow && State.totalClears >= FirstClearCount && State.gameplaySeconds >= SecondsBetweenAds
            && (!State.hasShown || (State.clearsSinceAd >= ClearsBetweenAds
                && State.gameplaySeconds - State.lastAdGameplaySeconds >= SecondsBetweenAds));
        // Called when the SDK actually opens a full-screen ad, never on load/request.
        public void RecordShown()
        {
            State.pendingShow = false; State.pendingShowGameplaySeconds = 0;
            State.hasShown = true; State.clearsSinceAd = 0;
            State.lastAdGameplaySeconds = State.gameplaySeconds;
        }
        public bool ReserveShow()
        {
            if (!CanShow) return false;
            State.pendingShow = true; State.pendingShowGameplaySeconds = State.gameplaySeconds;
            return true;
        }
        public void CancelReservation() { State.pendingShow = false; State.pendingShowGameplaySeconds = 0; }
    }
}
