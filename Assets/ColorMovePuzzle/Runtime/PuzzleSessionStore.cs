using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ColorMovePuzzle
{
    public sealed class PuzzleSessionState
    {
        public TileColor[] Cells { get; }
        public int MoveCount { get; }
        public PuzzleSessionState(TileColor[] cells, int moves) { Cells = cells; MoveCount = moves; }
    }

    // One most recent puzzle per mode. Never writes the existing unlock/profile files.
    public sealed class PuzzleSessionStore
    {
        [Serializable] sealed class Data
        {
            public int version;
            public int gridSize;
            public int stageNumber;
            public string stageFingerprint;
            public bool active;
            public TileColor[] cells;
            public int moveCount;
            public string checksum;
        }
        readonly System.Collections.Generic.Dictionary<PuzzleStageData, string> fingerprints =
            new System.Collections.Generic.Dictionary<PuzzleStageData, string>();
        bool preserveBackup;
        public string FilePath { get; }
        public string LastError { get; private set; }
        public bool RecoveredBackup { get; private set; }
        public int WriteCount { get; private set; }
        public PuzzleSessionStore(string path) { FilePath = path; }

        string Fingerprint(PuzzleStageData stage)
        {
            if (fingerprints.TryGetValue(stage, out string value)) return value;
            var initial = stage.CreateStartingBoard().CopyCells();
            var goal = stage.Target;
            var text = new StringBuilder().Append(stage.BoardSize).Append(':').Append(stage.StageNumber);
            foreach (var color in initial) text.Append(',').Append((int)color);
            text.Append('|');
            foreach (var color in goal) text.Append(',').Append((int)color);
            value = Hash(text.ToString());
            fingerprints.Add(stage, value);
            return value;
        }
        static string Hash(string text)
        {
            using (var sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
        static string Checksum(Data data)
        {
            string saved = data.checksum;
            data.checksum = null;
            string hash = Hash(JsonUtility.ToJson(data));
            data.checksum = saved;
            return hash;
        }
        static void ValidateBoard(PuzzleStageData stage, TileColor[] cells, int moves)
        {
            var board = new PuzzleBoard(stage.BoardSize, cells);
            if (moves < 0) throw new InvalidDataException("Negative move count.");
            var inventory = new int[(int)TileColor.Purple + 1];
            for (int i = 0; i < board.Count; i++) inventory[(int)board[i]]++;
            foreach (var color in stage.Target) inventory[(int)color]--;
            foreach (int count in inventory)
                if (count != 0) throw new InvalidDataException("Session tile inventory differs from stage.");
            if (moves == 0 && !board.Matches(stage.CreateStartingBoard().CopyCells()))
                throw new InvalidDataException("Zero-move session differs from stage start.");
        }
        public bool TryLoad(PuzzleStageData stage, out PuzzleSessionState state)
        {
            state = null;
            LastError = null;
            RecoveredBackup = false;
            preserveBackup = false;
            foreach (string candidate in new[] { FilePath, FilePath + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    if (new FileInfo(candidate).Length > 16384) throw new InvalidDataException("Oversized session file.");
                    var data = JsonUtility.FromJson<Data>(File.ReadAllText(candidate));
                    if (data == null || data.version != 1 || string.IsNullOrEmpty(data.stageFingerprint)
                        || string.IsNullOrEmpty(data.checksum) || data.checksum != Checksum(data)
                        || data.gridSize != stage.BoardSize || data.stageNumber < 1)
                        throw new InvalidDataException("Unsupported or corrupt puzzle session.");
                    // A valid record of another stage or a clear marker must not resurrect an older backup.
                    if (data.stageNumber != stage.StageNumber) return false;
                    if (data.stageFingerprint != Fingerprint(stage)) throw new InvalidDataException("Stage data changed.");
                    if (!data.active)
                    {
                        if ((data.cells != null && data.cells.Length != 0) || data.moveCount != 0)
                            throw new InvalidDataException("Invalid clear marker.");
                        return false;
                    }
                    ValidateBoard(stage, data.cells, data.moveCount);
                    state = new PuzzleSessionState(data.cells, data.moveCount);
                    RecoveredBackup = candidate != FilePath;
                    preserveBackup = RecoveredBackup;
                    return true;
                }
                catch (Exception error) when (IsStorageError(error)) { LastError = error.Message; }
            }
            return false;
        }
        public bool Save(PuzzleStageData stage, PuzzleBoard board, int moves)
        {
            LastError = null;
            try
            {
                if (board == null || board.Size != stage.BoardSize) throw new InvalidDataException("Wrong board size.");
                var cells = board.CopyCells();
                ValidateBoard(stage, cells, moves);
                return Write(new Data { version = 1, gridSize = stage.BoardSize, stageNumber = stage.StageNumber,
                    stageFingerprint = Fingerprint(stage), active = true, cells = cells, moveCount = moves });
            }
            catch (Exception error) when (IsStorageError(error)) { LastError = error.Message; return false; }
        }
        public bool Clear(PuzzleStageData stage)
        {
            LastError = null;
            try
            {
                if (!Write(new Data { version = 1, gridSize = stage.BoardSize, stageNumber = stage.StageNumber,
                    stageFingerprint = Fingerprint(stage), active = false })) return false;
                // Keep a clear marker in the backup too, so corrupting the primary cannot revive a cleared puzzle.
                File.Copy(FilePath, FilePath + ".bak", true);
                return true;
            }
            catch (Exception error) when (IsStorageError(error)) { LastError = error.Message; return false; }
        }
        bool Write(Data data)
        {
            data.checksum = Checksum(data);
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath)));
            string temporary = FilePath + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (!File.Exists(FilePath)) File.Move(temporary, FilePath);
            else
            {
                try { File.Replace(temporary, FilePath, preserveBackup ? null : FilePath + ".bak"); }
                catch (PlatformNotSupportedException)
                {
                    if (!preserveBackup) File.Copy(FilePath, FilePath + ".bak", true);
                    File.Copy(temporary, FilePath, true);
                    File.Delete(temporary);
                }
            }
            preserveBackup = false;
            WriteCount++;
            return true;
        }
        static bool IsStorageError(Exception error) => error is IOException || error is InvalidDataException || error is UnauthorizedAccessException
            || error is ArgumentException || error is NotSupportedException;
    }
}
