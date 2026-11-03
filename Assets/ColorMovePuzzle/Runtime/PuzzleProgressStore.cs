using System;
using System.IO;
using UnityEngine;

namespace ColorMovePuzzle
{
    public sealed class PuzzleProgressStore
    {
        [Serializable] sealed class SaveData
        {
            public int version = 1;
            public int[] clearedStages;
            public int lastPlayed = 1;
        }

        readonly int stageCount;
        public string FilePath { get; }
        public string LastError { get; private set; }
        public PuzzleProgressStore(string path, int count) { FilePath = path; stageCount = count; }

        public PuzzleProgress Load()
        {
            LastError = null;
            foreach (string candidate in new[] {FilePath, FilePath + ".bak"})
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(candidate));
                    if (data == null || data.version != 1 || data.clearedStages == null)
                        throw new InvalidDataException("Unsupported or incomplete progress data.");
                    return PuzzleProgress.Restore(stageCount, data.clearedStages, data.lastPlayed);
                }
                catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException || error is ArgumentException)
                { LastError = error.Message; }
            }
            return new PuzzleProgress(stageCount);
        }

        public bool Save(PuzzleProgress progress)
        {
            LastError = null;
            try
            {
                string directory = Path.GetDirectoryName(Path.GetFullPath(FilePath));
                Directory.CreateDirectory(directory);
                string temporary = FilePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(new SaveData {
                    clearedStages = progress.CompletedStages(), lastPlayed = progress.LastPlayed}, true));
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
