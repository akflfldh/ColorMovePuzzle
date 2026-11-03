using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorMovePuzzle
{
    public sealed class PuzzleBoardView : MonoBehaviour
    {
        [SerializeField] RectTransform tileLayer;
        [SerializeField] PuzzleTileView[] tiles;
        [SerializeField] int boardSize = 3;
        [SerializeField] float padding = 24;
        [SerializeField] float spacing = 16;
        public IReadOnlyList<PuzzleTileView> Tiles => tiles;
        public int Size => boardSize;
        public RectTransform Rect => (RectTransform)transform;

        public void Configure(int size, float inset, float gap, RectTransform layer, PuzzleTileView[] tileViews)
        {
            boardSize = size;
            padding = inset;
            spacing = gap;
            tileLayer = layer;
            tiles = tileViews;
        }

        public float CellSize => (Rect.rect.width - padding * 2 - spacing * (boardSize - 1)) / boardSize;

        public Vector2 CellPosition(int index)
        {
            float extent = (Rect.rect.width - CellSize) * 0.5f - padding;
            return new Vector2(-extent + index % boardSize * (CellSize + spacing),
                extent - index / boardSize * (CellSize + spacing));
        }

        public void Repaint(PuzzleBoard board, Action<int> onTap = null, Action<int, Vector2> onSwipe = null)
        {
            if (board.Size != boardSize) throw new ArgumentException("View and stage sizes differ.");
            int tile = 0;
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i] == TileColor.Empty) continue;
                var view = tiles[tile++];
                view.Bind(i, board[i], onTap, onSwipe);
                view.Rect.sizeDelta = Vector2.one * CellSize;
                view.Rect.anchoredPosition = CellPosition(i);
            }
            if (tile != tiles.Length) throw new ArgumentException("View tile count differs from board.");
        }

        public PuzzleTileView FindTile(int index)
        {
            foreach (var tile in tiles) if (tile.CellIndex == index) return tile;
            return null;
        }
    }
}
