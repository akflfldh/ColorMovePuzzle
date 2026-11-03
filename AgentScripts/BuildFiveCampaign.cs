var catalog = ColorMovePuzzle.Editor.PuzzleFiveByFiveGenerator.GenerateCampaign();
ColorMovePuzzle.Editor.PuzzleFiveSceneBuilder.EnableCampaign();
return new { count=catalog.Count, firstLower=catalog.GetStage(1).LowerBound, lastLower=catalog.GetStage(30).LowerBound };
