var modes=UnityEditor.AssetDatabase.LoadAssetAtPath<ColorMovePuzzle.PuzzleModeCatalog>("Assets/ColorMovePuzzle/Data/Modes.asset");
var records=new System.Collections.Generic.List<object>();
foreach(var mode in modes.Modes)foreach(var stage in mode.Stages.Stages)
    records.Add(new{size=stage.BoardSize,number=stage.StageNumber,cells=System.Linq.Enumerable.Select(stage.CreateStartingBoard().CopyCells(),c=>(int)c).ToArray()});
var palette=System.Linq.Enumerable.Range(0,7).Select(i=>i==0?"19253A":UnityEngine.ColorUtility.ToHtmlStringRGB(ColorMovePuzzle.PuzzlePalette.Tile((ColorMovePuzzle.TileColor)i))).ToArray();
System.IO.File.WriteAllText("Artifacts/AndroidStabilization/DeviceModels.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{stages=records,palette=palette}));
return new{stages=records.Count};
