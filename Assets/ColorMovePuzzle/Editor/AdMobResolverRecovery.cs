using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace ColorMovePuzzle.Editor
{
    // One-shot recovery, gated by a workspace marker; never changes player settings or runtime consent.
    public static class AdMobResolverRecovery
    {
        [InitializeOnLoadMethod]
        static void RecoverIfRequested()
        {
            const string marker = "Artifacts/AdMob/ResolverRecovery.request";
            if (!File.Exists(marker)) return;
            try
            {
                var type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => {
                    try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                }).First(t => t.Name == "EDMResolverSettings");
                var instance = (UnityEngine.ScriptableObject)type.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                var settings = new SerializedObject(instance);
                settings.FindProperty("android.promptBeforeAutoResolution.value").boolValue = false;
                settings.ApplyModifiedPropertiesWithoutUndo();
                type.GetMethod("Save", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null).Invoke(instance, null);
                File.WriteAllText("Artifacts/AdMob/ResolverRecovery.json", "{\"promptBeforeAutoResolution\":false,\"autoResolutionUnchanged\":true}");
                File.Delete(marker);
            }
            catch (Exception error) { UnityEngine.Debug.LogError("AdMob Resolver recovery did not complete: " + error.Message); }
        }
    }
}
