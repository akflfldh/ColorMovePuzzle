var all = UnityEngine.Object.FindObjectsByType<ColorMovePuzzle.PuzzleBoardView>(UnityEngine.FindObjectsInactive.Include);
var target = System.Linq.Enumerable.Single(all, v => v.name=="TargetGrid5");
var hint=(UnityEngine.RectTransform)target.transform.Find("ZoomHint");
hint.anchoredPosition=new UnityEngine.Vector2(0,-174);
var enlarged=System.Linq.Enumerable.Single(all,v=>v.name=="ExpandedTarget5");
foreach(var label in enlarged.GetComponentsInChildren<TMPro.TMP_Text>(true))label.fontSize=48;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
