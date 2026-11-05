using UnityEngine;

public enum CellAnchor { Center, Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight }

public static class CellAnchorExtensions
{
    public static Vector2 ToOffset(this CellAnchor anchor, float d = 0.25f)
    {
        switch (anchor)
        {
            case CellAnchor.Top: return new Vector2(d, d);
            case CellAnchor.Bottom: return new Vector2(-d, -d);
            case CellAnchor.Left: return new Vector2(-d, d);
            case CellAnchor.Right: return new Vector2(d, -d);
            case CellAnchor.TopRight: return new Vector2(d, 0);
            case CellAnchor.TopLeft: return new Vector2(0, d);
            case CellAnchor.BottomLeft: return new Vector2(-d, 0);
            case CellAnchor.BottomRight: return new Vector2(0, -d);
            default: return Vector2.zero;
        }
    }
}