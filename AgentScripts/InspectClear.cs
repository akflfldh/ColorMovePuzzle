var game = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleGameController>();
var snapshot = new {
    cleared = game.IsCleared, moves = game.MoveCount,
    matches = game.Board.Matches(game.Stage.Target),
    overlay = UnityEngine.GameObject.Find("ClearOverlay") != null,
    total = UnityEngine.GameObject.Find("ClearMoveCount").GetComponent<TMPro.TMP_Text>().text
};
if (!game.IsCleared || game.MoveCount != 24 || !game.Board.Matches(game.Stage.Target))
    throw new System.Exception("Live clear verification failed.");
var json = Newtonsoft.Json.JsonConvert.SerializeObject(snapshot, Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("Artifacts/Verification/ClearState.json", json);
return json;
