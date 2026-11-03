var events=UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>();
var modules=UnityEngine.Object.FindObjectsByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
var app=Unity.AppUI.Core.AppUI.settings;
var named=UnityEditor.Build.NamedBuildTarget.Android;
var snapshot=new{inputVersion=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UnityEngine.InputSystem.InputSystem).Assembly).version,
    updateMode=UnityEngine.InputSystem.InputSystem.settings.updateMode.ToString(),eventSystems=events.Length,inputModules=modules.Length,
    modules=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(modules,m=>new{point=m.point?.action?.name,click=m.leftClick?.action?.name,move=m.move?.action?.name})),
    appUISettings=UnityEditor.AssetDatabase.GetAssetPath(app),appUIEditorOnly=app.editorOnly,defines=UnityEditor.PlayerSettings.GetScriptingDefineSymbols(named),
    splitBinary=UnityEditor.PlayerSettings.Android.useAPKExpansionFiles,appBundle=UnityEditor.EditorUserBuildSettings.buildAppBundle,
    playerAssemblies=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Player),a=>a.name))};
string json=Newtonsoft.Json.JsonConvert.SerializeObject(snapshot,Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText(System.IO.File.Exists("Artifacts/AndroidStabilization/ConfigurationBefore.json")?"Artifacts/AndroidStabilization/ConfigurationAfter.json":"Artifacts/AndroidStabilization/ConfigurationBefore.json",json);return json;
