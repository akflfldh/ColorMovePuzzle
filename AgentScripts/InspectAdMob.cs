var config = UnityEditor.AssetDatabase.LoadAssetAtPath<ColorMovePuzzle.Ads.AdConfig>(ColorMovePuzzle.Editor.AdMobBuildValidation.ConfigPath);
var data = new {
    scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
    plugin = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(GoogleMobileAds.Api.MobileAds).Assembly).version,
    inputSystem = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UnityEngine.InputSystem.InputSystem).Assembly).version,
    defines = UnityEditor.PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Android),
    portrait = UnityEditor.PlayerSettings.defaultInterfaceOrientation.ToString(),
    il2cpp = UnityEditor.PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android).ToString(),
    architectures = UnityEditor.PlayerSettings.Android.targetArchitectures.ToString(),
    testAds = config.TestAds, firstClears = config.FirstClears, clearsBetween = config.ClearsBetweenAds,
    secondsBetween = config.SecondsBetweenAds, retries = config.MaximumLoadRetries,
    releaseError = config.ValidationError(false),
    appUiInPlayer = System.Linq.Enumerable.Any(UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Player), a => a.name == "Unity.AppUI"),
    adManagerCount = UnityEngine.Object.FindObjectsByType<ColorMovePuzzle.Ads.AdManager>().Length
};
System.IO.File.WriteAllText("Artifacts/AdMob/Configuration.json", Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
return data;
