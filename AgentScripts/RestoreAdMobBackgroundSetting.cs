UnityEditor.PlayerSettings.runInBackground = false;
UnityEngine.Application.runInBackground = false;
UnityEditor.AssetDatabase.SaveAssets();
return new { playerRunInBackground = UnityEditor.PlayerSettings.runInBackground, currentRunInBackground = UnityEngine.Application.runInBackground };
