var flow=UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleCampaignController>();var menu=flow.GetComponent<ColorMovePuzzle.PuzzleMenuController>();
string folder=System.IO.Path.GetFullPath("Artifacts/Android/Preview");flow.UseProgressDirectory(folder);menu.UseProfileStore(new ColorMovePuzzle.PuzzleMenuProfileStore(System.IO.Path.Combine(folder,"menu.json")));menu.ShowMainMenu();
if(!menu.Continue())throw new System.Exception("Saved Continue could not be restored.");
var game=flow.GetComponent<ColorMovePuzzle.PuzzleGameController>();
bool correct=game.Stage.BoardSize==5&&game.Stage.StageNumber==2&&game.MoveCount==0&&game.Board.Matches(game.Stage.CreateStartingBoard().CopyCells());
bool volumes=UnityEngine.Mathf.Abs(menu.Profile.BgmVolume-.2f)<.02f&&UnityEngine.Mathf.Abs(menu.Profile.SfxVolume-.55f)<.02f;
foreach(int size in new[]{3,4,5})
{
    string name=size==3?"progress-v1.json":$"progress-{size}x{size}-v1.json";
    var progress=new ColorMovePuzzle.PuzzleProgressStore(System.IO.Path.Combine(folder,name),30).Load();
    if(progress.ClearedCount!=1||progress.LastPlayed!=2)throw new System.Exception("Mode progress was not restored independently.");
}
string json=Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=correct&&volumes,environment="new Unity Editor Play session, not Android process relaunch",grid=game.Stage.BoardSize,stage=game.Stage.StageNumber,moves=game.MoveCount,initialBoardRestored=correct,volumesRestored=volumes,allModesProgressRestored=true});
System.IO.File.WriteAllText("Artifacts/Android/EditorRestartPersistence.json",json);
if(!correct||!volumes)throw new System.Exception("Restart/profile behavior changed.");return json;
