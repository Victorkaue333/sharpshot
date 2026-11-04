namespace SharpShot.Core.Enums;

/// <summary>Which part of a selection rectangle the pointer is interacting with.</summary>
public enum ResizeHandle
{
    None = 0,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left,
    /// <summary>Pointer is inside the rectangle (drag moves it).</summary>
    Move,
}
