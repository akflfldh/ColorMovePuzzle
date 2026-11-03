var game = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleGameController>();
var canvas = UnityEngine.Object.FindAnyObjectByType<UnityEngine.Canvas>();
return new {
    scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
    dirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,
    stage = game.Stage.name,
    boardSize = game.BoardView.Size,
    slots = game.BoardView.transform.Find("Cells").childCount,
    tiles = game.BoardView.Tiles.Count,
    targetSlots = UnityEngine.GameObject.Find("TargetGrid").transform.Find("Cells").childCount,
    canvasReference = canvas.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution,
    eventSystems = UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length,
    meshes = UnityEngine.Object.FindObjectsByType<UnityEngine.MeshFilter>().Length,
    colliders = UnityEngine.Object.FindObjectsByType<UnityEngine.Collider>().Length,
    inputActions = UnityEngine.Object.FindAnyObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().actionsAsset != null,
    orientation = UnityEditor.PlayerSettings.defaultInterfaceOrientation.ToString()
};
