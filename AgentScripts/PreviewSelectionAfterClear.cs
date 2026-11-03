var flow = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleCampaignController>();
if (!flow.Progress.IsCleared(1) || !flow.Progress.IsUnlocked(2)) throw new System.Exception("Stage 2 was not unlocked.");
flow.ShowSelection();
