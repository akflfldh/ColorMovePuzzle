if(UnityEngine.Application.isPlaying)throw new System.Exception("Stop Play mode first.");
var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(active.isDirty)throw new System.Exception("Unsaved scene changes require inspection.");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ColorMovePuzzle.Editor.PuzzleSceneBuilder.ScenePath);
