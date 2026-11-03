var game=UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleGameController>();
return new {frame=UnityEngine.Time.frameCount,playing=UnityEngine.Application.isPlaying,size=game?.Board?.Size,stage=game?.Stage?.StageNumber,moves=game?.MoveCount,moving=game?.IsMoving};
