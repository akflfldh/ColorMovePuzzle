using UnityEngine;

namespace ColorMovePuzzle.Editor
{
    // Editor-only asset. Contains public build metadata; never signing credentials or ad units.
    public sealed class ClosedTestReleaseSettings : ScriptableObject
    {
        public string consentAppId;
        public string versionName = "1.0";
        public int versionCode = 1;
    }
}
