using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace ColorMovePuzzle.Editor
{
    // Authoring only: never executes in a Player. No full-state-space search.
    public static class PuzzleFiveByFiveGenerator
    {
        public const string PrototypePath = "Assets/ColorMovePuzzle/Data/FiveByFive/Stage01.asset";
        public const string CatalogPath = "Assets/ColorMovePuzzle/Data/FiveByFive.asset";
        public static readonly TileColor[] Target = {
            TileColor.Red,TileColor.Red,TileColor.Red,TileColor.Red,TileColor.Blue,
            TileColor.Blue,TileColor.Blue,TileColor.Blue,TileColor.Yellow,TileColor.Yellow,
            TileColor.Yellow,TileColor.Yellow,TileColor.Green,TileColor.Green,TileColor.Green,
            TileColor.Green,TileColor.Purple,TileColor.Purple,TileColor.Purple,TileColor.Purple,
            TileColor.Black,TileColor.Black,TileColor.Empty,TileColor.Empty,TileColor.Empty};
        public const int MaxWalks = 512, MaxSteps = 180, MaxCandidates = 10000, SearchBudgetMs = 12000;
        sealed class Candidate
        {
            public string key;
            public TileColor[] cells;
            public PuzzleMove[] solution;
            public int lower;
        }
        // A string encodes every cell. A 64-bit, three-bits-per-cell key would truncate a 5x5 board.
        public static string Encode(TileColor[] cells) => new string(cells.Select(c => (char)('0' + (int)c)).ToArray());
        static List<PuzzleMove> FreshMoves(PuzzleBoard board, HashSet<string> visited)
        {
            var result = new List<PuzzleMove>(); var cells = board.CopyCells();
            for (int from = 0; from < board.Count; from++) foreach (int to in board.GetEmptyNeighbors(from))
            {
                Swap(cells, from, to); bool fresh = !visited.Contains(Encode(cells)); Swap(cells, from, to);
                if (fresh) result.Add(new PuzzleMove(from,to));
            }
            return result;
        }
        public static PuzzleStageData GeneratePrototype()
        {
            Directory.CreateDirectory("Assets/ColorMovePuzzle/Data/FiveByFive"); AssetDatabase.Refresh();
            var random = new System.Random(55005); var board = new PuzzleBoard(5, Target);
            var visited = new HashSet<string> {Encode(Target)}; var moves = new List<PuzzleMove>();
            for (int step = 0; step < 100; step++)
            {
                var options = FreshMoves(board, visited);
                if (options.Count == 0) throw new InvalidOperationException("Prototype walk trapped.");
                var move = options[random.Next(options.Count)]; board.TryMove(move.from,move.to); moves.Add(move);
                visited.Add(Encode(board.CopyCells()));
                int lower = PuzzleMatchingHeuristic.Evaluate(5,board.CopyCells(),Target);
                if (lower < 12 || moves.Count < 18) continue;
                return Save(1,board.CopyCells(),lower,PuzzleFourByFourGenerator.Reverse(moves.ToArray()));
            }
            throw new InvalidOperationException("No prototype met the introductory lower bound. Nothing accepted.");
        }
        static PuzzleStageData Save(int number, TileColor[] cells, int lower, PuzzleMove[] solution)
        {
            string path = $"Assets/ColorMovePuzzle/Data/FiveByFive/Stage{number:00}.asset";
            var stage = AssetDatabase.LoadAssetAtPath<PuzzleStageData>(path);
            if (!stage) {stage = ScriptableObject.CreateInstance<PuzzleStageData>(); AssetDatabase.CreateAsset(stage,path);}
            stage.ConfigureEstimated(number,5,cells,Target,lower,lower,solution);
            EditorUtility.SetDirty(stage); AssetDatabase.SaveAssets(); return stage;
        }
        public static string Band(int stage) => new[] {"Introduction","Easy","Medium","Hard","Very Hard","Expert"}[(stage-1)/5];

        [MenuItem("ColorMovePuzzle/Generate 30 Estimated 5x5 Stages")]
        public static PuzzleStageCatalog GenerateCampaign()
        {
            var prototype = AssetDatabase.LoadAssetAtPath<PuzzleStageData>(PrototypePath);
            if (!prototype || prototype.LowerBound < 8) throw new InvalidOperationException("Verify a qualifying single-stage prototype first.");
            var watch = Stopwatch.StartNew(); var random = new System.Random(55055);
            var pool = new Dictionary<string,Candidate>();
            for (int walk = 0; walk < MaxWalks && pool.Count < MaxCandidates; walk++)
            {
                var board = new PuzzleBoard(5,Target); var visited = new HashSet<string> {Encode(Target)};
                var moves = new List<PuzzleMove>();
                for (int step = 0; step < MaxSteps && pool.Count < MaxCandidates; step++)
                {
                    if (watch.ElapsedMilliseconds > SearchBudgetMs) throw new InvalidOperationException("Generation search budget exceeded; no campaign accepted.");
                    var options = FreshMoves(board,visited); if (options.Count == 0) break;
                    var move = options[random.Next(options.Count)]; board.TryMove(move.from,move.to); moves.Add(move);
                    var cells = board.CopyCells(); string key = Encode(cells); visited.Add(key);
                    if (moves.Count < 18 || moves.Count % 3 != 0 || pool.ContainsKey(key)) continue;
                    int lower = PuzzleMatchingHeuristic.Evaluate(5,cells,Target);
                    if (lower >= prototype.LowerBound) pool.Add(key,new Candidate {key=key,cells=cells,lower=lower,solution=PuzzleFourByFourGenerator.Reverse(moves.ToArray())});
                }
            }
            int maximum = pool.Values.GroupBy(c=>c.lower).Where(g=>g.Count()>=12).Max(g=>g.Key);
            if (maximum < 40) throw new InvalidOperationException($"Only lower bound {maximum} available; expert progression requires at least 40.");
            maximum = Math.Min(54,maximum);
            var selected = new List<Candidate> {new Candidate {key=Encode(prototype.CreateStartingBoard().CopyCells()),cells=prototype.CreateStartingBoard().CopyCells(),lower=prototype.LowerBound,solution=prototype.GetSolution()}};
            var used = new HashSet<string> {selected[0].key}; var stages = new PuzzleStageData[30]; stages[0]=prototype;
            // Strictly ascending target bounds, then prefer short witnesses and spatial diversity.
            for (int i=1;i<30;i++)
            {
                int desired = prototype.LowerBound + i*(maximum-prototype.LowerBound)/29;
                Candidate best = null; int bestScore=int.MinValue;
                foreach (var candidate in pool.Values)
                {
                    if (used.Contains(candidate.key) || candidate.lower < desired || candidate.lower < selected[i-1].lower || candidate.lower > maximum) continue;
                    int difference = selected.Min(p=>p.cells.Where((c,n)=>c!=candidate.cells[n]).Count());
                    int score = -(candidate.lower-desired)*100000 + difference*24 - candidate.solution.Length;
                    if(score>bestScore) {bestScore=score; best=candidate;}
                }
                if(best==null) throw new InvalidOperationException($"No unique stage for bound {desired}.");
                selected.Add(best); used.Add(best.key);
            }
            // Evaluate ALL selected layouts with a bounded beam probe. Its residual is descriptive,
            // not a distance bound or a proof of subjective difficulty. No Player uses this probe.
            var csv = new System.Text.StringBuilder("stage,start,target,generation_moves,matching_lower_bound,solution_upper_bound,estimated_band,probe_nodes,probe_best_residual,exact_minimum_claimed\n");
            for(int i=0;i<30;i++)
            {
                var c=selected[i]; var probe=Probe(c.cells);
                stages[i]=Save(i+1,c.cells,c.lower,c.solution);
                stages[i].ConfigureGenerationMetrics(c.solution.Length,Band(i+1),probe.nodes,probe.residual);
                EditorUtility.SetDirty(stages[i]);
                csv.AppendLine($"{i+1},{Pattern(c.cells)},{Pattern(Target)},{c.solution.Length},{c.lower},{c.solution.Length},{Band(i+1)},{probe.nodes},{probe.residual},false");
            }
            var catalog=AssetDatabase.LoadAssetAtPath<PuzzleStageCatalog>(CatalogPath);
            if(!catalog) {catalog=ScriptableObject.CreateInstance<PuzzleStageCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
            catalog.Configure(stages);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Artifacts/FiveByFive");File.WriteAllText("Artifacts/FiveByFive/Stages.csv",csv.ToString());
            File.WriteAllText("Artifacts/FiveByFive/Generation.json",JsonUtility.ToJson(new Report {stages=30,candidates=pool.Count,minimum=stages[0].LowerBound,maximum=stages[29].LowerBound,milliseconds=watch.ElapsedMilliseconds},true));
            return catalog;
        }
        // Depth 6, width 24, <=1024 examined successors, <=128 retained states per layer.
        public static (int nodes,int residual) Probe(TileColor[] initial)
        {
            var frontier=new List<TileColor[]> {initial};var visited=new HashSet<string>{Encode(initial)};
            int nodes=0,best=PuzzleMatchingHeuristic.Evaluate(5,initial,Target);
            var clock=Stopwatch.StartNew();
            for(int depth=0;depth<6;depth++)
            {
                var next=new List<(TileColor[] cells,int h)>();
                foreach(var cells in frontier)
                {
                    var board=new PuzzleBoard(5,cells);
                    foreach(var move in FreshMoves(board,visited))
                    {
                        if(nodes>=1024 || clock.ElapsedMilliseconds>100) return(nodes,best);
                        var child=new PuzzleBoard(5,cells);child.TryMove(move.from,move.to);var state=child.CopyCells();
                        if(!visited.Add(Encode(state))) continue;
                        int h=PuzzleMatchingHeuristic.Evaluate(5,state,Target);nodes++;best=Math.Min(best,h);next.Add((state,h));
                    }
                }
                frontier=next.OrderBy(p=>p.h).Take(24).Select(p=>p.cells).ToList();
                if(frontier.Count==0 || best==0) break;
            }
            return(nodes,best);
        }
        static string Pattern(TileColor[] cells)=>string.Concat(cells.Select(c=>c==TileColor.Empty?"E":PuzzlePalette.Symbol(c)));
        static void Swap(TileColor[] cells,int a,int b){var c=cells[a];cells[a]=cells[b];cells[b]=c;}
        [Serializable] sealed class Report
        {
            public int stages,candidates,minimum,maximum;
            public long milliseconds;
            public int maxWalks=MaxWalks,maxSteps=MaxSteps,maxCandidates=MaxCandidates,searchBudgetMs=SearchBudgetMs,probeNodeCap=1024,probeWidth=24,probeDepth=6,probeBudgetMs=100;
            public string method="legal self-avoiding walks; interchangeable-color minimum matching; diversity selection; bounded beam probe (descriptive only)";
        }
    }
}
