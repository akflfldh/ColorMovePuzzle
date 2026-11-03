using System;
using System.IO;
using UnityEngine;

namespace ColorMovePuzzle
{
    // Separate from progress-v1.json: menu changes never rewrite stage completion data.
    public sealed class PuzzleMenuProfileStore
    {
        [Serializable] sealed class Data
        {
            public int version;
            public bool hasContinue;
            public int lastGridSize;
            public int lastStage;
            public float bgmVolume = PuzzleMenuProfile.DefaultBgmVolume;
            public float sfxVolume = PuzzleMenuProfile.DefaultSfxVolume;
        }

        public string FilePath { get; }
        public string LastError { get; private set; }
        public bool HasAnyFile => File.Exists(FilePath) || File.Exists(FilePath + ".bak");
        public PuzzleMenuProfileStore(string path) { FilePath = path; }

        public PuzzleMenuProfile Load()
        {
            LastError = null;
            foreach (string candidate in new[] {FilePath, FilePath + ".bak"})
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    var data = JsonUtility.FromJson<Data>(File.ReadAllText(candidate));
                    if (data == null || data.version != 1) throw new InvalidDataException("Unsupported menu profile.");
                    return PuzzleMenuProfile.Restore(data.hasContinue, data.lastGridSize, data.lastStage, data.bgmVolume, data.sfxVolume);
                }
                catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException || error is ArgumentException)
                { LastError = error.Message; }
            }
            return new PuzzleMenuProfile();
        }

        public bool Save(PuzzleMenuProfile profile)
        {
            LastError = null;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath)));
                string temporary = FilePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(new Data {
                    version = 1, hasContinue = profile.HasContinue, lastGridSize = profile.LastGridSize,
                    lastStage = profile.LastStage, bgmVolume = profile.BgmVolume, sfxVolume = profile.SfxVolume}, true));
                if (!File.Exists(FilePath)) File.Move(temporary, FilePath);
                else
                {
                    try { File.Replace(temporary, FilePath, FilePath + ".bak"); }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(FilePath, FilePath + ".bak", true);
                        File.Copy(temporary, FilePath, true);
                        File.Delete(temporary);
                    }
                }
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            { LastError = error.Message; return false; }
        }
    }
}
