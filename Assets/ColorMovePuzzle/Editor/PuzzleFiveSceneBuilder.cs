using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ColorMovePuzzle.Editor.PuzzleSceneBuilder;

namespace ColorMovePuzzle.Editor
{
    public static class PuzzleFiveSceneBuilder
    {
        static void RequireSavedScene()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying || scene.path!=ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open the saved puzzle scene outside Play mode.");
        }
        public static void PreparePrototype()
        {
            RequireSavedScene();
            var game=UnityEngine.Object.FindAnyObjectByType<PuzzleGameController>();
            if(!game || game.SupportsSize(5))throw new InvalidOperationException("Existing controller missing or 5x5 already registered.");
            var stage=AssetDatabase.LoadAssetAtPath<PuzzleStageData>(PuzzleFiveByFiveGenerator.PrototypePath);
            if(!stage)throw new InvalidOperationException("Generate the qualifying prototype first.");
            LoadUIAssets();
            var boards=UnityEngine.Object.FindObjectsByType<PuzzleBoardView>(FindObjectsInactive.Include);
            var board3=boards.Single(v=>v.name=="CurrentBoard");var board4=boards.Single(v=>v.name=="CurrentBoard4");
            var target3=boards.Single(v=>v.name=="TargetGrid");var target4=boards.Single(v=>v.name=="TargetGrid4");
            var board5=CreateBoard("CurrentBoard5",board3.transform.parent,0,1264,840,22,10,stage.CreateStartingBoard(),true);
            var target5=CreateBoard("TargetGrid5",target3.transform.parent,0,548,300,10,4,new PuzzleBoard(5,stage.Target),false);
            game.ConfigureViewSet(new[]{board3,board4,board5},new[]{target3,target4,target5});
            // Only the preview background receives the click. Its tiles remain read-only.
            var open=target5.gameObject.AddComponent<UnityEngine.UI.Button>();open.targetGraphic=target5.GetComponent<UnityEngine.UI.Image>();
            open.targetGraphic.raycastTarget=true;
            var nav=open.navigation;nav.mode=Navigation.Mode.None;open.navigation=nav;
            var zoomHint=Label("ZoomHint",target5.transform,"TAP TO ENLARGE",0,0,360,30,18,PuzzlePalette.Hex("A1AFC4"));
            Place(zoomHint.rectTransform,0,-174,360,30);
            var canvas=GameObject.Find("PuzzleCanvas").transform;
            var overlay=Image("TargetZoom",canvas,new Color(0.025f,0.045f,0.08f,0.97f),true,false);Stretch(overlay.rectTransform);
            var safe=Node("ZoomSafeArea",overlay.transform);Stretch(safe);
            var content=Node("ZoomContent",safe);Place(content,0,0,1080,1920);
            overlay.gameObject.AddComponent<PuzzleScreenLayout>().Configure(safe,content);
            Label("Title",content,"TARGET PATTERN",0,380,900,80,46,Color.white,FontStyles.Bold);
            var enlarged=CreateBoard("ExpandedTarget5",content,0,920,800,26,12,new PuzzleBoard(5,stage.Target),false);
            foreach(var label in enlarged.GetComponentsInChildren<TMP_Text>())label.fontSize=48;
            Label("Hint",content,"Match every color and all three empty cells.",0,1400,980,60,28,PuzzlePalette.Hex("A1AFC4"));
            var close=Button("CloseTargetButton",content,"BACK TO PUZZLE",0,1550,620,96);
            var zoom=game.gameObject.AddComponent<PuzzleTargetZoom>();zoom.Configure(game,overlay.gameObject,enlarged,open,close);
            UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>().ConfigureTargetZoom(zoom);
            board5.gameObject.SetActive(false);target5.gameObject.SetActive(false);overlay.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void EnableCampaign()
        {
            RequireSavedScene();
            var game=UnityEngine.Object.FindAnyObjectByType<PuzzleGameController>();
            var five=AssetDatabase.LoadAssetAtPath<PuzzleStageCatalog>(PuzzleFiveByFiveGenerator.CatalogPath);
            var modes=AssetDatabase.LoadAssetAtPath<PuzzleModeCatalog>(PuzzleMenuSceneBuilder.ModesPath);
            if(!game.SupportsSize(5)||!five||five.Count!=30||!modes)throw new InvalidOperationException("Verify prototype and generate 30 stages first.");
            modes.Configure(new[]{modes.Find(3),modes.Find(4),new PuzzleModeDefinition(5,true,five,"progress-5x5-v1.json")});EditorUtility.SetDirty(modes);
            UnityEngine.Object.FindAnyObjectByType<PuzzleMenuController>().MainMenu.ModeButtons[2].Refresh(modes.Find(5),true);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
    }
}
