using UnityEngine;

namespace ColorMovePuzzle
{
    public static class PuzzlePalette
    {
        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }

        public static Color Tile(TileColor color)
        {
            switch (color)
            {
                case TileColor.Red: return Hex("EF6266");
                case TileColor.Blue: return Hex("5A9CF8");
                case TileColor.Yellow: return Hex("F6CE64");
                case TileColor.Black: return Hex("22252E");
                case TileColor.Green: return Hex("53BC91");
                case TileColor.Purple: return Hex("AD83E8");
                default: return Color.clear;
            }
        }

        public static string Symbol(TileColor color)
        {
            switch (color)
            {
                case TileColor.Red: return "R";
                case TileColor.Blue: return "B";
                case TileColor.Yellow: return "Y";
                case TileColor.Black: return "K";
                case TileColor.Green: return "G";
                case TileColor.Purple: return "P";
                default: return "";
            }
        }
    }
}
