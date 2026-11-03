var modes = UnityEditor.AssetDatabase.LoadAssetAtPath<ColorMovePuzzle.PuzzleModeCatalog>("Assets/ColorMovePuzzle/Data/Modes.asset");
var data = new {
    palette = System.Linq.Enumerable.Range(0, 7).Select(i => i == 0 ? "19253A" : UnityEngine.ColorUtility.ToHtmlStringRGB(ColorMovePuzzle.PuzzlePalette.Tile((ColorMovePuzzle.TileColor)i))).ToArray(),
    stages = System.Linq.Enumerable.ToArray(modes.Modes.SelectMany(m => m.Stages.Stages.Select(s => new {
        size = s.BoardSize, number = s.StageNumber, cells = s.CreateStartingBoard().CopyCells().Select(c => (int)c).ToArray(),
        target = s.Target.Select(c => (int)c).ToArray(), solution = s.GetSolution().Select(step => new { from = step.from, to = step.to }).ToArray()
    })))
};
System.IO.File.WriteAllText("Artifacts/AdMob/DeviceModels.json", Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
return new { stages = data.stages.Length };
