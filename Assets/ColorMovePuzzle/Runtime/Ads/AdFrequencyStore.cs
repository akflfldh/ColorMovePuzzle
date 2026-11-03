using System;
using System.IO;
using UnityEngine;

namespace ColorMovePuzzle.Ads
{
    public sealed class AdFrequencyStore
    {
        public string FilePath { get; }
        public AdFrequencyStore(string path) { FilePath = path; }
        public AdFrequencyState Load()
        {
            foreach (string file in new[] { FilePath, FilePath + ".bak" })
            {
                try
                {
                    if (!File.Exists(file) || new FileInfo(file).Length > 32768) continue;
                    var data = JsonUtility.FromJson<AdFrequencyState>(File.ReadAllText(file));
                    if (AdFrequencyController.IsValid(data)) return data;
                }
                catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException) { }
            }
            return new AdFrequencyState();
        }
        public bool Save(AdFrequencyState state)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath)));
                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(state));
                if (!File.Exists(FilePath)) File.Move(temp, FilePath);
                else
                {
                    try { File.Replace(temp, FilePath, FilePath + ".bak"); }
                    catch (PlatformNotSupportedException)
                    { File.Copy(FilePath, FilePath + ".bak", true); File.Copy(temp, FilePath, true); File.Delete(temp); }
                }
                return true;
            }
            catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException)
            { Debug.LogWarning("[Ads] Frequency state could not be saved. Game saves are independent."); return false; }
        }
    }
}
