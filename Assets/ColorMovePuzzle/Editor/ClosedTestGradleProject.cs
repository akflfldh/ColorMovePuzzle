using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;

namespace ColorMovePuzzle.Editor
{
    public sealed class ClosedTestGradleProject : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 10000;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (!UnityEditor.SessionState.GetBool("ColorMovePuzzle.ClosedTestBuild.Active", false)) return;
            string launcher = Path.Combine(Directory.GetParent(path).FullName, "launcher", "build.gradle");
            string text = File.ReadAllText(launcher);
            var release = new Regex(@"(?s)(\brelease\s*\{)([^{}]*)(\})");
            var match = release.Match(text);
            if (!match.Success) throw new System.InvalidOperationException("Cannot locate generated release Gradle block for external signing.");
            text = release.Replace(text, m => m.Groups[1].Value + Regex.Replace(m.Groups[2].Value, @"(?m)^\s*signingConfig\s+[^\r\n]+", "") + m.Groups[3].Value, 1);
            File.WriteAllText(launcher, text);
        }
    }
}
