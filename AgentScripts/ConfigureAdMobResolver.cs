var type = System.Linq.Enumerable.SelectMany(System.AppDomain.CurrentDomain.GetAssemblies(), a => { try { return a.GetTypes(); } catch { return System.Array.Empty<System.Type>(); } }).First(t => t.Name == "EDMResolverSettings");
var instance = (UnityEngine.ScriptableObject)type.BaseType.GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetValue(null);
var settings = new UnityEditor.SerializedObject(instance);
settings.FindProperty("android.promptBeforeAutoResolution.value").boolValue = false;
settings.ApplyModifiedPropertiesWithoutUndo();
type.GetMethod("Save", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, System.Type.EmptyTypes, null).Invoke(instance, null);
return new { promptBeforeAutoResolution = false, autoResolutionEnabled = settings.FindProperty("android.enableAutoResolution.value").boolValue, autoResolveOnBuild = settings.FindProperty("android.autoResolveOnBuild.value").boolValue };
