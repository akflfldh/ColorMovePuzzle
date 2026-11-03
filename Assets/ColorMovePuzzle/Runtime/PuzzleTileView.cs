using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ColorMovePuzzle
{
    [RequireComponent(typeof(RectTransform), typeof(UnityEngine.UI.Image))]
    public sealed class PuzzleTileView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] UnityEngine.UI.Image tileImage;
        [SerializeField] TMP_Text symbol;
        Action<int> tapped;
        Action<int, Vector2> swiped;
        int pointerId = int.MinValue;
        Vector2 pressPosition;
        bool dragged;

        public int CellIndex { get; private set; }
        public RectTransform Rect => (RectTransform)transform;
        public bool Interactive => tileImage.raycastTarget;

        public void Configure(UnityEngine.UI.Image image, TMP_Text label)
        {
            tileImage = image;
            symbol = label;
        }

        public void Bind(int index, TileColor color, Action<int> onTap, Action<int, Vector2> onSwipe)
        {
            CellIndex = index;
            tapped = onTap;
            swiped = onSwipe;
            tileImage.color = PuzzlePalette.Tile(color);
            tileImage.raycastTarget = onTap != null;
            symbol.text = PuzzlePalette.Symbol(color);
            symbol.color = color == TileColor.Yellow ? PuzzlePalette.Hex("54441D") : Color.white;
            pointerId = int.MinValue;
            dragged = false;
        }

        public void SetCellIndex(int index) => CellIndex = index;

        // Positions stay in board-local units, independent of Canvas scale or screen resolution.
        public IEnumerator SlideTo(Vector2 destination, float duration)
        {
            Vector2 start = Rect.anchoredPosition;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Rect.anchoredPosition = Vector2.LerpUnclamped(start, destination, t * t * (3f - 2f * t));
                yield return null;
            }
            Rect.anchoredPosition = destination;
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || pointerId != int.MinValue) return;
            pointerId = data.pointerId;
            pressPosition = data.position;
            dragged = false;
        }

        public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = true;
        public void OnBeginDrag(PointerEventData data)
        {
            if (data.pointerId == pointerId) dragged = true;
        }
        public void OnDrag(PointerEventData data) { }

        public void OnEndDrag(PointerEventData data)
        {
            if (data.pointerId != pointerId) return;
            Vector2 delta = LocalDelta(data);
            dragged = true;
            pointerId = int.MinValue;
            if (delta.magnitude >= Rect.rect.width * 0.12f) swiped?.Invoke(CellIndex, delta);
        }

        public void OnPointerClick(PointerEventData data)
        {
            if (data.pointerId != pointerId) return;
            // InputSystem can send click before end-drag when release is still inside this tile.
            // Preserve the captured pointer so OnEndDrag can commit the directional move.
            if (dragged || data.dragging) return;
            bool tap = !dragged && LocalDelta(data).magnitude < Rect.rect.width * 0.12f;
            pointerId = int.MinValue;
            if (tap) tapped?.Invoke(CellIndex);
        }

        // Pointer-up precedes click/end-drag. Release abandoned/canceled gestures next frame.
        public void OnPointerUp(PointerEventData data)
        {
            if (data.pointerId == pointerId) StartCoroutine(ReleasePointer(data.pointerId));
        }

        IEnumerator ReleasePointer(int releasedId)
        {
            yield return null;
            if (pointerId == releasedId) pointerId = int.MinValue;
        }

        Vector2 LocalDelta(PointerEventData data)
        {
            var parent = (RectTransform)Rect.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pressPosition, data.pressEventCamera, out var start);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, data.position, data.pressEventCamera, out var end);
            return end - start;
        }

        void OnDisable() { pointerId = int.MinValue; dragged = false; }
    }
}
