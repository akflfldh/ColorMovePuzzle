using System;
using System.IO;
using UnityEngine;

namespace ColorMovePuzzle.Ads
{
    [DisallowMultipleComponent]
    public sealed class AdManager : MonoBehaviour
    {
        [SerializeField] AdConfig config;
        [SerializeField] PuzzleCampaignController campaign;
        [SerializeField] PuzzleGameController game;
        [SerializeField] PuzzleMenuController menu;
        [SerializeField] GameObject inputBlocker;
        [SerializeField] UnityEngine.UI.Button privacyOptions;
        IAdBackend backend;
        IAdClock clock;
        AdConsentController consent;
        InterstitialAdController interstitial;
        RewardedAdController rewarded;
        AdFrequencyStore frequencyStore;
        AdFrequencyController frequency;
        bool setup, sdkRequested, sdkReady, foreground = true, alive;
        bool dirty, advertisingEnabled;
        string eligibleRun;
        Action pendingContinuation;
        double lastTick, lastPersist, initializationAt;
        public bool IsBlocking => alive && (interstitial?.IsShowing == true || consent?.Busy == true || pendingContinuation != null);
        public bool SdkReady => sdkReady;
        public bool AdReady => interstitial?.IsReady == true;
        public bool ConsentPermitsAds => consent?.CanRequestAds == true;
        public bool CanRequestAds => advertisingEnabled && ConsentPermitsAds;
        public bool AdvertisingEnabled => advertisingEnabled;
        public AdFrequencyController Frequency => frequency;
        public void Configure(AdConfig settings, PuzzleCampaignController stages, PuzzleGameController controller,
            PuzzleMenuController screens, GameObject blocker, UnityEngine.UI.Button privacy)
        { config = settings; campaign = stages; game = controller; menu = screens; inputBlocker = blocker; privacyOptions = privacy; }

        void Start()
        {
            if (setup) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            UseServices(new GoogleAdBackend(), new UnityAdClock(), Path.Combine(Application.persistentDataPath, "ColorMovePuzzle", "ads-frequency-v1.json"), AdBuildPolicy.AllowAdRequests);
#else
            // Editor regression tests and previews never request real network ads or fake UMP consent.
            UseServices(new UnavailableAdBackend(), new UnityAdClock(), null);
#endif
        }
        public void UseServices(IAdBackend source, IAdClock time, string frequencyPath, bool allowAdvertising = true)
        {
            if (setup) Detach();
            setup = alive = true;
            advertisingEnabled = allowAdvertising && AdBuildPolicy.AllowAdRequests;
            if (!config || (advertisingEnabled && config.ValidationError(Debug.isDebugBuild || Application.isEditor) != null))
            { Debug.LogError("[Ads] Invalid configuration; advertising disabled."); alive = false; return; }
            backend = source; clock = time;
            frequencyStore = !advertisingEnabled || frequencyPath == null ? null : new AdFrequencyStore(frequencyPath);
            frequency = new AdFrequencyController(frequencyStore?.Load(), config.FirstClears, config.ClearsBetweenAds, config.SecondsBetweenAds);
            // Existing completion records stay read-only. They are not reset for ad frequency.
            if (advertisingEnabled && frequencyPath != null)
            {
                string directory = Path.GetDirectoryName(frequencyPath);
                int legacy = 0;
                foreach (string file in new[] { "progress-v1.json", "progress-4x4-v1.json", "progress-5x5-v1.json" })
                    legacy += new PuzzleProgressStore(Path.Combine(directory, file), 30).Load().ClearedCount;
                frequency.SeedLegacyClears(legacy);
            }
            consent = new AdConsentController(backend);
            interstitial = new InterstitialAdController(backend, clock, config);
            rewarded = new RewardedAdController(backend, config);
            sdkRequested = sdkReady = false; eligibleRun = null;
            lastTick = lastPersist = clock.Now;
            campaign.PlayCompleted += OnCompleted;
            campaign.StageStarted += OnStageStarted;
            campaign.NextStageTransition = TryTransition;
            campaign.NavigationBlocked = () => IsBlocking;
            menu.NavigationBlocked = () => IsBlocking;
            if (privacyOptions) privacyOptions.onClick.AddListener(OpenPrivacyOptions);
            consent.Start();
            if (!advertisingEnabled) Debug.Log("[Ads] Closed test: ad initialization, requests and display are disabled; UMP remains active.");
        }
        void OnStageStarted(PuzzleStageData stage) { eligibleRun = null; }
        void OnCompleted(string run)
        {
            if (!alive || !advertisingEnabled || campaign.IsRestoring || !game.IsCleared || game.IsMoving) return;
            if (frequency.RecordClear(run)) { dirty = true; Persist(); }
            eligibleRun = run;
        }
        public void Tick()
        {
            if (!alive) return;
            if (foreground && pendingContinuation != null)
            { var next = pendingContinuation; pendingContinuation = null; next(); }
            double now = clock.Now, delta = Math.Max(0, now - lastTick); lastTick = now;
            bool playing = foreground && menu.Screen == PuzzleScreen.Playing && game.InputEnabled && !game.IsCleared && !IsBlocking;
            frequency.Advance(delta, playing && advertisingEnabled);
            if (advertisingEnabled && playing && delta > 0) dirty = true;
            consent.Tick(foreground && (menu.Screen == PuzzleScreen.MainMenu || menu.Screen == PuzzleScreen.Settings));
            if (advertisingEnabled && consent.CanRequestAds && !sdkRequested)
            {
                sdkRequested = true; initializationAt = now;
                Debug.Log("[Ads] Initializing Google Mobile Ads after UMP permission check.");
                backend.Initialize(ok => {
                    if (!alive || sdkReady) return;
                    sdkReady = ok;
                    Debug.Log(ok ? "[Ads] Google Mobile Ads initialization completed." : "[Ads] SDK initialization unavailable; gameplay continues.");
                });
            }
            if (sdkRequested && !sdkReady && now - initializationAt > 30)
            { initializationAt = double.PositiveInfinity; Debug.Log("[Ads] SDK initialization delayed; no gameplay is waiting for it."); }
            if (advertisingEnabled) interstitial.Tick(sdkReady && consent.CanRequestAds && foreground);
            if (privacyOptions)
            {
                privacyOptions.gameObject.SetActive(consent.PrivacyOptionsRequired);
                privacyOptions.interactable = !IsBlocking;
            }
            if (inputBlocker) { inputBlocker.SetActive(IsBlocking); if (IsBlocking) inputBlocker.transform.SetAsLastSibling(); }
            if (dirty && now - lastPersist >= 30) Persist();
        }
        void Update() => Tick();
        bool TryTransition(Action proceed)
        {
            // The only placement: explicit acknowledgment of a newly completed result via NEXT.
            // No delayed load callback is ever allowed to start showing an ad.
            bool permitted = alive && advertisingEnabled && foreground && consent.CanRequestAds && sdkReady && !IsBlocking
                && menu.Screen == PuzzleScreen.Playing && game.IsCleared && !game.IsMoving
                && !campaign.IsRestoring && eligibleRun == campaign.CurrentRunId && frequency.CanShow;
            eligibleRun = null; // This opportunity is consumed even when no ad is ready.
            if (!permitted || !interstitial.IsReady) return false;
            campaign.FlushSession();
            if (!frequency.ReserveShow()) return false;
            dirty = true;
            if (!Persist()) { frequency.CancelReservation(); dirty = true; return false; }
            if (inputBlocker) { inputBlocker.SetActive(true); inputBlocker.transform.SetAsLastSibling(); }
            bool accepted = interstitial.TryShow(() => { if (!alive) return; frequency.RecordShown(); dirty = true; Persist(); },
                () => {
                    if (!alive) return;
                    if (frequency.State.pendingShow) { frequency.CancelReservation(); dirty = true; Persist(); }
                    if (!foreground) { pendingContinuation = proceed; return; }
                    if (inputBlocker) inputBlocker.SetActive(false);
                    proceed();
                });
            if (!accepted)
            {
                frequency.CancelReservation(); dirty = true; Persist();
                if (inputBlocker) inputBlocker.SetActive(false);
            }
            return accepted;
        }
        public void OpenPrivacyOptions()
        {
            if (!alive || menu.Screen != PuzzleScreen.Settings || IsBlocking) return;
            interstitial.Invalidate(); rewarded.Dispose();
            rewarded = new RewardedAdController(backend, config);
            consent.ShowPrivacyOptions();
            if (inputBlocker) inputBlocker.SetActive(IsBlocking);
        }
        bool Persist()
        {
            if (!advertisingEnabled) return true;
            if (frequency == null) return false;
            bool saved = frequencyStore == null || frequencyStore.Save(frequency.State);
            if (saved) dirty = false;
            lastPersist = clock.Now;
            return saved;
        }
        void OnApplicationPause(bool paused) { SetForeground(!paused); }
        void OnApplicationFocus(bool focused) { SetForeground(focused); }
        void SetForeground(bool value)
        {
            if (!alive) return;
            foreground = value; lastTick = clock.Now;
            if (!value) { campaign.FlushSession(); Persist(); }
        }
        void OnApplicationQuit() { if (alive) Persist(); }
        void Detach()
        {
            alive = false;
            pendingContinuation = null;
            if (campaign)
            { campaign.PlayCompleted -= OnCompleted; campaign.StageStarted -= OnStageStarted; campaign.NextStageTransition = null; campaign.NavigationBlocked = null; }
            if (menu) menu.NavigationBlocked = null;
            if (privacyOptions) privacyOptions.onClick.RemoveListener(OpenPrivacyOptions);
            interstitial?.Dispose(); rewarded?.Dispose();
            if (inputBlocker) inputBlocker.SetActive(false);
        }
        void OnDestroy() { Detach(); }
    }
}
