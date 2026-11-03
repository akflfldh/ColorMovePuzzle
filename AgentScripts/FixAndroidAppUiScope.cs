if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Stop Play mode first.");
if(!Unity.AppUI.Core.AppUI.settings.editorOnly)throw new System.Exception("Do not exclude an AppUI runtime the project intends to use.");
var target=UnityEditor.Build.NamedBuildTarget.Android;
var symbols=new System.Collections.Generic.HashSet<string>(UnityEditor.PlayerSettings.GetScriptingDefineSymbols(target).Split(new[]{';'},System.StringSplitOptions.RemoveEmptyEntries));
bool changed=symbols.Add("APP_UI_EDITOR_ONLY");
if(changed)UnityEditor.PlayerSettings.SetScriptingDefineSymbols(target,string.Join(";",symbols));
UnityEditor.AssetDatabase.SaveAssets();
return new{changed=changed,editorOnly=Unity.AppUI.Core.AppUI.settings.editorOnly,defines=string.Join(";",symbols),note="Honor the existing AppUI Editor Only setting on Android; keep packages and game UI unchanged."};
