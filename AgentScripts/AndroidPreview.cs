var campaign=UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleCampaignController>();
var menu=campaign.GetComponent<ColorMovePuzzle.PuzzleMenuController>();
string folder=System.IO.Path.GetFullPath("Artifacts/Android/Preview");
foreach(string name in new[]{"progress-v1.json","progress-4x4-v1.json","progress-5x5-v1.json"})
    new ColorMovePuzzle.PuzzleProgressStore(System.IO.Path.Combine(folder,name),30).Save(new ColorMovePuzzle.PuzzleProgress(30));
var profile=new ColorMovePuzzle.PuzzleMenuProfileStore(System.IO.Path.Combine(folder,"menu.json"));profile.Save(new ColorMovePuzzle.PuzzleMenuProfile());
campaign.UseProgressDirectory(folder);menu.UseProfileStore(profile);menu.ShowMainMenu();
return new{save=campaign.SavePath,profile=menu.ProfilePath};
