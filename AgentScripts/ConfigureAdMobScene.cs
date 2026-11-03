ColorMovePuzzle.Editor.PuzzleAdMobSceneSetup.Apply();
var manager = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.Ads.AdManager>();
return new { scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path, manager = manager != null, sdk = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(GoogleMobileAds.Api.MobileAds).Assembly).version };
