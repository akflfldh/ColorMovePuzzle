var flow = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleCampaignController>();
if (!flow.PlayStage(1)) throw new System.Exception("Stage 1 failed to enter.");
