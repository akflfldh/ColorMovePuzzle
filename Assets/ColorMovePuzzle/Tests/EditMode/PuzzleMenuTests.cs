using System;
using System.IO;
using ColorMovePuzzle.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ColorMovePuzzle.Tests
{
    public sealed class PuzzleMenuTests
    {
        string path;
        [SetUp] public void Setup() => path = Path.Combine(Application.temporaryCachePath, "ColorMovePuzzleTests", Guid.NewGuid() + ".menu.json");
        [TearDown] public void Cleanup()
        {
            foreach (string suffix in new[] {"", ".bak", ".tmp", ".progress", ".progress.bak"})
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }

        [Test] public void FreshProfileHasVolumesButNoContinueHistory()
        {
            var profile = new PuzzleMenuProfileStore(path).Load();
            Assert.That(profile.HasContinue, Is.False);
            Assert.That(profile.BgmVolume, Is.EqualTo(0.7f));
            Assert.That(profile.SfxVolume, Is.EqualTo(0.8f));
        }

        [Test] public void OnlyExplicitStageRecordEnablesContinue()
        {
            var profile = new PuzzleMenuProfile();
            profile.SetVolumes(0.2f, 0.5f);
            Assert.That(profile.HasContinue, Is.False);
            profile.RecordStage(3, 12);
            Assert.That(profile.HasContinue, Is.True);
            Assert.That(profile.LastGridSize, Is.EqualTo(3));
            Assert.That(profile.LastStage, Is.EqualTo(12));
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.RecordStage(1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.RecordStage(3, 0));
        }

        [TestCase(-1f, 2f, 0f, 1f)]
        [TestCase(0.25f, 0.6f, 0.25f, 0.6f)]
        public void VolumeValuesAreNormalized(float bgm, float sfx, float expectedBgm, float expectedSfx)
        {
            var profile = new PuzzleMenuProfile();
            profile.SetVolumes(bgm, sfx);
            Assert.That(profile.BgmVolume, Is.EqualTo(expectedBgm));
            Assert.That(profile.SfxVolume, Is.EqualTo(expectedSfx));
        }

        [Test] public void NonFiniteVolumesAndInvalidHistoryAreSafe()
        {
            var profile = PuzzleMenuProfile.Restore(true, -3, 0, float.NaN, float.PositiveInfinity);
            Assert.That(profile.HasContinue, Is.False);
            Assert.That(profile.BgmVolume, Is.EqualTo(PuzzleMenuProfile.DefaultBgmVolume));
            Assert.That(profile.SfxVolume, Is.EqualTo(PuzzleMenuProfile.DefaultSfxVolume));
        }

        [Test] public void MenuFileRoundTripPreservesGridStageAndBothVolumes()
        {
            var profile = new PuzzleMenuProfile();
            profile.RecordStage(3, 8);
            profile.SetVolumes(0.13f, 0.91f);
            Assert.That(new PuzzleMenuProfileStore(path).Save(profile), Is.True);
            var loaded = new PuzzleMenuProfileStore(path).Load();
            Assert.That(loaded.HasContinue, Is.True);
            Assert.That(loaded.LastGridSize, Is.EqualTo(3));
            Assert.That(loaded.LastStage, Is.EqualTo(8));
            Assert.That(loaded.BgmVolume, Is.EqualTo(0.13f).Within(0.0001));
            Assert.That(loaded.SfxVolume, Is.EqualTo(0.91f).Within(0.0001));
        }

        [Test] public void MenuSavesNeverChangeLegacyProgressBytes()
        {
            var progress = new PuzzleProgress(30);
            progress.MarkCleared(1); progress.MarkCleared(2); progress.Select(3);
            var oldStore = new PuzzleProgressStore(path + ".progress", 30);
            Assert.That(oldStore.Save(progress), Is.True);
            string original = File.ReadAllText(oldStore.FilePath);
            var profile = new PuzzleMenuProfile();
            profile.RecordStage(3, 3); profile.SetVolumes(0.4f, 0.6f);
            Assert.That(new PuzzleMenuProfileStore(path).Save(profile), Is.True);
            Assert.That(File.ReadAllText(oldStore.FilePath), Is.EqualTo(original));
            Assert.That(oldStore.Load().ClearedCount, Is.EqualTo(2));
        }

        [Test] public void BrokenMenuFileUsesBackup()
        {
            var store = new PuzzleMenuProfileStore(path);
            var profile = new PuzzleMenuProfile();
            profile.RecordStage(3, 2); Assert.That(store.Save(profile), Is.True);
            profile.RecordStage(3, 3); Assert.That(store.Save(profile), Is.True);
            File.WriteAllText(path, "broken JSON");
            Assert.That(new PuzzleMenuProfileStore(path).Load().LastStage, Is.EqualTo(2));
        }

        [Test] public void UnsupportedVersionDoesNotInventContinueHistory()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "{\"version\":99,\"hasContinue\":true,\"lastGridSize\":3,\"lastStage\":30}");
            Assert.That(new PuzzleMenuProfileStore(path).Load().HasContinue, Is.False);
        }

        [Test] public void ModeDataOffersNinetyImplementedStagesAcrossThreeSizes()
        {
            var modes = AssetDatabase.LoadAssetAtPath<PuzzleModeCatalog>(PuzzleMenuSceneBuilder.ModesPath);
            Assert.That(modes, Is.Not.Null);
            Assert.That(modes.Modes.Count, Is.EqualTo(3));
            Assert.That(modes.Find(3).Available, Is.True);
            Assert.That(modes.Find(3).Stages.Count, Is.EqualTo(30));
            Assert.That(modes.Find(4).Available, Is.True);
            Assert.That(modes.Find(4).Stages.Count, Is.EqualTo(30));
            Assert.That(modes.Find(5).Available, Is.True);
            Assert.That(modes.Find(5).Stages.Count, Is.EqualTo(30));
            Assert.That(new PuzzleModeDefinition(4, true, modes.Find(3).Stages, "future.json").Available, Is.False,
                "A 3x3 catalog cannot masquerade as implemented 4x4 content.");
        }
    }
}
