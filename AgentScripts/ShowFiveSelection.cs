var menu=UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleMenuController>();
menu.ShowMainMenu();
if(!menu.SelectMode(5))throw new System.Exception("5x5 selection failed.");
