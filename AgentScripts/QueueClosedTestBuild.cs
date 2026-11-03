System.IO.Directory.CreateDirectory("Artifacts/ClosedTest");
System.IO.File.WriteAllText("Artifacts/ClosedTest/BuildState.json", "{\"status\":\"queued\"}");
UnityEditor.EditorApplication.delayCall += () => {
    try {
        System.IO.File.WriteAllText("Artifacts/ClosedTest/BuildState.json", "{\"status\":\"building\"}");
        ColorMovePuzzle.Editor.ClosedTestReleaseBuild.Build();
        System.IO.File.WriteAllText("Artifacts/ClosedTest/BuildState.json", "{\"status\":\"completed\"}");
    }
    catch (System.Exception exception) {
        System.IO.File.WriteAllText("Artifacts/ClosedTest/BuildState.json", "{\"status\":\"failed\"}");
        System.IO.File.WriteAllText("Artifacts/ClosedTest/BuildException.txt", exception.ToString());
        UnityEngine.Debug.LogError("Closed-test build failed; inspect Artifacts/ClosedTest/BuildException.txt.");
    }
};
return new { queued = true, channel = "ClosedTestNoAds", signing = "Separate JDK step after user approval" };
