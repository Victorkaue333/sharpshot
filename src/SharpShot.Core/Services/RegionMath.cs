using SharpShot.Core.Enums;
using SharpShot.Core.Models;

namespace SharpShot.Core.Services;

/// <summary>
/// Pure geometry used by the region selector: drag normalization, clamping, DPI conversion,
/// handle hit-testing and handle-driven resizing. Everything is in physical pixels unless noted.
/// </summary>
public static class RegionMath
{
    /// <summary>Converts a device-independent coordinate to a physical pixel coordinate.</summary>
    public static int DipToPixel(double dip, double scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(scale);
        return (int)Math.Round(dip * scale, MidpointRounding.AwayFromZero);
    }

    public static double PixelToDip(int pixel, double scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(scale);
        return pixel / scale;
    }

    /// <summary>Clamps a point so that it stays inside <paramref name="bounds"/> (inclusive of the far edge).</summary>
    public static (int X, int Y) ClampPoint(int x, int y, PixelRectangle bounds)
        => (Math.Clamp(x, bounds.Left, bounds.Right), Math.Clamp(y, bounds.Top, bounds.Bottom));

    /// <summary>Builds the selection rectangle for a drag from <c>start</c> to <c>current</c>, clamped to bounds.</summary>
    public static PixelRectangle FromDrag(int startX, int startY, int currentX, int currentY, PixelRectangle bounds)
    {
        var (sx, sy) = ClampPoint(startX, startY, bounds);
        var (cx, cy) = ClampPoint(currentX, currentY, bounds);
        return PixelRectangle.FromPoints(sx, sy, cx, cy);
    }

    public static PixelRectangle Clamp(PixelRectangle rect, PixelRectangle bounds) => rect.Intersect(bounds);

    /// <summary>Moves the rectangle by (dx, dy) without letting it leave <paramref name="bounds"/>.</summary>
    public static PixelRectangle Move(PixelRectangle rect, int dx, int dy, PixelRectangle bounds)
    {
        var x = Math.Clamp(rect.X + dx, bounds.Left, Math.Max(bounds.Left, bounds.Right - rect.Width));
        var y = Math.Clamp(rect.Y + dy, bounds.Top, Math.Max(bounds.Top, bounds.Bottom - rect.Height));
        return rect with { X = x, Y = y };
    }

    /// <summary>Determines which handle (or the body) of <paramref name="rect"/> is under the point.</summary>
    public static ResizeHandle HitTest(PixelRectangle rect, int x, int y, int tolerance)
    {
        if (rect.IsEmpty)
        {
            return ResizeHandle.None;
        }

        var nearLeft = Math.Abs(x - rect.Left) <= tolerance;
        var nearRight = Math.Abs(x - rect.Right) <= tolerance;
        var nearTop = Math.Abs(y - rect.Top) <= tolerance;
        var nearBottom = Math.Abs(y - rect.Bottom) <= tolerance;
        var withinX = x >= rect.Left - tolerance && x <= rect.Right + tolerance;
        var withinY = y >= rect.Top - tolerance && y <= rect.Bottom + tolerance;

        if (nearTop && nearLeft)
        {
            return ResizeHandle.TopLeft;
        }

        if (nearTop && nearRight)
        {
            return ResizeHandle.TopRight;
        }

        if (nearBottom && nearLeft)
        {
            return ResizeHandle.BottomLeft;
        }

        if (nearBottom && nearRight)
        {
            return ResizeHandle.BottomRight;
        }

        if (nearTop && withinX)
        {
            return ResizeHandle.Top;
        }

        if (nearBottom && withinX)
        {
            return ResizeHandle.Bottom;
        }

        if (nearLeft && withinY)
        {
            return ResizeHandle.Left;
        }

        if (nearRight && withinY)
        {
            return ResizeHandle.Right;
        }

        return rect.Contains(x, y) ? ResizeHandle.Move : ResizeHandle.None;
    }

    /// <summary>
    /// Resizes <paramref name="rect"/> by dragging <paramref name="handle"/> to (x, y). The opposite edge(s)
    /// stay anchored; crossing over flips the rectangle rather than producing a negative size.
    /// </summary>
    public static PixelRectangle Resize(PixelRectangle rect, ResizeHandle handle, int x, int y, PixelRectangle bounds)
    {
        var (px, py) = ClampPoint(x, y, bounds);
        int left = rect.Left, top = rect.Top, right = rect.Right, bottom = rect.Bottom;

        switch (handle)
        {
            case ResizeHandle.TopLeft:
                left = px;
                top = py;
                break;
            case ResizeHandle.Top:
                top = py;
                break;
            case ResizeHandle.TopRight:
                right = px;
                top = py;
                break;
            case ResizeHandle.Right:
                right = px;
                break;
            case ResizeHandle.BottomRight:
                right = px;
                bottom = py;
                break;
            case ResizeHandle.Bottom:
                bottom = py;
                break;
            case ResizeHandle.BottomLeft:
                left = px;
                bottom = py;
                break;
            case ResizeHandle.Left:
                left = px;
                break;
            default:
                return rect;
        }

        return PixelRectangle.FromPoints(left, top, right, bottom);
    }

    /// <summary>Returns the opposite handle, used when a drag crosses over the anchored edge.</summary>
    public static ResizeHandle Flip(ResizeHandle handle, bool horizontally, bool vertically)
    {
        var h = handle;
        if (horizontally)
        {
            h = h switch
            {
                ResizeHandle.TopLeft => ResizeHandle.TopRight,
                ResizeHandle.TopRight => ResizeHandle.TopLeft,
                ResizeHandle.BottomLeft => ResizeHandle.BottomRight,
                ResizeHandle.BottomRight => ResizeHandle.BottomLeft,
                ResizeHandle.Left => ResizeHandle.Right,
                ResizeHandle.Right => ResizeHandle.Left,
                _ => h,
            };
        }

        if (vertically)
        {
            h = h switch
            {
                ResizeHandle.TopLeft => ResizeHandle.BottomLeft,
                ResizeHandle.BottomLeft => ResizeHandle.TopLeft,
                ResizeHandle.TopRight => ResizeHandle.BottomRight,
                ResizeHandle.BottomRight => ResizeHandle.TopRight,
                ResizeHandle.Top => ResizeHandle.Bottom,
                ResizeHandle.Bottom => ResizeHandle.Top,
                _ => h,
            };
        }

        return h;
    }
}
