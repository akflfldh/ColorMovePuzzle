var menu = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleMenuController>();
foreach (var slider in new[] {menu.Settings.BgmSlider, menu.Settings.SfxSlider})
{
    var area = (UnityEngine.RectTransform)slider.handleRect.parent;
    area.SetSizeWithCurrentAnchors(UnityEngine.RectTransform.Axis.Vertical, 0);
    slider.handleRect.sizeDelta = new UnityEngine.Vector2(48, 48);
    if (!UnityEngine.Application.isPlaying) UnityEditor.EditorUtility.SetDirty(area);
}
UnityEngine.Canvas.ForceUpdateCanvases();
if (!UnityEngine.Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
return new {bgmThumbHeight = menu.Settings.BgmSlider.handleRect.rect.height, sfxThumbHeight = menu.Settings.SfxSlider.handleRect.rect.height};
