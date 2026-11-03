using System;
using ColorMovePuzzle.Ads;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ColorMovePuzzle.Tests
{
    public sealed class ReleasePreparationTests
    {
        // Syntax-only fixtures; never written to assets, initialized with an SDK or sent to Google.
        static string Fixture(char separator) => "ca-app-pub-" + new string('0', 16) + separator + new string('0', 10);
        AdConfig config;
        [SetUp] public void Setup() { config = ScriptableObject.CreateInstance<AdConfig>(); }
        [TearDown] public void Cleanup() { UnityEngine.Object.DestroyImmediate(config); }
        void Set(string name, string value) { var s = new SerializedObject(config); s.FindProperty(name).stringValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
        void Flag(string name, bool value) { var s = new SerializedObject(config); s.FindProperty(name).boolValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
        void ReleaseShape()
        {
            Flag("testAds", false); Set("productionAppId", Fixture('~')); Set("productionInterstitialId", Fixture('/'));
        }
        [Test] public void AppAndUnitSeparatorsCannotBeInterchanged()
        {
            Assert.That(AdConfig.IsProductionId(Fixture('~'), '~'), Is.True);
            Assert.That(AdConfig.IsProductionId(Fixture('/'), '~'), Is.False);
            Assert.That(AdConfig.IsProductionId(Fixture('~'), '/'), Is.False);
        }
        [Test] public void IdSyntaxRejectsTrailingNewlineAndUnicodeDigits()
        {
            Assert.That(AdConfig.IsProductionId(Fixture('~') + "\n", '~'), Is.False);
            Assert.That(AdConfig.IsProductionId(" " + Fixture('~'), '~'), Is.False);
            Assert.That(AdConfig.IsProductionId(Fixture('~').Replace('0', '\u0660'), '~'), Is.False);
        }
        [Test] public void ReleaseNeverFallsBackToTestIdsWhenProductionIsMissing()
        {
            Flag("testAds", false); Flag("releasePrivacyAndAudienceReviewed", true);
            Assert.That(config.ValidationError(false), Does.Contain("App ID"));
            Assert.That(config.AppId, Is.Empty); Assert.That(config.InterstitialId, Is.Empty);
        }
        [Test] public void OwnerReviewIsRequiredBeforeRelease()
        {
            ReleaseShape(); Assert.That(config.ValidationError(false), Does.Contain("review is incomplete"));
            Assert.That(config.ValidationError(true), Does.Contain("Development builds must use"));
        }
        [Test] public void OptionalRewardedIdCannotBeAnOfficialTestIdInRelease()
        {
            ReleaseShape(); Flag("releasePrivacyAndAudienceReviewed", true);
            Set("productionRewardedId", AdConfig.TestRewardedId);
            Assert.That(config.ValidationError(false), Does.Contain("rewarded"));
            Set("productionRewardedId", ""); Assert.That(config.ValidationError(false), Is.Null);
        }
        [Test] public void DevelopmentConsentAppIdNeverSelectsProductionAdUnits()
        {
            Set("developmentConsentAppId", Fixture('~'));
            Assert.That(config.ValidationError(true), Is.Null);
            Assert.That(config.AppId, Is.EqualTo(Fixture('~')));
            Assert.That(config.InterstitialId, Is.EqualTo(AdConfig.TestInterstitialId));
            Assert.That(config.RewardedId, Is.EqualTo(AdConfig.TestRewardedId));
            Assert.That(config.ValidationError(false), Does.Contain("Release build blocked"));
        }
        [Test] public void DevelopmentConsentCannotUseAnAdUnitAsAnAppId()
        {
            Set("developmentConsentAppId", Fixture('/'));
            Assert.That(config.ValidationError(true), Does.Contain("consent App ID"));
        }
        [Test] public void UmpPrivacyOptionsUseSdkPermissionAfterEveryReentry()
        {
            var backend = new ConsentBackend(); var consent = new AdConsentController(backend);
            consent.Start(); consent.Start(); Assert.That(backend.Updates, Is.EqualTo(1));
            backend.Updated(true); consent.Tick(true); backend.FormReturned();
            Assert.That(consent.CanRequestAds, Is.False);
            backend.Allowed = true; Assert.That(consent.CanRequestAds, Is.True);
            Assert.That(consent.ShowPrivacyOptions(), Is.True);
            Assert.That(consent.ShowPrivacyOptions(), Is.False); Assert.That(consent.CanRequestAds, Is.False);
            backend.Allowed = false; backend.FormReturned(); Assert.That(consent.CanRequestAds, Is.False);
            Assert.That(backend.Options, Is.EqualTo(1));
            Assert.That(consent.ShowPrivacyOptions(), Is.True);
            backend.Allowed = true; backend.FormReturned(); Assert.That(consent.CanRequestAds, Is.True);
            Assert.That(backend.Options, Is.EqualTo(2));
        }
        [TestCase("")]
        [TestCase(AdConfig.TestAppId)]
        [TestCase(AdConfig.TestInterstitialId)]
        public void ClosedTestRejectsMissingOrTestOrAdUnitConsentAppId(string value)
        {
            Assert.That(ColorMovePuzzle.Editor.ClosedTestReleaseBuild.ValidateConsentAppId(value), Is.Not.Null);
        }
        [Test] public void ClosedTestConsentAppIdDoesNotUnlockStandardProductionRelease()
        {
            Assert.That(ColorMovePuzzle.Editor.ClosedTestReleaseBuild.ValidateConsentAppId("ca-app-pub-9556737105305150~6563007441"), Is.Null);
            Assert.That(config.ValidationError(false), Does.Contain("Release build blocked"));
            Assert.That(config.ProductionIdsError(), Is.Not.Null);
            Assert.That(AdBuildPolicy.AllowAdRequests, Is.True, "The Editor and standard Development/production path retain the existing ad policy.");
        }
        sealed class ConsentBackend : IAdBackend
        {
            public int Updates, Options; public bool Allowed;
            public Action<bool> Updated; public Action FormReturned;
            public bool CanRequestAds => Allowed;
            public bool PrivacyOptionsRequired => true;
            public bool ConsentRequired => true;
            public void UpdateConsent(Action<bool> callback) { Updates++; Updated = callback; }
            public void ShowRequiredConsent(Action callback) { FormReturned = callback; }
            public void ShowPrivacyOptions(Action callback) { Options++; FormReturned = callback; }
            public void Initialize(Action<bool> callback) { throw new InvalidOperationException("Consent test must never initialize ads."); }
            public void LoadInterstitial(string unit, Action<IFullScreenAd> callback) { throw new InvalidOperationException(); }
            public void LoadRewarded(string unit, Action<IRewardedAd> callback) { throw new InvalidOperationException(); }
        }
    }
}
