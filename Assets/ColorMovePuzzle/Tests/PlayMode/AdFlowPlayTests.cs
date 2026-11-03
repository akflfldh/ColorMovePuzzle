using System;
using System.Collections;
using System.IO;
using ColorMovePuzzle.Ads;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColorMovePuzzle.Tests
{
    public sealed class AdFlowPlayTests
    {
        string folder;
        PuzzleMenuController menu;
        PuzzleCampaignController campaign;
        PuzzleGameController game;
        AdManager ads;
        FakeBackend backend;
        FakeClock clock;
        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "ColorMovePuzzleTests", Guid.NewGuid().ToString());
            yield return Load();
        }
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity"); yield return null;
            menu = UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>();
            campaign = UnityEngine.Object.FindAnyObjectByType<PuzzleCampaignController>();
            game = UnityEngine.Object.FindAnyObjectByType<PuzzleGameController>();
            ads = UnityEngine.Object.FindAnyObjectByType<AdManager>();
            Assert.That(ads, Is.Not.Null);
            campaign.UseProgressDirectory(folder);
            menu.UseProfileStore(new PuzzleMenuProfileStore(Path.Combine(folder, "menu.json"))); menu.ShowMainMenu();
            backend = new FakeBackend(); clock = new FakeClock();
            ads.UseServices(backend, clock, Path.Combine(folder, "ads.json")); ads.Tick();
        }
        [TearDown] public void Cleanup()
        {
            if (!Directory.Exists(folder)) return;
            foreach (string file in Directory.GetFiles(folder)) File.Delete(file); Directory.Delete(folder);
        }
        IEnumerator Finished()
        {
            double end = Time.realtimeSinceStartupAsDouble + 2;
            while (game.IsMoving && Time.realtimeSinceStartupAsDouble < end) yield return null;
            Assert.That(game.IsMoving, Is.False);
        }
        IEnumerator EligibleClear()
        {
            ads.Frequency.State.totalClears = ads.Frequency.State.clearsSinceAd = 4;
            menu.SelectMode(3); campaign.PlayStage(1);
            clock.Now += 180; ads.Tick();
            foreach (var move in game.Stage.GetSolution()) { game.TryMove(move.from, move.to); yield return Finished(); }
            Assert.That(game.IsCleared, Is.True); Assert.That(ads.Frequency.CanShow, Is.True);
            // Mock placement tests control app lifecycle as well as time; Editor focus is external state.
            ads.SendMessage("OnApplicationFocus", true);
            ads.SendMessage("OnApplicationPause", false);
        }
        [Test] public void NoAdAtStartupMenuOrSettingsAndInitializationOnlyOnce()
        {
            Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.MainMenu));
            for (int i = 0; i < 20; i++) ads.Tick();
            Assert.That(backend.Initializations, Is.EqualTo(1)); Assert.That(backend.Loads, Is.EqualTo(1));
            Assert.That(backend.Shows, Is.Zero); Assert.That(backend.RewardLoads, Is.Zero);
            menu.OpenSettings(); ads.Tick(); menu.HandleBack(); ads.Tick();
            Assert.That(backend.Shows, Is.Zero);
        }
        [UnityTest] public IEnumerator SdkDispatcherDeliversCallbacksBeforeAdsInitialization()
        {
            var bridge = new GoogleAdBackend(); // Constructor only prepares the dispatcher; no network request.
            bool delivered = false;
            typeof(GoogleAdBackend).GetMethod("Main", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { (Action)(() => delivered = true) });
            yield return null; yield return null;
            Assert.That(delivered, Is.True, "UMP callbacks must run before the consent-gated ads initialization.");
        }
        [UnityTest] public IEnumerator OnlyNextAfterEligibleClearCanShowAndDuplicateCloseCannotSkipAStage()
        {
            yield return EligibleClear(); Assert.That(backend.Shows, Is.Zero, "Clear overlay itself must never show an ad.");
            string progress = File.ReadAllText(campaign.SavePath);
            campaign.PlayNext(); Assert.That(backend.Shows, Is.EqualTo(1)); Assert.That(campaign.IsTransitionPending, Is.True);
            campaign.PlayNext(); Assert.That(backend.Shows, Is.EqualTo(1));
            Assert.That(menu.HandleBack(), Is.True); Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.Playing));
            Assert.That(campaign.PlayStage(2), Is.False);
            backend.Opened(); backend.Opened(); Assert.That(ads.Frequency.State.clearsSinceAd, Is.Zero);
            Assert.That(File.ReadAllText(campaign.SavePath), Is.EqualTo(progress));
            backend.Closed(); backend.Closed(); backend.Failed();
            Assert.That(game.Stage.StageNumber, Is.EqualTo(2)); Assert.That(game.MoveCount, Is.Zero);
            Assert.That(campaign.Progress.IsCleared(1), Is.True); Assert.That(campaign.Progress.IsUnlocked(2), Is.True);
            Assert.That(campaign.IsTransitionPending, Is.False);
        }
        [UnityTest] public IEnumerator LoadNotReadySkipsImmediatelyAndDoesNotShowLater()
        {
            backend.CanShow = false;
            yield return EligibleClear(); campaign.PlayNext();
            Assert.That(game.Stage.StageNumber, Is.EqualTo(2)); Assert.That(backend.Shows, Is.Zero);
            backend.CanShow = true; ads.Tick(); yield return null;
            Assert.That(backend.Shows, Is.Zero, "A late ad must not appear in the next puzzle.");
        }
        [UnityTest] public IEnumerator ShowingFailureRestoresNormalNextStageAndSaves()
        {
            yield return EligibleClear(); campaign.PlayNext(); backend.Failed(); backend.Failed();
            Assert.That(game.Stage.StageNumber, Is.EqualTo(2)); Assert.That(game.MoveCount, Is.Zero);
            Assert.That(ads.IsBlocking, Is.False); Assert.That(ads.Frequency.State.hasShown, Is.False);
            Assert.That(new PuzzleProgressStore(campaign.SavePath, 30).Load().IsUnlocked(2), Is.True);
        }
        [UnityTest] public IEnumerator PlayingMovingRestartBackAndSettingsNeverShowEvenWhenFrequencyIsEligible()
        {
            ads.Frequency.State.totalClears = 5; ads.Frequency.State.gameplaySeconds = 180;
            menu.SelectMode(5); campaign.PlayStage(1); var move = game.Stage.GetSolution()[0];
            game.TryMove(move.from, move.to); ads.Tick(); Assert.That(backend.Shows, Is.Zero);
            game.Restart(); ads.Tick(); Assert.That(backend.Shows, Is.Zero);
            menu.HandleBack(); menu.HandleBack(); menu.OpenSettings(); ads.Tick();
            Assert.That(backend.Shows, Is.Zero); yield return null;
        }
        [UnityTest] public IEnumerator ConsentRevocationPreventsRequestsAndDisplayWithoutBlockingPuzzle()
        {
            yield return EligibleClear(); int loads = backend.Loads;
            backend.Allowed = false; ads.Tick();
            Assert.That(ads.CanRequestAds, Is.False); campaign.PlayNext();
            Assert.That(game.Stage.StageNumber, Is.EqualTo(2)); Assert.That(backend.Shows, Is.Zero);
            for (int i = 0; i < 10; i++) { clock.Now += 100; ads.Tick(); }
            Assert.That(backend.Loads, Is.EqualTo(loads));
        }
        [UnityTest] public IEnumerator AllThreeModesKeepMovesAndAutosavesWithoutAnAd()
        {
            foreach (int size in new[] {3,4,5})
            {
                menu.SelectMode(size); campaign.PlayStage(1); var move = game.Stage.GetSolution()[0];
                game.TryMove(move.from, move.to); yield return Finished();
                Assert.That(new PuzzleSessionStore(campaign.SessionPath).TryLoad(game.Stage, out var saved), Is.True);
                CollectionAssert.AreEqual(game.Board.CopyCells(), saved.Cells); Assert.That(saved.MoveCount, Is.EqualTo(1));
                Assert.That(backend.Shows, Is.Zero); menu.ShowMainMenu();
            }
            yield return Load(); menu.Continue();
            Assert.That(game.Stage.BoardSize, Is.EqualTo(5)); Assert.That(game.MoveCount, Is.EqualTo(1)); Assert.That(backend.Shows, Is.Zero);
        }
        [UnityTest] public IEnumerator RestoringSolvedSnapshotCompletesUnlockButCannotGenerateAdOpportunity()
        {
            menu.SelectMode(3); campaign.PlayStage(1);
            var target = new PuzzleBoard(3, game.Stage.Target);
            new PuzzleSessionStore(campaign.SessionPath).Save(game.Stage, target, game.Stage.GetSolution().Length);
            menu.ShowMainMenu();
            ads.Frequency.State.totalClears = 5; ads.Frequency.State.gameplaySeconds = 180;
            menu.Continue(); Assert.That(game.IsCleared, Is.True); Assert.That(campaign.Progress.IsUnlocked(2), Is.True);
            campaign.PlayNext(); Assert.That(game.Stage.StageNumber, Is.EqualTo(2)); Assert.That(backend.Shows, Is.Zero);
            yield return null;
        }
        [UnityTest] public IEnumerator ClosingAdWhileBackgroundedDefersTransitionUntilForeground()
        {
            yield return EligibleClear(); campaign.PlayNext();
            Assert.That(backend.Shows, Is.EqualTo(1), $"SDK={ads.SdkReady}, ready={ads.AdReady}, consent={ads.CanRequestAds}, run={campaign.CurrentRunId}");
            backend.Opened();
            ads.SendMessage("OnApplicationPause", true); backend.Closed();
            Assert.That(game.Stage.StageNumber, Is.EqualTo(1)); Assert.That(campaign.IsTransitionPending, Is.True);
            ads.SendMessage("OnApplicationPause", false); ads.Tick();
            Assert.That(game.Stage.StageNumber, Is.EqualTo(2)); Assert.That(campaign.IsTransitionPending, Is.False);
        }
        [UnityTest] public IEnumerator AdCountsAndGameDataSurviveSceneReload()
        {
            yield return EligibleClear(); campaign.PlayNext(); backend.Opened(); backend.Closed();
            string path = campaign.SavePath; string progress = File.ReadAllText(path);
            var state = new AdFrequencyStore(Path.Combine(folder, "ads.json")).Load();
            Assert.That(state.hasShown, Is.True); Assert.That(state.totalClears, Is.EqualTo(5));
            yield return Load();
            Assert.That(ads.Frequency.State.hasShown, Is.True); Assert.That(ads.Frequency.State.totalClears, Is.EqualTo(5));
            Assert.That(File.ReadAllText(path), Is.EqualTo(progress));
            Assert.That(menu.CanContinue, Is.True); menu.Continue(); Assert.That(game.Stage.StageNumber, Is.EqualTo(2));
        }
        [Test] public void DelayedNonRequiredConsentFinishesDuringPuzzleWithoutShowingAnything()
        {
            backend = new FakeBackend { DelayConsent = true };
            ads.UseServices(backend, clock, Path.Combine(folder, "ads-delayed.json"));
            menu.SelectMode(5); campaign.PlayStage(1); ads.Tick(); Assert.That(backend.Initializations, Is.Zero);
            backend.ConsentUpdated(true); ads.Tick();
            Assert.That(backend.Initializations, Is.EqualTo(1)); Assert.That(ads.IsBlocking, Is.False);
            Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.Playing)); Assert.That(backend.Shows, Is.Zero);
        }
        [UnityTest] public IEnumerator InitializationFailureNeverBlocksClearOrNextStage()
        {
            backend = new FakeBackend { InitSuccess = false };
            ads.UseServices(backend, clock, Path.Combine(folder, "ads-init-fail.json")); ads.Tick();
            yield return EligibleClear(); campaign.PlayNext();
            Assert.That(backend.Initializations, Is.EqualTo(1)); Assert.That(backend.Loads, Is.Zero);
            Assert.That(game.Stage.StageNumber, Is.EqualTo(2)); Assert.That(campaign.Progress.IsCleared(1), Is.True);
        }
        [Test] public void RequiredPrivacyEntryReopensFormAndRevocationDiscardsPreparedAds()
        {
            backend = new FakeBackend { PrivacyRequired = true, DelayPrivacy = true };
            ads.UseServices(backend, clock, Path.Combine(folder, "ads-privacy.json")); ads.Tick(); menu.OpenSettings(); ads.Tick();
            var button = menu.Settings.transform.Find("SettingsPanelSafeArea/SettingsPanelContent/PrivacyOptionsButton").GetComponent<UnityEngine.UI.Button>();
            Assert.That(button.gameObject.activeSelf, Is.True); button.onClick.Invoke();
            Assert.That(ads.IsBlocking, Is.True); Assert.That(menu.HandleBack(), Is.True);
            Assert.That(menu.Screen, Is.EqualTo(PuzzleScreen.Settings));
            backend.Allowed = false; backend.PrivacyCompleted(); ads.Tick();
            Assert.That(ads.CanRequestAds, Is.False); Assert.That(ads.AdReady, Is.False); Assert.That(backend.Shows, Is.Zero);
        }
        [Test] public void ClosedTestKeepsConsentAndPrivacyButNeverInitializesOrLoadsAds()
        {
            backend = new FakeBackend { PrivacyRequired = true, DelayPrivacy = true };
            string path = Path.Combine(folder, "closed-privacy-frequency.json");
            ads.UseServices(backend, clock, path, false);
            clock.Now += 3600; ads.Tick();
            Assert.That(ads.AdvertisingEnabled, Is.False);
            Assert.That(ads.ConsentPermitsAds, Is.True); Assert.That(ads.CanRequestAds, Is.False);
            menu.OpenSettings(); ads.Tick();
            var button = menu.Settings.transform.Find("SettingsPanelSafeArea/SettingsPanelContent/PrivacyOptionsButton").GetComponent<UnityEngine.UI.Button>();
            Assert.That(button.gameObject.activeSelf, Is.True);
            button.onClick.Invoke(); Assert.That(ads.IsBlocking, Is.True);
            backend.PrivacyCompleted(); ads.Tick(); Assert.That(ads.IsBlocking, Is.False);
            ads.SendMessage("OnApplicationPause", true);
            Assert.That(File.Exists(path), Is.False);
            Assert.That(backend.ConsentUpdates, Is.EqualTo(1));
            Assert.That(backend.Initializations + backend.Loads + backend.RewardLoads + backend.Shows, Is.Zero);
        }
        [UnityTest] public IEnumerator ClosedTestAllModesClearRestartAndContinueWithoutChangingAdFrequency()
        {
            backend = new FakeBackend();
            string path = Path.Combine(folder, "closed-frequency.json");
            Assert.That(new AdFrequencyStore(path).Save(new AdFrequencyState { totalClears = 7, clearsSinceAd = 4, gameplaySeconds = 999 }), Is.True);
            string ledger = File.ReadAllText(path);
            ads.UseServices(backend, clock, path, false);
            foreach (int size in new[] { 3, 4, 5 })
            {
                menu.SelectMode(size); campaign.PlayStage(1);
                clock.Now += 3600; ads.Tick();
                foreach (var move in game.Stage.GetSolution()) { Assert.That(game.TryMove(move.from, move.to), Is.True); yield return Finished(); }
                Assert.That(game.IsCleared, Is.True); Assert.That(campaign.Progress.IsCleared(1), Is.True);
                campaign.PlayNext(); Assert.That(game.Stage.StageNumber, Is.EqualTo(2));
                var next = game.Stage.GetSolution()[0]; game.TryMove(next.from, next.to); yield return Finished();
                game.Restart(); Assert.That(game.MoveCount, Is.Zero);
                CollectionAssert.AreEqual(game.Stage.CreateStartingBoard().CopyCells(), game.Board.CopyCells());
                game.TryMove(next.from, next.to); yield return Finished();
                Assert.That(game.MoveCount, Is.EqualTo(1)); menu.ShowMainMenu();
            }
            menu.Continue(); Assert.That(game.Stage.BoardSize, Is.EqualTo(5));
            Assert.That(game.Stage.StageNumber, Is.EqualTo(2)); Assert.That(game.MoveCount, Is.EqualTo(1));
            ads.SendMessage("OnApplicationPause", true);
            Assert.That(File.ReadAllText(path), Is.EqualTo(ledger));
            Assert.That(backend.Initializations + backend.Loads + backend.RewardLoads + backend.Shows, Is.Zero);
        }
        sealed class FakeClock : IAdClock { public double Now { get; set; } }
        sealed class FakeBackend : IAdBackend, IFullScreenAd
        {
            public int Initializations, Loads, Shows, RewardLoads, ConsentUpdates;
            public bool Allowed = true, InitSuccess = true, DelayConsent, PrivacyRequired, DelayPrivacy; public bool CanShow { get; set; } = true;
            public Action Opened, Closed, Failed, PrivacyCompleted; public Action<bool> ConsentUpdated;
            public bool CanRequestAds => Allowed; public bool PrivacyOptionsRequired => PrivacyRequired; public bool ConsentRequired => false;
            public void UpdateConsent(Action<bool> done) { ConsentUpdates++; ConsentUpdated = done; if (!DelayConsent) done(true); }
            public void ShowRequiredConsent(Action done) => done();
            public void ShowPrivacyOptions(Action done) { PrivacyCompleted = done; if (!DelayPrivacy) done(); }
            public void Initialize(Action<bool> done) { Initializations++; done(InitSuccess); done(InitSuccess); }
            public void LoadInterstitial(string unit, Action<IFullScreenAd> done) { Loads++; done(this); }
            public void LoadRewarded(string unit, Action<IRewardedAd> done) { RewardLoads++; done(null); }
            public void Show(Action opened, Action closed, Action failed) { Shows++; Opened = opened; Closed = closed; Failed = failed; }
            public void Dispose() { }
        }
    }
}
