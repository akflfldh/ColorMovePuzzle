using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColorMovePuzzle;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace ColorMovePuzzle.Editor
{
    public static class PuzzleSceneBuilder
    {
        public const string ScenePath = "Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity";
        public const string StagePath = "Assets/ColorMovePuzzle/Data/Stage01.asset";
        const string ArtPath = "Assets/ColorMovePuzzle/UI/RoundedSquare.png";
        static Sprite rounded;
        static TMP_FontAsset font;
        static readonly Color Ink = PuzzlePalette.Hex("F2F4F8");
        static readonly Color Muted = PuzzlePalette.Hex("A1AFC4");

        internal static void LoadUIAssets()
        {
            rounded = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (!rounded || !font) throw new InvalidOperationException("Original puzzle UI assets are missing.");
        }

        [MenuItem("ColorMovePuzzle/Create Stage 01 Scene")]
        public static void Build()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before authoring.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Game scene already exists; open it instead of overwriting it.");
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save existing scene changes before creating the game scene.");
            Directory.CreateDirectory("Assets/ColorMovePuzzle/Scenes");
            Directory.CreateDirectory("Assets/ColorMovePuzzle/Data");
            Directory.CreateDirectory("Assets/ColorMovePuzzle/UI");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            rounded = CreateRoundedSprite();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (!font) throw new InvalidOperationException("Import TMP Essentials first.");
            var stage = CreateStage();

            var cameraObject = new GameObject("UICamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = PuzzlePalette.Hex("101A2C");
            camera.cullingMask = 0;
            cameraObject.transform.position = new Vector3(0, 0, -10);

            var canvasObject = new GameObject("PuzzleCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var canvasRect = (RectTransform)canvasObject.transform;
            var background = Image("Background", canvasRect, PuzzlePalette.Hex("101A2C"), false, false);
            Stretch(background.rectTransform);
            var safe = Node("SafeArea", canvasRect);
            Stretch(safe);
            var content = Node("Content", safe);
            Place(content, 0, 0, 1080, 1920);
            canvasObject.AddComponent<PuzzleScreenLayout>().Configure(safe, content);

            var header = Node("Header", content);
            Stretch(header);
            var stageLabel = Label("StageLabel", header, "STAGE 01", -344, 92, 280, 48, 30, Muted);
            var moveCaption = Label("MoveCaption", header, "MOVES", 317, 77, 230, 32, 24, Muted);
            var moveLabel = Label("MoveCount", header, "0", 317, 124, 230, 66, 56, Ink);
            Label("Title", header, "COLOR SLIDE", 0, 216, 880, 100, 82, Ink, FontStyles.Bold);
            Label("Subtitle", header, "A LITTLE SHIFT. A PERFECT MATCH.", 0, 294, 920, 44, 25, Muted);

            var targetArea = Node("TargetArea", content);
            Stretch(targetArea);
            Label("TargetCaption", targetArea, "THE TARGET", 0, 365, 600, 44, 27, Muted, FontStyles.Bold);
            var preview = CreateBoard("TargetGrid", targetArea, 0, 548, 300, 12, 6,
                new PuzzleBoard(stage.BoardSize, stage.Target), false);
            Label("TargetHint", targetArea, "Match the colors. Keep the gaps.", 0, 742, 900, 42, 29, Muted);

            var boardArea = Node("PuzzleArea", content);
            Stretch(boardArea);
            Label("BoardCaption", boardArea, "YOUR PATTERN", 0, 811, 800, 42, 25, Muted, FontStyles.Bold);
            var board = CreateBoard("CurrentBoard", boardArea, 0, 1264, 840, 24, 16,
                stage.CreateStartingBoard(), true);

            var footer = Node("Footer", content);
            Stretch(footer);
            var status = Label("Instructions", footer, "Tap to slide. Swipe to choose a direction.", 0, 1735, 1000, 52, 28, Muted);
            var restart = Button("RestartButton", footer, "RESTART", 0, 1830, 440, 100);

            var overlay = Image("ClearOverlay", canvasRect, new Color(0.025f, 0.045f, 0.08f, 0.94f), true, false);
            Stretch(overlay.rectTransform);
            // Fit modal with the same safe-area content scale as the main UI.
            var modal = Node("ClearContent", overlay.rectTransform);
            Place(modal, 0, 0, 1080, 1920);
            var modalSafe = Node("ClearSafeArea", overlay.rectTransform);
            Stretch(modalSafe);
            modal.SetParent(modalSafe, false);
            overlay.gameObject.AddComponent<PuzzleScreenLayout>().Configure(modalSafe, modal);
            Label("ClearEyebrow", modal, "STAGE 01 COMPLETE", 0, 740, 950, 54, 30, PuzzlePalette.Hex("F6CE64"), FontStyles.Bold);
            Label("ClearTitle", modal, "Perfect match!", 0, 858, 1000, 132, 86, Ink, FontStyles.Bold);
            var clearMoves = Label("ClearMoveCount", modal, "You matched the pattern in 0 moves.", 0, 982, 980, 78, 33, Muted);
            var clearRestart = Button("PlayAgainButton", modal, "PLAY AGAIN", 0, 1140, 560, 116);
            overlay.gameObject.SetActive(false);

            var controller = new GameObject("PuzzleGame", typeof(PuzzleGameController)).GetComponent<PuzzleGameController>();
            controller.Configure(stage, board, preview, stageLabel, moveLabel, status,
                overlay.gameObject, clearMoves, restart, clearRestart);

            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            events.GetComponent<EventSystem>().pixelDragThreshold = 10;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Scene save failed.");
            var previous = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            previous.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = previous.ToArray();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = canvasObject;
            SetGameViewSize(1080, 1920);
            Debug.Log("ColorMovePuzzle: Stage 01 scene authored and saved.");
        }

        static PuzzleStageData CreateStage()
        {
            var stage = AssetDatabase.LoadAssetAtPath<PuzzleStageData>(StagePath);
            if (stage) return stage;
            stage = ScriptableObject.CreateInstance<PuzzleStageData>();
            int[] pairs = {4,7, 1,4, 5,8, 2,1, 4,5, 5,2, 7,4, 4,5, 1,4, 2,1,
                6,7, 3,6, 0,3, 1,0, 4,1, 5,2, 1,4, 2,5, 0,1, 1,2, 3,0, 4,1, 6,3, 5,4};
            var moves = new PuzzleMove[pairs.Length / 2];
            for (int i = 0; i < moves.Length; i++) moves[i] = new PuzzleMove(pairs[i * 2], pairs[i * 2 + 1]);
            stage.Configure(1, 3, new[] {TileColor.Red, TileColor.Red, TileColor.Blue,
                TileColor.Blue, TileColor.Yellow, TileColor.Yellow, TileColor.Black, TileColor.Empty, TileColor.Empty}, moves);
            AssetDatabase.CreateAsset(stage, StagePath);
            return stage;
        }

        internal static PuzzleBoardView CreateBoard(string name, Transform parent, float x, float y, float size,
            float padding, float gap, PuzzleBoard data, bool interactive)
        {
            var background = Image(name, parent, PuzzlePalette.Hex("0A1322"), false);
            Top(background.rectTransform, x, y, size, size);
            var cellLayer = Node("Cells", background.transform);
            Stretch(cellLayer);
            var tileLayer = Node("Tiles", background.transform);
            Stretch(tileLayer);
            var view = background.gameObject.AddComponent<PuzzleBoardView>();
            view.Configure(data.Size, padding, gap, tileLayer, Array.Empty<PuzzleTileView>());
            float cellSize = view.CellSize;
            for (int i = 0; i < data.Count; i++)
            {
                var cell = Image($"Cell{i:00}", cellLayer, PuzzlePalette.Hex("19253A"), false);
                Place(cell.rectTransform, view.CellPosition(i).x, view.CellPosition(i).y, cellSize, cellSize);
            }
            var tiles = new List<PuzzleTileView>();
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i] == TileColor.Empty) continue;
                var image = Image($"Tile{tiles.Count:00}", tileLayer, PuzzlePalette.Tile(data[i]), interactive);
                Place(image.rectTransform, view.CellPosition(i).x, view.CellPosition(i).y, cellSize, cellSize);
                // A subtle rim makes the black tile visible against the empty slots too.
                var outline = image.gameObject.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = new Color(1, 1, 1, 0.13f);
                outline.effectDistance = Vector2.one * (interactive ? 2 : 1);
                var letter = Label("ColorLabel", image.transform, PuzzlePalette.Symbol(data[i]),
                    0, 0, cellSize, cellSize, interactive ? 52 : 25, Ink, FontStyles.Bold);
                Place(letter.rectTransform, 0, 0, cellSize, cellSize);
                var tile = image.gameObject.AddComponent<PuzzleTileView>();
                tile.Configure(image, letter);
                tile.Bind(i, data[i], null, null);
                image.raycastTarget = interactive;
                tiles.Add(tile);
            }
            view.Configure(data.Size, padding, gap, tileLayer, tiles.ToArray());
            return view;
        }

        internal static RectTransform Node(string name, Transform parent)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            return (RectTransform)node.transform;
        }

        internal static UnityEngine.UI.Image Image(string name, Transform parent, Color color, bool raycast, bool round = true)
        {
            var rect = Node(name, parent);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = raycast;
            if (round) { image.sprite = rounded; image.type = UnityEngine.UI.Image.Type.Sliced; }
            return image;
        }

        internal static TMP_Text Label(string name, Transform parent, string text, float x, float top,
            float width, float height, int size, Color color, FontStyles style = FontStyles.Normal)
        {
            var rect = Node(name, parent);
            Top(rect, x, top, width, height);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.fontStyle = style;
            label.text = text;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        internal static UnityEngine.UI.Button Button(string name, Transform parent, string text, float x, float y, float width, float height)
        {
            var image = Image(name, parent, PuzzlePalette.Hex("DFE7F4"), true);
            Top(image.rectTransform, x, y, width, height);
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = Color.white;
            colors.pressedColor = PuzzlePalette.Hex("A5B9D8");
            button.colors = colors;
            var navigation = button.navigation;
            navigation.mode = UnityEngine.UI.Navigation.Mode.None;
            button.navigation = navigation;
            var label = Label("Label", image.transform, text, 0, 0, width, height, 30, PuzzlePalette.Hex("142137"), FontStyles.Bold);
            Place(label.rectTransform, 0, 0, width, height);
            return button;
        }

        internal static void Top(RectTransform rect, float x, float y, float width, float height)
            => Place(rect, x, 960 - y, width, height);

        internal static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static Sprite CreateRoundedSprite()
        {
            if (!File.Exists(ArtPath))
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        float dx = Mathf.Max(Mathf.Abs(x - 31.5f) - 19.5f, 0);
                        float dy = Mathf.Max(Mathf.Abs(y - 31.5f) - 19.5f, 0);
                        float alpha = Mathf.Clamp01(12f - Mathf.Sqrt(dx * dx + dy * dy));
                        texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                texture.Apply();
                File.WriteAllBytes(ArtPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(ArtPath);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(16, 16, 16, 16);
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
        }

        // Editor-only reflection: Unity exposes no public API for choosing a Game View preset.
        public static void SetGameViewSize(int width, int height)
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] {Enum.Parse(groupType, EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ? "Android" : "Standalone")});
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var kindType = assembly.GetType("UnityEditor.GameViewSizeType");
            var binding = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            int builtins = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
            int customs = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
            int index = -1;
            for (int i = 0; i < builtins + customs; i++)
            {
                var existing = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] {i});
                if ((int)sizeType.GetProperty("width", binding).GetValue(existing) == width
                    && (int)sizeType.GetProperty("height", binding).GetValue(existing) == height)
                { index = i; break; }
            }
            if (index < 0)
            {
                var size = Activator.CreateInstance(sizeType, binding, null,
                    new[] {Enum.ToObject(kindType, 1), (object)width, height, $"Puzzle {width}x{height}"}, null);
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] {size});
                index = builtins + customs;
            }
            var gameViewType = assembly.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameViewType);
            gameViewType.GetProperty("selectedSizeIndex", binding).SetValue(window, index);
            window.Repaint();
        }
    }
}
