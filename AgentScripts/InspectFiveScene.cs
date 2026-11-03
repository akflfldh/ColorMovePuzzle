var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var game=UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleGameController>();
var menu=game.GetComponent<ColorMovePuzzle.PuzzleMenuController>();
var modes=UnityEditor.AssetDatabase.LoadAssetAtPath<ColorMovePuzzle.PuzzleModeCatalog>(ColorMovePuzzle.Editor.PuzzleMenuSceneBuilder.ModesPath);
int total=0;
foreach(int size in new[]{3,4,5})
{var mode=modes.Find(size);if(!mode.Available||mode.Stages.Count!=30||!game.SupportsSize(size))throw new System.Exception("Missing mode/catalog/view.");total+=mode.Stages.Count;}
var boards=UnityEngine.Object.FindObjectsByType<ColorMovePuzzle.PuzzleBoardView>(UnityEngine.FindObjectsInactive.Include);
var missing=new System.Collections.Generic.List<string>();
foreach(var component in UnityEngine.Object.FindObjectsByType<UnityEngine.MonoBehaviour>(UnityEngine.FindObjectsInactive.Include))
{
    if(!component||component.GetType().Namespace!="ColorMovePuzzle")continue;
    foreach(var field in component.GetType().GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic))
        if(field.GetCustomAttributes(typeof(UnityEngine.SerializeField),false).Length>0&&typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)&&!((UnityEngine.Object)field.GetValue(component)))missing.Add(component.name+"."+field.Name);
}
var snapshot=new {scene=scene.path,saved=!scene.isDirty,totalStages=total,modes=modes.Modes.Count,
    boards=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(boards,b=>new{name=b.name,size=b.Size,cells=b.transform.Find("Cells").childCount,tiles=b.Tiles.Count})),
    missingReferences=missing,eventSystems=UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length,
    meshRenderers=UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>().Length,
    rigidbodies=UnityEngine.Object.FindObjectsByType<UnityEngine.Rigidbody>().Length,
    colliders=UnityEngine.Object.FindObjectsByType<UnityEngine.Collider>().Length,
    mainMenuActive=menu.MainMenu.gameObject.activeSelf};
string json=Newtonsoft.Json.JsonConvert.SerializeObject(snapshot,Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("Artifacts/FiveByFive/SavedScene.json",json);
if(scene.isDirty||total!=90||missing.Count>0||snapshot.eventSystems!=1||snapshot.meshRenderers!=0||snapshot.rigidbodies!=0||snapshot.colliders!=0||!snapshot.mainMenuActive)throw new System.Exception("Saved scene verification failed.");
return json;
