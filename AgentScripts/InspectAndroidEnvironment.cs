var named=UnityEditor.Build.NamedBuildTarget.Android;
var externalType=System.Linq.Enumerable.FirstOrDefault(System.Linq.Enumerable.Select(System.AppDomain.CurrentDomain.GetAssemblies(),a=>a.GetType("UnityEditor.Android.AndroidExternalToolsSettings")),t=>t!=null);
var roots=new System.Collections.Generic.Dictionary<string,string>();
if(externalType!=null)foreach(var p in externalType.GetProperties(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static))
    if(p.PropertyType==typeof(string)&&p.GetIndexParameters().Length==0&&(p.Name.ToLowerInvariant().Contains("path")||p.Name.ToLowerInvariant().Contains("root")))
        try{roots[p.Name]=(string)p.GetValue(null);}catch(System.Exception e){roots[p.Name]="ERROR: "+e.GetBaseException().Message;}
var settings=new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
var input=settings.FindProperty("activeInputHandler");
var snapshot=new {unity=UnityEngine.Application.unityVersion,editor=UnityEditor.EditorApplication.applicationPath,
    contents=UnityEditor.EditorApplication.applicationContentsPath,androidSupported=UnityEditor.BuildPipeline.IsBuildTargetSupported(UnityEditor.BuildTargetGroup.Android,UnityEditor.BuildTarget.Android),
    activeTarget=UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString(),playing=UnityEditor.EditorApplication.isPlaying,
    scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,sceneSaved=!UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,
    packageName=UnityEditor.PlayerSettings.GetApplicationIdentifier(named),orientation=UnityEditor.PlayerSettings.defaultInterfaceOrientation.ToString(),
    scriptingBackend=UnityEditor.PlayerSettings.GetScriptingBackend(named).ToString(),architectures=UnityEditor.PlayerSettings.Android.targetArchitectures.ToString(),
    minimumSdk=UnityEditor.PlayerSettings.Android.minSdkVersion.ToString(),targetSdk=UnityEditor.PlayerSettings.Android.targetSdkVersion.ToString(),
    inputHandler=input==null?-1:input.intValue,development=UnityEditor.EditorUserBuildSettings.development,
    appBundle=UnityEditor.EditorUserBuildSettings.buildAppBundle,exportProject=UnityEditor.EditorUserBuildSettings.exportAsGoogleAndroidProject,
    scenes=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(UnityEditor.EditorBuildSettings.scenes,s=>new{path=s.path,enabled=s.enabled})),externalTools=roots};
System.IO.Directory.CreateDirectory("Artifacts/Android");
string json=Newtonsoft.Json.JsonConvert.SerializeObject(snapshot,Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText(System.IO.File.Exists("Artifacts/Android/EnvironmentBefore.json")?"Artifacts/Android/EnvironmentAfter.json":"Artifacts/Android/EnvironmentBefore.json",json);
return json;
