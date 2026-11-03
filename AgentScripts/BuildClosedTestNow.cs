UnityEditor.PlayerSettings.Android.targetSdkVersion = (UnityEditor.AndroidSdkVersions)36;
UnityEditor.AssetDatabase.SaveAssets();
System.IO.Directory.CreateDirectory("Artifacts/ClosedTest");
System.IO.File.WriteAllText("Artifacts/ClosedTest/BuildState.json", "{\"status\":\"building\"}");
try {
    ColorMovePuzzle.Editor.ClosedTestReleaseBuild.Build();
    System.IO.File.WriteAllText("Artifacts/ClosedTest/BuildState.json", "{\"status\":\"completed\"}");
} catch (System.Exception exception) {
    System.IO.File.WriteAllText("Artifacts/ClosedTest/BuildState.json", "{\"status\":\"failed\"}");
    System.IO.File.WriteAllText("Artifacts/ClosedTest/BuildException.txt", exception.ToString());
    throw;
}
return new { state = System.IO.File.ReadAllText("Artifacts/ClosedTest/BuildState.json") };
