using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ColorMovePuzzle.Editor.PuzzleSceneBuilder;

namespace ColorMovePuzzle.Editor
{
    public static class PuzzleCampaignSceneBuilder
    {
        [MenuItem("ColorMovePuzzle/Upgrade Scene With Stage Selection")]
        public static void Upgrade()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before authoring.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || scene.isDirty) throw new InvalidOperationException("Open the saved puzzle scene first.");
            if (UnityEngine.Object.FindAnyObjectByType<PuzzleCampaignController>()) throw new InvalidOperationException("Campaign UI already exists; edit it in place.");
            var game = UnityEngine.Object.FindAnyObjectByType<PuzzleGameController>();
            var canvas = GameObject.Find("PuzzleCanvas").GetComponent<RectTransform>();
            var gameContent = canvas.Find("SafeArea").gameObject;
            var header = gameContent.transform.Find("Content/Header");
            var clearContent = canvas.Find("ClearOverlay/ClearSafeArea/ClearContent");
            if (!game || !header || !clearContent) throw new InvalidOperationException("Original puzzle hierarchy is incomplete.");
            LoadUIAssets();
            var catalog = AssetDatabase.LoadAssetAtPath<PuzzleStageCatalog>(PuzzleCampaignGenerator.CatalogPath);
            if (!catalog || catalog.Count != 30) throw new InvalidOperationException("Generate the campaign first.");

            var back = Button("StageSelectButton", header, "STAGES", -350, 100, 230, 74);
            back.GetComponentInChildren<TMP_Text>().fontSize = 24;
            Top(header.Find("StageLabel").GetComponent<RectTransform>(), 0, 92, 270, 48);
            var next = Button("NextStageButton", clearContent, "NEXT STAGE", 0, 1140, 560, 110);
            var again = clearContent.Find("PlayAgainButton").GetComponent<RectTransform>();
            Top(again, 0, 1280, 560, 96);
            Place(again.GetComponentInChildren<TMP_Text>().rectTransform, 0, 0, 560, 96);
            var clearBack = Button("ClearStageSelectButton", clearContent, "STAGE SELECT", 0, 1410, 560, 90);
            clearBack.GetComponentInChildren<TMP_Text>().fontSize = 26;

            var panel = Image("StageSelection", canvas, PuzzlePalette.Hex("101A2C"), true, false);
            Stretch(panel.rectTransform);
            var safe = Node("SelectionSafeArea", panel.transform);
            Stretch(safe);
            var design = Node("SelectionContent", safe);
            Place(design, 0, 0, 1080, 1920);
            panel.gameObject.AddComponent<PuzzleScreenLayout>().Configure(safe, design);
            Label("SelectionEyebrow", design, "COLOR SLIDE / 30 STAGES", 0, 98, 980, 48, 27, PuzzlePalette.Hex("A1AFC4"));
            Label("SelectionTitle", design, "Choose your pattern.", 0, 224, 1020, 120, 76, Color.white, FontStyles.Bold);
            var summary = Label("ProgressSummary", design, "00 / 30 COMPLETE", -220, 352, 520, 44, 28, PuzzlePalette.Hex("A1AFC4"));
            var nextLabel = Label("NextStageLabel", design, "UP NEXT: STAGE 01", 266, 352, 490, 44, 27, PuzzlePalette.Hex("F6CE64"));
            var saveStatus = Label("SaveStatus", design, "PROGRESS IS SAVED ON THIS DEVICE", 0, 1840, 1030, 48, 23, PuzzlePalette.Hex("A1AFC4"));

            var scrollRoot = Node("StageScrollView", design);
            Top(scrollRoot, 0, 1100, 960, 1340);
            var scroll = scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            var viewport = Image("Viewport", scrollRoot, Color.white, true, false);
            Stretch(viewport.rectTransform);
            viewport.rectTransform.offsetMax = new Vector2(-28, 0);
            var mask = viewport.gameObject.AddComponent<UnityEngine.UI.Mask>();
            mask.showMaskGraphic = false;
            var content = Node("StageRows", viewport.transform);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var vertical = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            vertical.padding = new RectOffset(12, 12, 12, 12);
            vertical.spacing = 20;
            vertical.childControlWidth = vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;

            var track = Image("Scrollbar", scrollRoot, PuzzlePalette.Hex("1E2D42"), true);
            track.rectTransform.anchorMin = new Vector2(1, 0);
            track.rectTransform.anchorMax = Vector2.one;
            track.rectTransform.pivot = new Vector2(1, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(8, 0);
            track.rectTransform.anchoredPosition = Vector2.zero;
            var handle = Image("Handle", track.transform, PuzzlePalette.Hex("647892"), true);
            Stretch(handle.rectTransform);
            var scrollbar = track.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.Permanent;

            Sprite lockSprite = CreateIcon(false), checkSprite = CreateIcon(true);
            var cards = new PuzzleStageButtonView[30];
            for (int row = 0; row < 10; row++)
            {
                var rowRect = Node($"Row{row + 1:00}", content);
                var rowSize = rowRect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                rowSize.minHeight = rowSize.preferredHeight = 236;
                var horizontal = rowRect.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                horizontal.spacing = 20;
                horizontal.childControlWidth = horizontal.childControlHeight = true;
                horizontal.childForceExpandWidth = horizontal.childForceExpandHeight = true;
                for (int column = 0; column < 3; column++)
                {
                    int number = row * 3 + column + 1;
                    var image = Image($"Stage{number:00}Button", rowRect, PuzzlePalette.Hex("172338"), true);
                    var layout = image.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                    layout.flexibleWidth = 1;
                    var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
                    button.targetGraphic = image;
                    var colors = button.colors;
                    colors.disabledColor = Color.white;
                    colors.pressedColor = PuzzlePalette.Hex("AFBDD3");
                    button.colors = colors;
                    var navigation = button.navigation;
                    navigation.mode = UnityEngine.UI.Navigation.Mode.None;
                    button.navigation = navigation;
                    var numberText = CardLabel("StageNumber", image.transform, number.ToString("00"), -65, 40, 130, 76, 58);
                    var state = CardLabel("StageState", image.transform, "LOCKED", 0, -29, 270, 38, 23);
                    var distance = CardLabel("MinimumMoves", image.transform, "", 0, -80, 270, 34, 22);
                    var padlock = Image("LockIcon", image.transform, PuzzlePalette.Hex("8291A8"), false, false);
                    padlock.sprite = lockSprite;
                    Place(padlock.rectTransform, 86, 42, 42, 42);
                    var check = Image("CompletedIcon", image.transform, PuzzlePalette.Hex("83D9B9"), false, false);
                    check.sprite = checkSprite;
                    Place(check.rectTransform, 86, 42, 42, 42);
                    var card = image.gameObject.AddComponent<PuzzleStageButtonView>();
                    card.Configure(button, image, numberText, state, distance, padlock.gameObject, check.gameObject);
                    cards[number - 1] = card;
                }
            }
            var selection = panel.gameObject.AddComponent<PuzzleStageSelectionView>();
            selection.Configure(cards, scroll, summary, nextLabel, saveStatus);
            selection.Initialize(catalog, null);
            selection.Refresh(new PuzzleProgress(30), null);
            var campaign = game.gameObject.AddComponent<PuzzleCampaignController>();
            campaign.Configure(catalog, game, selection, gameContent, back, next, clearBack,
                clearContent.Find("ClearEyebrow").GetComponent<TMP_Text>(),
                clearContent.Find("ClearTitle").GetComponent<TMP_Text>(),
                gameContent.transform.Find("Content/TargetArea/TargetHint").GetComponent<TMP_Text>());
            gameContent.SetActive(false);
            Canvas.ForceUpdateCanvases();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save upgraded scene.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = panel.gameObject;
            Debug.Log("Campaign selection and navigation authored with 30 wired stage buttons.");
        }

        static TMP_Text CardLabel(string name, Transform parent, string text, float x, float y, float width, float height, int size)
        {
            var label = Label(name, parent, text, 0, 0, width, height, size, PuzzlePalette.Hex("A1AFC4"), FontStyles.Bold);
            Place(label.rectTransform, x, y, width, height);
            return label;
        }

        static Sprite CreateIcon(bool check)
        {
            string path = $"Assets/ColorMovePuzzle/UI/{(check ? "Check" : "Lock")}.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    var p = new Vector2(x, y);
                    bool solid;
                    if (check) solid = Segment(p, new Vector2(10, 31), new Vector2(26, 15)) < 4.5f
                        || Segment(p, new Vector2(26, 15), new Vector2(55, 49)) < 4.5f;
                    else
                    {
                        float ring = Vector2.Distance(p, new Vector2(32, 37));
                        solid = (x >= 13 && x <= 51 && y >= 8 && y <= 35)
                            || (y >= 32 && ring >= 11 && ring <= 17);
                        if (Vector2.Distance(p, new Vector2(32, 25)) < 4 || (x >= 30 && x <= 34 && y >= 16 && y <= 24)) solid = false;
                    }
                    texture.SetPixel(x, y, new Color(1, 1, 1, solid ? 1 : 0));
                }
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static float Segment(Vector2 p, Vector2 a, Vector2 b)
        { var v = b - a; return Vector2.Distance(p, a + v * Mathf.Clamp01(Vector2.Dot(p - a, v) / v.sqrMagnitude)); }
    }
}
