namespace SharpShot.Core.Models;

/// <summary>
/// An axis-aligned rectangle in physical pixels. Right and Bottom are exclusive.
/// Coordinates may be negative on the Windows virtual desktop.
/// </summary>
public readonly record struct PixelRectangle(int X, int Y, int Width, int Height)
{
    public static readonly PixelRectangle Empty = new(0, 0, 0, 0);

    public int Left => X;
    public int Top => Y;
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public bool IsEmpty => Width <= 0 || Height <= 0;
    public long Area => (long)Math.Max(Width, 0) * Math.Max(Height, 0);

    /// <summary>Creates a normalized rectangle from two arbitrary corner points.</summary>
    public static PixelRectangle FromPoints(int x1, int y1, int x2, int y2)
    {
        var left = Math.Min(x1, x2);
        var top = Math.Min(y1, y2);
        return new PixelRectangle(left, top, Math.Abs(x2 - x1), Math.Abs(y2 - y1));
    }

    public static PixelRectangle FromLTRB(int left, int top, int right, int bottom)
        => new(left, top, right - left, bottom - top);

    public bool Contains(int x, int y) => x >= Left && x < Right && y >= Top && y < Bottom;

    public bool Contains(PixelRectangle other)
        => other.Left >= Left && other.Top >= Top && other.Right <= Right && other.Bottom <= Bottom;

    public PixelRectangle Intersect(PixelRectangle other)
    {
        var left = Math.Max(Left, other.Left);
        var top = Math.Max(Top, other.Top);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);
        return right <= left || bottom <= top ? Empty : FromLTRB(left, top, right, bottom);
    }

    public PixelRectangle Offset(int dx, int dy) => this with { X = X + dx, Y = Y + dy };

    /// <summary>Returns this rectangle expressed relative to <paramref name="origin"/>'s top-left corner.</summary>
    public PixelRectangle RelativeTo(PixelRectangle origin) => Offset(-origin.X, -origin.Y);

    public override string ToString() => $"{Width}×{Height} @ ({X}, {Y})";
}
