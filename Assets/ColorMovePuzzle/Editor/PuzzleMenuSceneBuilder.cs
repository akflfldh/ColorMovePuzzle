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
    public static class PuzzleMenuSceneBuilder
    {
        public const string ModesPath = "Assets/ColorMovePuzzle/Data/Modes.asset";
        [MenuItem("ColorMovePuzzle/Add Main Menu And Settings")]
        public static void Upgrade()
        {
            var scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open the saved puzzle scene outside Play mode.");
            if (UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>()) throw new InvalidOperationException("Menu already exists; edit it in place.");
            var campaign = UnityEngine.Object.FindAnyObjectByType<PuzzleCampaignController>();
            var canvas = GameObject.Find("PuzzleCanvas").GetComponent<RectTransform>();
            var selectionDesign = canvas.Find("StageSelection/SelectionSafeArea/SelectionContent");
            if (!campaign || !selectionDesign) throw new InvalidOperationException("The existing campaign UI is missing.");
            LoadUIAssets();
            var modes = AssetDatabase.LoadAssetAtPath<PuzzleModeCatalog>(ModesPath);
            if (!modes)
            {
                modes = ScriptableObject.CreateInstance<PuzzleModeCatalog>();
                modes.Configure(new[] {
                    new PuzzleModeDefinition(3, true, campaign.Catalog, "progress-v1.json"),
                    new PuzzleModeDefinition(4, false, null, "progress-4x4-v1.json"),
                    new PuzzleModeDefinition(5, false, null, "progress-5x5-v1.json")});
                AssetDatabase.CreateAsset(modes, ModesPath);
            }
            var back = Button("MainMenuBackButton", selectionDesign, "MENU", -380, 94, 190, 68);
            back.GetComponentInChildren<TMP_Text>().fontSize = 23;
            var eyebrow = selectionDesign.Find("SelectionEyebrow").GetComponent<TMP_Text>();
            eyebrow.text = "3 × 3 COLLECTION";
            Top(eyebrow.rectTransform, 120, 98, 680, 48);

            var main = Panel("MainMenu", canvas, out var design);
            Label("MenuEyebrow", design, "A COLOR SLIDING PUZZLE", 0, 105, 1000, 50, 25, PuzzlePalette.Hex("A1AFC4"));
            BuildDecoration(design);
            Label("GameTitle", design, "COLOR MOVE", 0, 660, 1040, 150, 110, PuzzlePalette.Hex("F2F4F8"), FontStyles.Bold);
            Label("MenuSubtitle", design, "A LITTLE SHIFT. A PERFECT MATCH.", 0, 758, 1020, 48, 26, PuzzlePalette.Hex("A1AFC4"));
            var resume = Button("ContinueButton", design, "CONTINUE", 0, 948, 888, 138);
            var resumeTitle = resume.GetComponentInChildren<TMP_Text>();
            Place(resumeTitle.rectTransform, 0, 22, 850, 54);
            resumeTitle.fontSize = 34;
            var detail = Label("ContinueDetail", resume.transform, "SELECT A PUZZLE TO BEGIN", 0, 0, 850, 36, 22, PuzzlePalette.Hex("46546A"));
            Place(detail.rectTransform, 0, -28, 850, 36);
            var resumeColors = resume.colors;
            resumeColors.disabledColor = PuzzlePalette.Hex("718098");
            resume.colors = resumeColors;
            resume.interactable = false;
            Label("GridCaption", design, "CHOOSE YOUR GRID", 0, 1090, 900, 44, 25, PuzzlePalette.Hex("A1AFC4"), FontStyles.Bold);
            var cards = new PuzzleModeButtonView[3];
            for (int i = 0; i < 3; i++)
            {
                int size = i + 3;
                var button = Button($"Mode{size}Button", design, "", (i - 1) * 304, 1315, 280, 320);
                button.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
                var image = button.GetComponent<UnityEngine.UI.Image>();
                var colors = button.colors;
                colors.disabledColor = Color.white;
                button.colors = colors;
                var title = Label("ModeTitle", button.transform, $"{size} × {size}", 0, 0, 260, 70, 48, Color.white, FontStyles.Bold);
                Place(title.rectTransform, 0, 100, 260, 70);
                var caption = Label("ModeAvailability", button.transform, "COMING SOON", 0, 0, 270, 50, 23, PuzzlePalette.Hex("8190A6"), FontStyles.Bold);
                Place(caption.rectTransform, 0, -108, 270, 50);
                BuildModeIcon(button.transform, size, i == 0);
                var card = button.gameObject.AddComponent<PuzzleModeButtonView>();
                card.Configure(size, button, image, title, caption);
                card.Refresh(modes.Find(size), i == 0);
                cards[i] = card;
            }
            var options = Button("SettingsButton", design, "SETTINGS", 0, 1660, 600, 100);
            var storage = Label("MenuStorageStatus", design, "TAKE YOUR TIME. FIND YOUR MATCH.", 0, 1840, 1050, 48, 22, PuzzlePalette.Hex("A1AFC4"));
            var mainView = main.AddComponent<PuzzleMainMenuView>();
            mainView.Configure(resume, options, detail, storage, cards);

            var preferences = Panel("SettingsPanel", canvas, out var settingsDesign);
            Label("SettingsEyebrow", settingsDesign, "MAKE IT YOUR OWN", 0, 320, 1000, 44, 27, PuzzlePalette.Hex("A1AFC4"));
            Label("SettingsTitle", settingsDesign, "Settings", 0, 450, 1000, 140, 94, Color.white, FontStyles.Bold);
            var bgm = VolumeCard("Bgm", settingsDesign, "BGM VOLUME", 780, PuzzlePalette.Hex("F6CE64"), out var bgmValue);
            var sfx = VolumeCard("Sfx", settingsDesign, "SOUND EFFECTS", 1130, PuzzlePalette.Hex("5A9CF8"), out var sfxValue);
            var done = Button("SettingsDoneButton", settingsDesign, "DONE", 0, 1510, 600, 112);
            var saveStatus = Label("SettingsStorageStatus", settingsDesign, "YOUR PREFERENCES ARE SAVED AUTOMATICALLY", 0, 1745, 1040, 48, 22, PuzzlePalette.Hex("A1AFC4"));
            var settingsView = preferences.AddComponent<PuzzleSettingsView>();
            settingsView.Configure(bgm, sfx, bgmValue, sfxValue, saveStatus, done);
            settingsView.Refresh(new PuzzleMenuProfile(), null);
            preferences.SetActive(false);
            campaign.Selection.gameObject.SetActive(false);
            campaign.SetMenuManagedStartup();
            var menu = campaign.gameObject.AddComponent<PuzzleMenuController>();
            menu.Configure(campaign, modes, mainView, settingsView, back);
            Canvas.ForceUpdateCanvases();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Menu scene save failed.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = main;
            Debug.Log("Main menu, three data-driven mode cards and saved volume settings authored in the existing scene.");
        }

        static GameObject Panel(string name, Transform parent, out RectTransform design)
        {
            var background = Image(name, parent, PuzzlePalette.Hex("101A2C"), true, false);
            Stretch(background.rectTransform);
            var safe = Node(name + "SafeArea", background.transform);
            Stretch(safe);
            design = Node(name + "Content", safe);
            Place(design, 0, 0, 1080, 1920);
            background.gameObject.AddComponent<PuzzleScreenLayout>().Configure(safe, design);
            return background.gameObject;
        }

        static void BuildDecoration(Transform parent)
        {
            var board = Image("DecorativeTiles", parent, PuzzlePalette.Hex("0A1322"), false);
            Top(board.rectTransform, 0, 365, 360, 360);
            var colors = new[] {TileColor.Red, TileColor.Red, TileColor.Blue, TileColor.Blue,
                TileColor.Yellow, TileColor.Yellow, TileColor.Black, TileColor.Empty, TileColor.Empty};
            for (int i = 0; i < 9; i++)
            {
                var tile = Image($"DecorativeCell{i}", board.transform,
                    colors[i] == TileColor.Empty ? PuzzlePalette.Hex("19253A") : PuzzlePalette.Tile(colors[i]), false);
                Place(tile.rectTransform, (i % 3 - 1) * 112, (1 - i / 3) * 112, 102, 102);
            }
        }

        static void BuildModeIcon(Transform parent, int size, bool available)
        {
            float cell = 110f / size, gap = 4;
            for (int i = 0; i < size * size; i++)
            {
                var dot = Image($"ModeIconCell{i}", parent, available ? PuzzlePalette.Hex("50627E") : PuzzlePalette.Hex("35445C"), false);
                Place(dot.rectTransform, -55 + cell * (i % size + 0.5f), 55 - cell * (i / size + 0.5f), cell - gap, cell - gap);
            }
        }

        static UnityEngine.UI.Slider VolumeCard(string name, Transform parent, string caption, float top, Color accent, out TMP_Text value)
        {
            var card = Image(name + "VolumeCard", parent, PuzzlePalette.Hex("19273B"), false);
            Top(card.rectTransform, 0, top, 888, 270);
            var label = Label(name + "Caption", card.transform, caption, 0, 0, 610, 60, 29, Color.white, FontStyles.Bold);
            Place(label.rectTransform, -76, 73, 610, 60);
            label.alignment = TextAlignmentOptions.Left;
            value = Label(name + "Value", card.transform, "", 0, 0, 160, 60, 32, accent, FontStyles.Bold);
            Place(value.rectTransform, 320, 73, 160, 60);
            var root = Image(name + "Slider", card.transform, Color.clear, true, false);
            Place(root.rectTransform, 0, -38, 760, 100);
            var track = Image("Background", root.transform, PuzzlePalette.Hex("3A4960"), false);
            Place(track.rectTransform, 0, 0, 760, 12);
            var fillArea = Node("FillArea", root.transform);
            Place(fillArea, 0, 0, 712, 12);
            var fill = Image("Fill", fillArea, accent, false);
            Stretch(fill.rectTransform);
            var handleArea = Node("HandleArea", root.transform);
            // Slider stretches the handle vertically; a zero-height rail keeps its 48px thumb square.
            Place(handleArea, 0, 0, 712, 0);
            var handle = Image("Handle", handleArea, Color.white, true);
            Place(handle.rectTransform, 0, 0, 48, 48);
            var slider = root.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0; slider.maxValue = 1;
            slider.wholeNumbers = false;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            var navigation = slider.navigation;
            navigation.mode = UnityEngine.UI.Navigation.Mode.None;
            slider.navigation = navigation;
            return slider;
        }
    }
}
