using System;

namespace ColorMovePuzzle
{
    public sealed class PuzzleMenuProfile
    {
        public const float DefaultBgmVolume = 0.7f;
        public const float DefaultSfxVolume = 0.8f;
        public bool HasContinue { get; private set; }
        public int LastGridSize { get; private set; }
        public int LastStage { get; private set; }
        public float BgmVolume { get; private set; } = DefaultBgmVolume;
        public float SfxVolume { get; private set; } = DefaultSfxVolume;

        public void RecordStage(int gridSize, int stage)
        {
            if (gridSize < 2 || stage < 1) throw new ArgumentOutOfRangeException();
            LastGridSize = gridSize;
            LastStage = stage;
            HasContinue = true;
        }

        public void SetVolumes(float bgm, float sfx)
        {
            BgmVolume = Normalize(bgm, DefaultBgmVolume);
            SfxVolume = Normalize(sfx, DefaultSfxVolume);
        }

        public static PuzzleMenuProfile Restore(bool hasContinue, int gridSize, int stage, float bgm, float sfx)
        {
            var profile = new PuzzleMenuProfile();
            if (hasContinue && gridSize >= 2 && stage >= 1) profile.RecordStage(gridSize, stage);
            profile.SetVolumes(bgm, sfx);
            return profile;
        }

        static float Normalize(float value, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(0, Math.Min(1, value));
    }
}
