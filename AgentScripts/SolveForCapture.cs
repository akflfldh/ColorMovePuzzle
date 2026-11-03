using System;
using System.Collections;
using ColorMovePuzzle;
using UnityEngine;

public static class PuzzleSolutionCapture
{
    public static void Main()
    {
        var game = UnityEngine.Object.FindAnyObjectByType<PuzzleGameController>();
        game.Restart();
        game.StartCoroutine(Solve(game));
    }

    static IEnumerator Solve(PuzzleGameController game)
    {
        foreach (var move in game.Stage.GetSolution())
        {
            if (!game.TryMove(move.from, move.to)) throw new Exception("Recorded solution failed.");
            while (game.IsMoving) yield return null;
        }
        if (!game.IsCleared) throw new Exception("Clear state not reached.");
    }
}
