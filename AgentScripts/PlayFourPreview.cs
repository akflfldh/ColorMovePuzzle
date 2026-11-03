var flow = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleCampaignController>();
if (!flow.GetComponent<ColorMovePuzzle.PuzzleMenuController>().SelectMode(4)) throw new System.Exception("4x4 mode entry failed.");
if (!flow.PlayStage(1)) throw new System.Exception("4x4 stage entry failed.");
