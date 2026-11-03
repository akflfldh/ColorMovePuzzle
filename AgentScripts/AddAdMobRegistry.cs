var method = System.Linq.Enumerable.Single(typeof(UnityEditor.PackageManager.Client).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static), m => m.Name == "AddScopedRegistry" && m.GetParameters().Length == 4 && m.GetParameters()[0].ParameterType == typeof(string));
var request = (UnityEditor.PackageManager.Requests.Request)method.Invoke(null, new object[] { "OpenUPM Google", "https://package.openupm.com", new[] { "com.google.ads.mobile" }, false });
UnityEditor.EditorApplication.CallbackFunction poll = null;
poll = () => {
    if (!request.IsCompleted) return;
    UnityEditor.EditorApplication.update -= poll;
    System.IO.File.WriteAllText("Artifacts/AdMob/RegistryStatus.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { status = request.Status.ToString(), error = request.Error?.message }, Newtonsoft.Json.Formatting.Indented));
};
UnityEditor.EditorApplication.update += poll;
return "Scoped registry requested through Unity Package Manager API.";
