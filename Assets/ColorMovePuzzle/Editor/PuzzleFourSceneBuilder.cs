using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ColorMovePuzzle.Editor.PuzzleSceneBuilder;

namespace ColorMovePuzzle.Editor
{
    public static class PuzzleFourSceneBuilder
    {
        public static void EnableCampaign()
        {
            var scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != ScenePath || scene.isDirty) throw new InvalidOperationException("Open the saved puzzle scene outside Play mode.");
            var game = UnityEngine.Object.FindAnyObjectByType<PuzzleGameController>();
            var four = AssetDatabase.LoadAssetAtPath<PuzzleStageCatalog>(PuzzleFourByFourGenerator.CatalogPath);
            var modes = AssetDatabase.LoadAssetAtPath<PuzzleModeCatalog>(PuzzleMenuSceneBuilder.ModesPath);
            if (!game.SupportsSize(4) || !four || four.Count != 30 || !modes) throw new InvalidOperationException("Verify the prototype and generate all stages first.");
            modes.Configure(new[] {modes.Find(3), new PuzzleModeDefinition(4, true, four, "progress-4x4-v1.json"), modes.Find(5)});
            EditorUtility.SetDirty(modes);
            var menu = UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>();
            menu.MainMenu.ModeButtons[1].Refresh(modes.Find(4), true);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        public static void PreparePrototype()
        {
            var scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != ScenePath || scene.isDirty) throw new InvalidOperationException("Open the saved puzzle scene outside Play mode.");
            var game = UnityEngine.Object.FindAnyObjectByType<PuzzleGameController>();
            if (!game || game.SupportsSize(4)) throw new InvalidOperationException("Existing controller missing or 4x4 already registered.");
            var stage = AssetDatabase.LoadAssetAtPath<PuzzleStageData>(PuzzleFourByFourGenerator.PrototypePath);
            if (!stage) throw new InvalidOperationException("Create the prototype data first.");
            LoadUIAssets();
            var board3 = game.BoardView;
            var target3 = game.TargetView;
            var board4 = CreateBoard("CurrentBoard4", board3.transform.parent, 0, 1264, 840, 24, 12, stage.CreateStartingBoard(), true);
            var target4 = CreateBoard("TargetGrid4", target3.transform.parent, 0, 548, 300, 12, 5, new PuzzleBoard(4, stage.Target), false);
            game.ConfigureViewSet(new[] {board3, board4}, new[] {target3, target4});
            board4.gameObject.SetActive(false); target4.gameObject.SetActive(false);
            var selection = UnityEngine.Object.FindAnyObjectByType<PuzzleCampaignController>().Selection;
            selection.ConfigureCollectionLabel(selection.transform.Find("SelectionSafeArea/SelectionContent/SelectionEyebrow").GetComponent<TMP_Text>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Single 4x4 prototype UI registered with the shared game controller; menu remains unchanged pending validation.");
        }
    }
}
