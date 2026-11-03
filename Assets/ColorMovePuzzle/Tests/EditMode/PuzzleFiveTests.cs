using System;
using System.Linq;
using System.Collections.Generic;
using ColorMovePuzzle.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ColorMovePuzzle.Tests
{
    public sealed class PuzzleFiveTests
    {
        static readonly int[] Inventory={3,4,4,4,2,4,4};
        [Test] public void ThirtyFiveStagesHaveExactInventoryUniqueStartsAndVerifiedReversiblePaths()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<PuzzleStageCatalog>(PuzzleFiveByFiveGenerator.CatalogPath);
            Assert.That(catalog.Count,Is.EqualTo(30));var seen=new HashSet<string>();int previous=0;
            foreach(var stage in catalog.Stages)
            {
                var board=stage.CreateStartingBoard();var start=board.CopyCells();
                Assert.That(board.Size,Is.EqualTo(5));Assert.That(board.Count,Is.EqualTo(25));
                foreach(var cells in new[]{start,stage.Target})CollectionAssert.AreEqual(Inventory,Enumerable.Range(0,7).Select(c=>cells.Count(x=>(int)x==c)).ToArray());
                Assert.That(seen.Add(PuzzleFiveByFiveGenerator.Encode(start)),Is.True);Assert.That(board.Matches(stage.Target),Is.False);
                foreach(var move in stage.GetSolution())
                {Assert.That(board.TryMove(move.from,move.to),Is.True);CollectionAssert.AreEqual(Inventory,Enumerable.Range(0,7).Select(c=>board.CopyCells().Count(x=>(int)x==c)).ToArray());}
                Assert.That(board.Matches(stage.Target),Is.True);
                var reverse=new PuzzleBoard(5,stage.Target);var visited=new HashSet<string>{PuzzleFiveByFiveGenerator.Encode(stage.Target)};
                foreach(var move in stage.Scramble){Assert.That(reverse.TryMove(move.from,move.to),Is.True);Assert.That(visited.Add(PuzzleFiveByFiveGenerator.Encode(reverse.CopyCells())),Is.True,"No repeated state or immediate reversal in witness.");}
                CollectionAssert.AreEqual(start,reverse.CopyCells());
                Assert.That(stage.LowerBound,Is.EqualTo(PuzzleMatchingHeuristic.Evaluate(5,start,stage.Target)));
                Assert.That(stage.LowerBound,Is.GreaterThanOrEqualTo(previous));previous=stage.LowerBound;
                Assert.That(stage.KnownSolutionLength,Is.InRange(stage.LowerBound,PuzzleFiveByFiveGenerator.MaxSteps));
                Assert.That(stage.GenerationMoves,Is.EqualTo(stage.Scramble.Length));
                Assert.That(stage.MinimumMoves,Is.Zero);Assert.That(stage.HasExactMinimum,Is.False);
                Assert.That(stage.DifficultyBand,Is.EqualTo(PuzzleFiveByFiveGenerator.Band(stage.StageNumber)));
                Assert.That(stage.EvaluationNodes,Is.InRange(1,1024));Assert.That(stage.EvaluationResidual,Is.InRange(0,stage.LowerBound));
                if(stage.StageNumber<=5)Assert.That(stage.LowerBound,Is.GreaterThanOrEqualTo(8));
            }
            Assert.That(catalog.GetStage(30).LowerBound,Is.GreaterThanOrEqualTo(40));
        }
        [TestCase(17,22,true)][TestCase(18,23,true)][TestCase(19,24,true)][TestCase(21,22,true)]
        [TestCase(17,23,false)][TestCase(19,23,false)][TestCase(22,23,false)][TestCase(0,24,false)]
        public void FiveUsesSharedOrthogonalMovementRules(int from,int to,bool expected)
            =>Assert.That(new PuzzleBoard(5,PuzzleFiveByFiveGenerator.Target).TryMove(from,to),Is.EqualTo(expected));
        [Test] public void ThreeAdjacentEmptyCellsRemainInterchangeableAndAllDirectionsAvailable()
        {
            var cells=(TileColor[])PuzzleFiveByFiveGenerator.Target.Clone();
            foreach(var pair in new[]{(7,22),(11,23),(13,24)}){var c=cells[pair.Item1];cells[pair.Item1]=cells[pair.Item2];cells[pair.Item2]=c;}
            var board=new PuzzleBoard(5,cells);CollectionAssert.AreEquivalent(new[]{7,11,13},board.GetEmptyNeighbors(12));
            foreach(int to in board.GetEmptyNeighbors(12)){var copy=new PuzzleBoard(5,cells);Assert.That(copy.TryMove(12,to),Is.True);Assert.That(copy.TryMove(to,12),Is.True);Assert.That(copy.Matches(cells),Is.True);}
            Assert.That(board.TryMove(12,8),Is.False);Assert.That(board.TryMove(10,14),Is.False);
            var goal=(TileColor[])PuzzleFiveByFiveGenerator.Target.Clone();var first=goal[16];goal[16]=goal[19];goal[19]=first;
            Assert.That(new PuzzleBoard(5,goal).Matches(PuzzleFiveByFiveGenerator.Target),Is.True);
            Assert.That((int)TileColor.Black,Is.EqualTo(4));Assert.That((int)TileColor.Green,Is.EqualTo(5));Assert.That((int)TileColor.Purple,Is.EqualTo(6));
        }
        [Test] public void MatchingIsOptimalForAllCopiesAndChangesByAtMostOnePerLegalMove()
        {
            var board=new PuzzleBoard(5,PuzzleFiveByFiveGenerator.Target);var rng=new System.Random(505);
            for(int i=0;i<100;i++)
            {
                var options=Enumerable.Range(0,25).SelectMany(from=>board.GetEmptyNeighbors(from).Select(to=>new PuzzleMove(from,to))).ToArray();
                var move=options[rng.Next(options.Length)];int before=PuzzleMatchingHeuristic.Evaluate(5,board.CopyCells(),PuzzleFiveByFiveGenerator.Target);
                board.TryMove(move.from,move.to);var cells=board.CopyCells();int after=PuzzleMatchingHeuristic.Evaluate(5,cells,PuzzleFiveByFiveGenerator.Target);
                Assert.That(Math.Abs(before-after),Is.LessThanOrEqualTo(1));int colored=0,empty=0;
                for(int color=0;color<7;color++)
                {
                    var from=Enumerable.Range(0,25).Where(p=>(int)cells[p]==color).ToArray();var to=Enumerable.Range(0,25).Where(p=>(int)PuzzleFiveByFiveGenerator.Target[p]==color).ToArray();
                    int cost=Brute(from,to,0,0);if(color==0)empty=cost;else colored+=cost;
                }
                Assert.That(after,Is.EqualTo(Math.Max(colored,empty)));
            }
        }
        static int Brute(int[] from,int[] to,int i,int used)
        {if(i==from.Length)return 0;int best=int.MaxValue;for(int j=0;j<to.Length;j++)if((used&(1<<j))==0)best=Math.Min(best,Math.Abs(from[i]/5-to[j]/5)+Math.Abs(from[i]%5-to[j]%5)+Brute(from,to,i+1,used|(1<<j)));return best;}
        [Test] public void EncodingPreservesTheLastCellsBeyond64Bits()
        {
            var a=(TileColor[])PuzzleFiveByFiveGenerator.Target.Clone();var b=(TileColor[])a.Clone();b[24]=TileColor.Purple;
            Assert.That(PuzzleFiveByFiveGenerator.Encode(a),Is.Not.EqualTo(PuzzleFiveByFiveGenerator.Encode(b)));
            Assert.That(PuzzleFiveByFiveGenerator.Encode(a).Length,Is.EqualTo(25));
        }
        [Test] public void MissingFiveSaveInitializesWithoutTouchingExistingProgressAndProfile()
        {
            string folder=System.IO.Path.Combine(Application.temporaryCachePath,"ColorMovePuzzleTests",Guid.NewGuid().ToString());
            try
            {
                foreach(string file in new[]{"progress-v1.json","progress-4x4-v1.json"}){var p=new PuzzleProgress(30);p.MarkCleared(1);new PuzzleProgressStore(System.IO.Path.Combine(folder,file),30).Save(p);}
                var paths=System.IO.Directory.GetFiles(folder);var bytes=paths.Select(System.IO.File.ReadAllText).ToArray();
                var store=new PuzzleProgressStore(System.IO.Path.Combine(folder,"progress-5x5-v1.json"),30);var five=store.Load();Assert.That(five.ClearedCount,Is.Zero);Assert.That(five.IsUnlocked(1),Is.True);Assert.That(five.IsUnlocked(2),Is.False);
                five.MarkCleared(1);five.Select(2);store.Save(five);Assert.That(store.Load().LastPlayed,Is.EqualTo(2));
                for(int i=0;i<paths.Length;i++)Assert.That(System.IO.File.ReadAllText(paths[i]),Is.EqualTo(bytes[i]));
            }
            finally{if(System.IO.Directory.Exists(folder)){foreach(string f in System.IO.Directory.GetFiles(folder))System.IO.File.Delete(f);System.IO.Directory.Delete(folder);}}
        }
    }
}
