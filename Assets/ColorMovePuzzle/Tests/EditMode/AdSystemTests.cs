using System;
using System.IO;
using ColorMovePuzzle.Ads;
using NUnit.Framework;
using UnityEngine;

namespace ColorMovePuzzle.Tests
{
    public sealed class AdSystemTests
    {
        AdConfig config;
        [SetUp] public void Setup() { config = ScriptableObject.CreateInstance<AdConfig>(); }
        [TearDown] public void Cleanup() { UnityEngine.Object.DestroyImmediate(config); }
        static void Clears(AdFrequencyController frequency, int count, string prefix)
        { for (int i = 0; i < count; i++) frequency.RecordClear(prefix + i); }
        [Test] public void FirstAdNeedsFiveClearsAndThreeMinutesOfGameplay()
        {
            var f = new AdFrequencyController(null); f.Advance(180, true); Clears(f, 4, "a");
            Assert.That(f.CanShow, Is.False); f.RecordClear("fifth"); Assert.That(f.CanShow, Is.True);
            var early = new AdFrequencyController(null); Clears(early, 5, "b"); early.Advance(179, true);
            Assert.That(early.CanShow, Is.False); early.Advance(1, true); Assert.That(early.CanShow, Is.True);
        }
        [Test] public void LaterAdsNeedBothFourNewClearsAndAnotherThreeMinutes()
        {
            var f = new AdFrequencyController(null); Clears(f, 5, "a"); f.Advance(180, true); f.RecordShown();
            Clears(f, 4, "b"); f.Advance(179, true); Assert.That(f.CanShow, Is.False);
            f.Advance(1, true); Assert.That(f.CanShow, Is.True); f.RecordShown();
            Clears(f, 3, "c"); f.Advance(999, true); Assert.That(f.CanShow, Is.False);
        }
        [Test] public void DuplicateClearCannotAdvanceCountsBeforeOrAfterReload()
        {
            var f = new AdFrequencyController(null); Assert.That(f.RecordClear("run"), Is.True);
            for (int i = 0; i < 100; i++) Assert.That(f.RecordClear("run"), Is.False);
            var restored = new AdFrequencyController(JsonUtility.FromJson<AdFrequencyState>(JsonUtility.ToJson(f.State)));
            Assert.That(restored.RecordClear("run"), Is.False); Assert.That(restored.State.totalClears, Is.EqualTo(1));
        }
        [Test] public void ModesShareCountsAndNonGameplayTimeDoesNotCount()
        {
            var f = new AdFrequencyController(null); f.SeedLegacyClears(2);
            f.RecordClear("3x3-run"); f.RecordClear("4x4-run"); f.RecordClear("5x5-run");
            f.Advance(1000, false); Assert.That(f.CanShow, Is.False);
            f.Advance(180, true); Assert.That(f.CanShow, Is.True);
        }
        [Test] public void InterruptedNativeShowRecoversAConservativeCooldownBeforeCallbacks()
        {
            var f = new AdFrequencyController(null); Clears(f, 5, "run"); f.Advance(180, true);
            Assert.That(f.ReserveShow(), Is.True); Assert.That(f.CanShow, Is.False);
            var restored = new AdFrequencyController(JsonUtility.FromJson<AdFrequencyState>(JsonUtility.ToJson(f.State)));
            Assert.That(restored.State.hasShown, Is.True); Assert.That(restored.CanShow, Is.False);
            Clears(restored, 4, "new"); restored.Advance(179, true); Assert.That(restored.CanShow, Is.False);
            restored.Advance(1, true); Assert.That(restored.CanShow, Is.True);
        }
        [Test] public void FailedNativeShowReservationDoesNotConsumeTheClearCounts()
        {
            var f = new AdFrequencyController(null); Clears(f, 5, "run"); f.Advance(180, true);
            Assert.That(f.ReserveShow(), Is.True); f.CancelReservation();
            Assert.That(f.State.totalClears, Is.EqualTo(5)); Assert.That(f.State.hasShown, Is.False); Assert.That(f.CanShow, Is.True);
        }
        [Test] public void TestIdsAreExactAndReleaseIsBlocked()
        {
            Assert.That(config.AppId, Is.EqualTo(AdConfig.TestAppId));
            Assert.That(config.InterstitialId, Is.EqualTo(AdConfig.TestInterstitialId));
            Assert.That(config.RewardedId, Is.EqualTo(AdConfig.TestRewardedId));
            Assert.That(config.ValidationError(true), Is.Null);
            Assert.That(config.ValidationError(false), Does.Contain("Release build blocked"));
            Assert.That(AdConfig.IsProductionId(AdConfig.TestAppId, '~'), Is.False);
            Assert.That(AdConfig.IsProductionId("", '/'), Is.False);
        }
        [Test] public void UnavailableAdNeverWaitsForLoadingOrCallsContinuationItself()
        {
            var backend = new FakeBackend(); var time = new FakeClock();
            using (var ads = new InterstitialAdController(backend, time, config))
            {
                int callbacks = 0; ads.Tick(true);
                Assert.That(ads.TryShow(null, () => callbacks++), Is.False);
                Assert.That(callbacks, Is.Zero);
            }
        }
        [Test] public void LoadFailureUsesDelayedBoundedRetries()
        {
            var backend = new FakeBackend { FailLoads = true }; var time = new FakeClock();
            using (var ads = new InterstitialAdController(backend, time, config))
            {
                ads.Tick(true); Assert.That(backend.Loads, Is.EqualTo(1));
                for (int i = 0; i < 100; i++) ads.Tick(true);
                Assert.That(backend.Loads, Is.EqualTo(1));
                for (int i = 0; i < 20; i++) { time.Now += 1000; ads.Tick(true); }
                Assert.That(backend.Loads, Is.EqualTo(4));
            }
        }
        [Test] public void LateLoadAfterConsentRevocationIsDestroyedAndNeverShown()
        {
            var backend = new FakeBackend(); var ad = new FakeAd();
            using (var ads = new InterstitialAdController(backend, new FakeClock(), config))
            {
                ads.Tick(true); var late = backend.Load;
                ads.Tick(false); late(ad);
                Assert.That(ads.IsReady, Is.False); Assert.That(ad.Disposals, Is.EqualTo(1)); Assert.That(ad.Shows, Is.Zero);
            }
        }
        [Test] public void DuplicateOpenedClosedAndFailedEventsOnlyCountAndContinueOnce()
        {
            var backend = new FakeBackend(); var ad = new FakeAd();
            using (var ads = new InterstitialAdController(backend, new FakeClock(), config))
            {
                ads.Tick(true); backend.Load(ad); int opens = 0, ends = 0;
                Assert.That(ads.TryShow(() => opens++, () => ends++), Is.True);
                ad.Opened(); ad.Opened(); Assert.That(opens, Is.EqualTo(1));
                ad.Closed(); ad.Failed(); ad.Closed(); Assert.That(ends, Is.EqualTo(1));
                Assert.That(ad.Disposals, Is.EqualTo(1)); Assert.That(ads.IsShowing, Is.False);
            }
        }
        [Test] public void ShowFailureAndExceptionRecoverExactlyOnce()
        {
            foreach (bool throws in new[] {false, true})
            {
                var backend = new FakeBackend(); var ad = new FakeAd { Throws = throws };
                using (var ads = new InterstitialAdController(backend, new FakeClock(), config))
                {
                    ads.Tick(true); backend.Load(ad); int done = 0;
                    ads.TryShow(null, () => done++);
                    if (!throws) { ad.Failed(); ad.Failed(); }
                    Assert.That(done, Is.EqualTo(1)); Assert.That(ads.IsShowing, Is.False);
                }
            }
        }
        [Test] public void ConsentIsUpdatedOnceAndFormsWaitForSafeScreen()
        {
            var backend = new FakeBackend { Allowed = true, Required = true }; var consent = new AdConsentController(backend);
            consent.Start(); consent.Start(); Assert.That(backend.ConsentUpdates, Is.EqualTo(1));
            Assert.That(consent.CanRequestAds, Is.False);
            backend.ConsentUpdated(true); consent.Tick(false); Assert.That(backend.Forms, Is.Zero);
            Assert.That(consent.CanRequestAds, Is.False);
            consent.Tick(true); Assert.That(backend.Forms, Is.EqualTo(1)); Assert.That(consent.Busy, Is.True);
            backend.FormCompleted(); Assert.That(consent.CanRequestAds, Is.True);
            backend.Allowed = false; Assert.That(consent.CanRequestAds, Is.False);
        }
        [Test] public void ConsentUpdateErrorOnlyUsesTheRealSdkPermissionDecision()
        {
            foreach (bool allowed in new[] {false, true})
            {
                var backend = new FakeBackend { Allowed = allowed }; var consent = new AdConsentController(backend);
                consent.Start(); backend.ConsentUpdated(false);
                Assert.That(consent.CanRequestAds, Is.EqualTo(allowed)); Assert.That(backend.Forms, Is.Zero);
            }
        }
        [Test] public void LateNonRequiredConsentCanFinishDuringPlayWithoutBlockingInput()
        {
            var backend = new FakeBackend { Allowed = true }; var consent = new AdConsentController(backend);
            consent.Start(); backend.ConsentUpdated(true); consent.Tick(false);
            Assert.That(backend.Forms, Is.EqualTo(1)); Assert.That(consent.Busy, Is.False);
            Assert.That(consent.CanRequestAds, Is.False);
            backend.FormCompleted(); Assert.That(consent.CanRequestAds, Is.True);
        }
        [Test] public void RewardedCannotLoadOrShowWithoutARewardAndUserOptIn()
        {
            var backend = new FakeBackend();
            using (var ads = new RewardedAdController(backend, config))
            {
                Assert.That(ads.Prepare(null, true), Is.False); Assert.That(backend.RewardLoads, Is.Zero);
                var consumer = new FakeConsumer(); Assert.That(ads.Prepare(consumer, true), Is.True);
                var ad = new FakeReward(); backend.RewardLoad(ad);
                Assert.That(ads.TryShow(consumer, false, true, null), Is.False); Assert.That(ad.Shows, Is.Zero);
                Assert.That(ads.TryShow(consumer, true, false, null), Is.False);
            }
        }
        [Test] public void RewardIsGrantedOnlyOnceFromEarnedCallback()
        {
            var backend = new FakeBackend(); var consumer = new FakeConsumer(); var ad = new FakeReward();
            using (var ads = new RewardedAdController(backend, config))
            {
                ads.Prepare(consumer, true); backend.RewardLoad(ad); int completions = 0; bool granted = false;
                ads.TryShow(consumer, true, true, result => { completions++; granted = result; });
                Assert.That(consumer.Grants, Is.Zero); ad.Earned(); ad.Earned(); Assert.That(consumer.Grants, Is.EqualTo(1));
                ad.Closed(); ad.Closed(); Assert.That(completions, Is.EqualTo(1)); Assert.That(granted, Is.True);
            }
        }
        [Test] public void ClosingRewardedWithoutEarningGrantsNothing()
        {
            var backend = new FakeBackend(); var consumer = new FakeConsumer(); var ad = new FakeReward();
            using (var ads = new RewardedAdController(backend, config))
            {
                ads.Prepare(consumer, true); backend.RewardLoad(ad); bool? granted = null;
                ads.TryShow(consumer, true, true, result => granted = result); ad.Closed(); ad.Earned();
                Assert.That(consumer.Grants, Is.Zero); Assert.That(granted, Is.False);
            }
        }
        [Test] public void FrequencySaveAndBackupRecoveryNeverRewriteGameProgress()
        {
            string folder = Path.Combine(Application.temporaryCachePath, "ColorMovePuzzleTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(folder);
            try
            {
                string progress = Path.Combine(folder, "progress-v1.json"); File.WriteAllText(progress, "legacy untouched");
                string path = Path.Combine(folder, "ads.json"); var store = new AdFrequencyStore(path);
                var f = new AdFrequencyController(null); f.RecordClear("one"); store.Save(f.State);
                f.RecordClear("two"); store.Save(f.State); File.WriteAllText(path, "{broken");
                Assert.That(store.Load().totalClears, Is.EqualTo(1)); Assert.That(File.ReadAllText(progress), Is.EqualTo("legacy untouched"));
            }
            finally { foreach (var file in Directory.GetFiles(folder)) File.Delete(file); Directory.Delete(folder); }
        }
        sealed class FakeClock : IAdClock { public double Now { get; set; } }
        sealed class FakeBackend : IAdBackend
        {
            public bool Allowed, FailLoads, Required; public int Loads, Forms, ConsentUpdates, RewardLoads;
            public Action<IFullScreenAd> Load; public Action<IRewardedAd> RewardLoad;
            public Action<bool> ConsentUpdated; public Action FormCompleted;
            public bool CanRequestAds => Allowed; public bool PrivacyOptionsRequired => Allowed; public bool ConsentRequired => Required;
            public void UpdateConsent(Action<bool> done) { ConsentUpdates++; ConsentUpdated = done; }
            public void ShowRequiredConsent(Action done) { Forms++; FormCompleted = done; }
            public void ShowPrivacyOptions(Action done) { FormCompleted = done; }
            public void Initialize(Action<bool> done) => done(true);
            public void LoadInterstitial(string unit, Action<IFullScreenAd> done) { Loads++; Load = done; if (FailLoads) done(null); }
            public void LoadRewarded(string unit, Action<IRewardedAd> done) { RewardLoads++; RewardLoad = done; }
        }
        sealed class FakeAd : IFullScreenAd
        {
            public int Shows, Disposals; public bool Throws; public Action Opened, Closed, Failed;
            public bool CanShow => Disposals == 0;
            public void Show(Action opened, Action closed, Action failed)
            { Shows++; Opened = opened; Closed = closed; Failed = failed; if (Throws) throw new InvalidOperationException(); }
            public void Dispose() => Disposals++;
        }
        sealed class FakeReward : IRewardedAd
        {
            public int Shows; public Action Earned, Closed; public bool CanShow => true;
            public void Show(Action earned, Action closed, Action failed) { Shows++; Earned = earned; Closed = closed; }
            public void Dispose() { }
        }
        sealed class FakeConsumer : IAdRewardConsumer
        { public int Grants; public bool CanProvideReward => true; public void GrantReward(string transactionId) => Grants++; }
    }
}
