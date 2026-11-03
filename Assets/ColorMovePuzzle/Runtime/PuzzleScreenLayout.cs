using UnityEngine;

namespace ColorMovePuzzle
{
    [ExecuteAlways]
    public sealed class PuzzleScreenLayout : MonoBehaviour
    {
        [SerializeField] RectTransform safeArea;
        [SerializeField] RectTransform content;
        public void Configure(RectTransform safe, RectTransform design) { safeArea = safe; content = design; }

        void OnEnable() => Refresh();
        void Update() => Refresh();

        public void Refresh()
        {
            if (!safeArea || !content || Screen.width <= 0 || Screen.height <= 0) return;
            Rect area = Screen.safeArea;
            safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            float scale = Mathf.Min(safeArea.rect.width / 1080f, safeArea.rect.height / 1920f);
            content.localScale = Vector3.one * Mathf.Max(0.001f, scale);
        }
    }
}
