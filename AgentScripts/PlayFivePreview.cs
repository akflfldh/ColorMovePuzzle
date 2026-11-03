var menu=UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleMenuController>();
if(!menu.SelectMode(5))throw new System.Exception("5x5 unavailable.");
if(!menu.GetComponent<ColorMovePuzzle.PuzzleCampaignController>().PlayStage(1))throw new System.Exception("5x5 stage 1 unavailable.");
