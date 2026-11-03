var flow=UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleCampaignController>();
var menu=flow.GetComponent<ColorMovePuzzle.PuzzleMenuController>();
var folder=System.IO.Path.GetFullPath("Artifacts/FiveByFive/Preview");
foreach(string name in new[]{"progress-v1.json","progress-4x4-v1.json","progress-5x5-v1.json"})
    new ColorMovePuzzle.PuzzleProgressStore(System.IO.Path.Combine(folder,name),30).Save(new ColorMovePuzzle.PuzzleProgress(30));
var store=new ColorMovePuzzle.PuzzleMenuProfileStore(System.IO.Path.Combine(folder,"menu.json"));store.Save(new ColorMovePuzzle.PuzzleMenuProfile());
flow.UseProgressDirectory(folder);menu.UseProfileStore(store);menu.ShowMainMenu();
return new {screen=menu.Screen.ToString(),continueEnabled=menu.CanContinue};
