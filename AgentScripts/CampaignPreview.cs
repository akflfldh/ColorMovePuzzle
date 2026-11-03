var flow = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleCampaignController>();
var path = System.IO.Path.GetFullPath("Artifacts/StageSystem/PreviewProgress.json");
var store = new ColorMovePuzzle.PuzzleProgressStore(path, 30);
store.Save(new ColorMovePuzzle.PuzzleProgress(30));
flow.UseProgressStore(store);
flow.ShowSelection();
return new {cards = flow.Selection.Buttons.Count, unlocked = flow.Progress.NextStage, savePath = path};
