using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ColorMovePuzzle.Tests
{
    public sealed class PuzzleSessionStoreTests
    {
        string folder;
        string path;
        [SetUp] public void Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "ColorMovePuzzleTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(folder); path = Path.Combine(folder, "session.json");
        }
        [TearDown] public void Cleanup()
        {
            foreach (string file in Directory.GetFiles(folder)) File.Delete(file);
            Directory.Delete(folder);
        }
        static PuzzleStageData Stage(int size, int number = 1) => AssetDatabase.LoadAssetAtPath<PuzzleStageData>(
            $"Assets/ColorMovePuzzle/Data/{(size == 3 ? "Campaign" : size == 4 ? "FourByFour" : "FiveByFive")}/Stage{number:00}.asset");
        [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void CommittedBoardAndMovesRoundTripForEveryMode(int size)
        {
            var stage = Stage(size); var board = stage.CreateStartingBoard();
            foreach (var move in stage.GetSolution().Take(2)) Assert.That(board.TryMove(move.from, move.to), Is.True);
            Assert.That(new PuzzleSessionStore(path).Save(stage, board, 2), Is.True);
            Assert.That(new PuzzleSessionStore(path).TryLoad(stage, out var saved), Is.True);
            CollectionAssert.AreEqual(board.CopyCells(), saved.Cells);
            Assert.That(saved.MoveCount, Is.EqualTo(2));
            Assert.That(saved.Cells.Count(c => c == TileColor.Empty), Is.EqualTo(size == 5 ? 3 : 2));
        }
        [Test] public void CorruptPrimaryRecoversBackupAndNextSaveProtectsThatBackup()
        {
            var stage = Stage(5); var board = stage.CreateStartingBoard(); var solution = stage.GetSolution();
            var store = new PuzzleSessionStore(path);
            board.TryMove(solution[0].from, solution[0].to); store.Save(stage, board, 1);
            board.TryMove(solution[1].from, solution[1].to); store.Save(stage, board, 2);
            string goodBackup = File.ReadAllText(path + ".bak");
            File.WriteAllText(path, "{broken");
            Assert.That(store.TryLoad(stage, out var saved), Is.True);
            Assert.That(store.RecoveredBackup, Is.True); Assert.That(saved.MoveCount, Is.EqualTo(1));
            Assert.That(store.Save(stage, new PuzzleBoard(5, saved.Cells), saved.MoveCount), Is.True);
            Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(goodBackup));
        }
        [Test] public void ClearMarkerPreventsBackupResurrection()
        {
            var stage = Stage(3); var store = new PuzzleSessionStore(path);
            store.Save(stage, stage.CreateStartingBoard(), 0); store.Clear(stage);
            Assert.That(store.TryLoad(stage, out _), Is.False);
            File.WriteAllText(path, "{bad");
            Assert.That(store.TryLoad(stage, out _), Is.False);
        }
        [Test] public void NewStageDoesNotReadPreviousStageBackupAndModesDoNotCrossLoad()
        {
            var store = new PuzzleSessionStore(path); var one = Stage(3); var two = Stage(3, 2);
            store.Save(one, one.CreateStartingBoard(), 0); store.Save(two, two.CreateStartingBoard(), 0);
            Assert.That(store.TryLoad(one, out _), Is.False);
            Assert.That(store.TryLoad(two, out _), Is.True);
            Assert.That(store.TryLoad(Stage(4), out _), Is.False);
        }
        [Test] public void MissingOrBothCorruptFilesAndInterruptedTemporaryFileAreIgnored()
        {
            var store = new PuzzleSessionStore(path); var stage = Stage(5);
            Assert.That(store.TryLoad(stage, out _), Is.False);
            File.WriteAllText(path + ".tmp", "partial");
            Assert.That(store.TryLoad(stage, out _), Is.False);
            File.WriteAllText(path, "null"); File.WriteAllText(path + ".bak", "{bad");
            Assert.That(store.TryLoad(stage, out _), Is.False);
            Assert.That(store.Save(stage, stage.CreateStartingBoard(), 0), Is.True);
            Assert.That(store.TryLoad(stage, out _), Is.True);
        }
        [Serializable] sealed class SavedData
        {
            public int version; public int gridSize; public int stageNumber; public string stageFingerprint;
            public bool active; public TileColor[] cells; public int moveCount; public string checksum;
        }
        [TestCase("version")] [TestCase("size")] [TestCase("inventory")] [TestCase("empty")]
        [TestCase("negative")] [TestCase("zero")] [TestCase("fingerprint")] [TestCase("color")] [TestCase("checksum")]
        public void InvalidDataIsRejectedEvenWithAnOtherwiseCorrectChecksum(string fault)
        {
            var stage = Stage(5); var board = stage.CreateStartingBoard(); var move = stage.GetSolution()[0];
            board.TryMove(move.from, move.to); var store = new PuzzleSessionStore(path); store.Save(stage, board, 1);
            var data = JsonUtility.FromJson<SavedData>(File.ReadAllText(path));
            switch (fault)
            {
                case "version": data.version = 99; break;
                case "size": data.gridSize = 4; break;
                case "inventory": data.cells[Array.FindIndex(data.cells, c => c == TileColor.Red)] = TileColor.Blue; break;
                case "empty": data.cells[Array.FindIndex(data.cells, c => c == TileColor.Empty)] = TileColor.Red; break;
                case "negative": data.moveCount = -1; break;
                case "zero": data.moveCount = 0; break;
                case "fingerprint": data.stageFingerprint = "changed-stage"; break;
                case "color": data.cells[0] = (TileColor)99; break;
                case "checksum": data.moveCount = 999; break;
            }
            if (fault != "checksum")
            {
                data.checksum = null;
                using (var sha = SHA256.Create())
                    data.checksum = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(data))));
            }
            File.WriteAllText(path, JsonUtility.ToJson(data));
            Assert.That(store.TryLoad(stage, out _), Is.False);
        }
        [Test] public void WriteFailurePreservesLastGoodDataAndLegacyStoresAreUntouched()
        {
            var stage = Stage(3); var store = new PuzzleSessionStore(path);
            store.Save(stage, stage.CreateStartingBoard(), 0); string before = File.ReadAllText(path);
            Directory.CreateDirectory(path + ".tmp"); // Prevent opening the temporary file.
            Assert.That(store.Save(stage, stage.CreateStartingBoard(), 0), Is.False);
            Assert.That(store.LastError, Is.Not.Empty); Assert.That(File.ReadAllText(path), Is.EqualTo(before));
            Directory.Delete(path + ".tmp");
            string legacy = Path.Combine(folder, "progress-v1.json");
            File.WriteAllText(legacy, "{\"version\":1,\"clearedStages\":[1,2],\"lastPlayed\":3}");
            Assert.That(new PuzzleProgressStore(legacy, 30).Load().ClearedCount, Is.EqualTo(2));
            Assert.That(File.ReadAllText(legacy), Does.Contain("[1,2]"));
        }
        [Test] public void SmallSnapshotsMeasureOneHundredDurableWrites()
        {
            var stage = Stage(5); var board = stage.CreateStartingBoard(); var move = stage.GetSolution()[0];
            var store = new PuzzleSessionStore(path); var times = new double[100];
            for (int i = 0; i < times.Length; i++)
            {
                Assert.That(board.TryMove(i % 2 == 0 ? move.from : move.to, i % 2 == 0 ? move.to : move.from), Is.True);
                var clock = System.Diagnostics.Stopwatch.StartNew();
                bool saved = store.Save(stage, board, i + 1);
                Assert.That(saved, Is.True, $"Write {i + 1}: {store.LastError}"); times[i] = clock.Elapsed.TotalMilliseconds;
            }
            Directory.CreateDirectory("Artifacts/Autosave");
            File.WriteAllText("Artifacts/Autosave/EditorWriteMetrics.json", JsonUtility.ToJson(new Metrics {
                writes = store.WriteCount, bytes = new FileInfo(path).Length,
                meanMilliseconds = times.Average(), maxMilliseconds = times.Max() }, true));
            Assert.That(new FileInfo(path).Length, Is.LessThan(2048));
        }
        [Serializable] sealed class Metrics { public int writes; public long bytes; public double meanMilliseconds; public double maxMilliseconds; }
    }
}
