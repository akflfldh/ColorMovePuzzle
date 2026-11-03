var type = System.Linq.Enumerable.SelectMany(System.AppDomain.CurrentDomain.GetAssemblies(), a => { try { return a.GetTypes(); } catch { return System.Array.Empty<System.Type>(); } }).First(t => t.FullName == "GoogleMobileAds.Editor.GoogleMobileAdsSettings");
var asset = (UnityEngine.ScriptableObject)type.GetMethod("LoadInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
var settings = new UnityEditor.SerializedObject(asset);
settings.FindProperty("adMobAndroidAppId").stringValue = "ca-app-pub-3940256099942544~3347511713";
settings.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(asset);
UnityEditor.AssetDatabase.SaveAssets();
return new { settingsPath = UnityEditor.AssetDatabase.GetAssetPath(asset), androidTestAppIdConfigured = true, edm = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.SelectMany(System.AppDomain.CurrentDomain.GetAssemblies(), a => { try { return a.GetTypes(); } catch { return System.Array.Empty<System.Type>(); } }).Where(t => t.Name == "PlayServicesResolver").Select(t => new { type = t.FullName, methods = t.GetMethods().Where(m => m.Name.Contains("Resolve")).Select(m => m.ToString()).ToArray() })) };
