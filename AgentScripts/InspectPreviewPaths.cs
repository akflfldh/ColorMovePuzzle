var flow=UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleCampaignController>();
var menu=flow.GetComponent<ColorMovePuzzle.PuzzleMenuController>();
return new {cwd=System.Environment.CurrentDirectory,save=flow.SavePath,profile=menu.ProfilePath,expected=System.IO.Path.GetFullPath("Artifacts/FiveByFive/Preview"),playing=UnityEngine.Application.isPlaying};
