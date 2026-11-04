using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using SharpShot.App.Interop;
using SharpShot.App.Services;
using SharpShot.Core.Enums;
using SharpShot.Core.Models;
using SharpShot.Core.Services;
using Windows.Graphics;
using Windows.System;

namespace SharpShot.App.Views;

/// <summary>Focusable host for the overlay that also owns the crosshair cursor.</summary>
public sealed partial class SelectionSurface : ContentControl
{
    public SelectionSurface()
    {
        DefaultStyleKey = typeof(ContentControl);
    }

    public void SetCursor(InputSystemCursorShape shape) => ProtectedCursor = InputSystemCursor.Create(shape);
}

/// <summary>
/// Full-monitor borderless window that shows the frozen frame of one monitor and lets the user draw,
/// move and resize a selection. All selection math is in physical pixels (via RegionMath); XAML gets DIPs.
/// </summary>
public sealed partial class RegionOverlayWindow : Window
{
    private const int HandleTolerancePx = 10;
    private const double ToolbarGap = 12;

    private readonly RegionSelectionSession _session;
    private readonly PixelRectangle _bounds; // monitor-local: (0,0,w,h)
    private PixelRectangle _selection = PixelRectangle.Empty;
    private double _scale = 1.0;
    private bool _dragging;
    private ResizeHandle _activeHandle = ResizeHandle.None;
    private int _dragStartX;
    private int _dragStartY;
    private PixelRectangle _dragOrigin;
    private uint _capturedPointerId;

    public RegionOverlayWindow(CapturedFrame frame, RegionSelectionSession session)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(session);
        Frame = frame;
        _session = session;
        _bounds = new PixelRectangle(0, 0, frame.Pixels.Width, frame.Pixels.Height);

        InitializeComponent();
        _scale = frame.Monitor.ScaleFactor;

        FrozenImage.Source = CreateBitmap(frame.Pixels);
        Surface.SetCursor(InputSystemCursorShape.Cross);

        Surface.PointerPressed += OnPointerPressed;
        Surface.PointerMoved += OnPointerMoved;
        Surface.PointerReleased += OnPointerReleased;
        Surface.PointerCanceled += OnPointerCanceled;
        Surface.PointerCaptureLost += OnPointerCanceled;
        Surface.KeyDown += OnKeyDown;
        Surface.SizeChanged += (_, _) => Render();
        Surface.Loaded += (_, _) =>
        {
            _scale = Surface.XamlRoot?.RasterizationScale ?? _scale;
            Render();
            Surface.Focus(FocusState.Programmatic);
        };
        Activated += (_, _) => Surface.Focus(FocusState.Programmatic);
    }

    public CapturedFrame Frame { get; }

    public void ShowOverlay()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        if (!WindowHost.AllowSelfCapture)
        {
            User32.SetWindowDisplayAffinity(hwnd, User32.WdaExcludeFromCapture);
        }

        var presenter = OverlappedPresenter.Create();
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = false;
        presenter.IsMinimizable = false;
        presenter.IsMaximizable = false;
        presenter.IsAlwaysOnTop = true;

        var appWindow = AppWindow;
        appWindow.SetPresenter(presenter);
        appWindow.IsShownInSwitchers = false;
        var b = Frame.Monitor.Bounds;
        appWindow.MoveAndResize(new RectInt32(b.X, b.Y, b.Width, b.Height));
        appWindow.Show();
    }

    public void CloseOverlay()
    {
        try
        {
            Close();
        }
        catch (Exception)
        {
            // Already closed.
        }
    }

    public bool ContainsCursor()
        => User32.GetCursorPos(out var p) && Frame.Monitor.Bounds.Contains(p.X, p.Y);

    public void ClearSelection()
    {
        _selection = PixelRectangle.Empty;
        _dragging = false;
        _activeHandle = ResizeHandle.None;
        Render();
    }

    // ----------------------------------------------------------------- input

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Surface);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var (px, py) = ToPixels(point.Position.X, point.Position.Y);
        _session.SelectionStarted(this);

        var handle = _selection.IsEmpty ? ResizeHandle.None : RegionMath.HitTest(_selection, px, py, HandleTolerancePx);
        _activeHandle = handle;
        _dragStartX = px;
        _dragStartY = py;
        _dragOrigin = _selection;
        _dragging = true;

        if (handle == ResizeHandle.None)
        {
            _selection = PixelRectangle.Empty;
        }

        _capturedPointerId = e.Pointer.PointerId;
        Surface.CapturePointer(e.Pointer);
        Hint.Visibility = Visibility.Collapsed;
        Render();
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Surface);
        var (px, py) = ToPixels(point.Position.X, point.Position.Y);

        if (!_dragging)
        {
            UpdateCursor(px, py);
            return;
        }

        _selection = _activeHandle switch
        {
            ResizeHandle.None => RegionMath.FromDrag(_dragStartX, _dragStartY, px, py, _bounds),
            ResizeHandle.Move => RegionMath.Move(_dragOrigin, px - _dragStartX, py - _dragStartY, _bounds),
            _ => RegionMath.Resize(_dragOrigin, _activeHandle, px, py, _bounds),
        };
        Render();
        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        _activeHandle = ResizeHandle.None;
        Surface.ReleasePointerCapture(e.Pointer);
        if (_selection.IsEmpty)
        {
            Hint.Visibility = Visibility.Visible;
        }

        Render();
        e.Handled = true;
    }

    private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerId != _capturedPointerId)
        {
            return;
        }

        _dragging = false;
        _activeHandle = ResizeHandle.None;
        Render();
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Escape:
                e.Handled = true;
                _session.Cancel();
                break;
            case VirtualKey.Enter:
            case VirtualKey.Space:
                if (!_selection.IsEmpty)
                {
                    e.Handled = true;
                    _session.Confirm(this, _selection);
                }

                break;
        }
    }

    private void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_selection.IsEmpty)
        {
            _session.Confirm(this, _selection);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => _session.Cancel();

    private void UpdateCursor(int px, int py)
    {
        var handle = _selection.IsEmpty ? ResizeHandle.None : RegionMath.HitTest(_selection, px, py, HandleTolerancePx);
        Surface.SetCursor(handle switch
        {
            ResizeHandle.TopLeft or ResizeHandle.BottomRight => InputSystemCursorShape.SizeNorthwestSoutheast,
            ResizeHandle.TopRight or ResizeHandle.BottomLeft => InputSystemCursorShape.SizeNortheastSouthwest,
            ResizeHandle.Top or ResizeHandle.Bottom => InputSystemCursorShape.SizeNorthSouth,
            ResizeHandle.Left or ResizeHandle.Right => InputSystemCursorShape.SizeWestEast,
            ResizeHandle.Move => InputSystemCursorShape.SizeAll,
            _ => InputSystemCursorShape.Cross,
        });
    }

    // ----------------------------------------------------------------- rendering

    private void Render()
    {
        var width = Surface.ActualWidth;
        var height = Surface.ActualHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var full = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, width, height) };
        var group = new GeometryGroup { FillRule = FillRule.EvenOdd };
        group.Children.Add(full);

        if (_selection.IsEmpty)
        {
            DimPath.Data = group;
            SelectionBorder.Visibility = Visibility.Collapsed;
            SizeBadge.Visibility = Visibility.Collapsed;
            Toolbar.Visibility = Visibility.Collapsed;
            SetHandlesVisible(false);
            PositionHint(width, height);
            return;
        }

        var left = RegionMath.PixelToDip(_selection.X, _scale);
        var top = RegionMath.PixelToDip(_selection.Y, _scale);
        var w = RegionMath.PixelToDip(_selection.Width, _scale);
        var h = RegionMath.PixelToDip(_selection.Height, _scale);

        group.Children.Add(new RectangleGeometry { Rect = new Windows.Foundation.Rect(left, top, w, h) });
        DimPath.Data = group;

        Canvas.SetLeft(SelectionBorder, left);
        Canvas.SetTop(SelectionBorder, top);
        SelectionBorder.Width = w;
        SelectionBorder.Height = h;
        SelectionBorder.Visibility = Visibility.Visible;

        PlaceHandle(HandleTL, left, top);
        PlaceHandle(HandleT, left + w / 2, top);
        PlaceHandle(HandleTR, left + w, top);
        PlaceHandle(HandleR, left + w, top + h / 2);
        PlaceHandle(HandleBR, left + w, top + h);
        PlaceHandle(HandleB, left + w / 2, top + h);
        PlaceHandle(HandleBL, left, top + h);
        PlaceHandle(HandleL, left, top + h / 2);
        SetHandlesVisible(!_dragging || _activeHandle != ResizeHandle.None);

        SizeText.Text = $"{_selection.Width} × {_selection.Height}";
        SizeBadge.Visibility = Visibility.Visible;
        SizeBadge.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var badgeW = SizeBadge.DesiredSize.Width;
        var badgeH = SizeBadge.DesiredSize.Height;
        var badgeX = Math.Clamp(left + (w - badgeW) / 2, 0, Math.Max(0, width - badgeW));
        var badgeY = top - badgeH - 8;
        if (badgeY < 0)
        {
            badgeY = Math.Min(top + 8, height - badgeH);
        }

        Canvas.SetLeft(SizeBadge, badgeX);
        Canvas.SetTop(SizeBadge, badgeY);

        Toolbar.Visibility = _dragging ? Visibility.Collapsed : Visibility.Visible;
        if (!_dragging)
        {
            Toolbar.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            var tbW = Toolbar.DesiredSize.Width;
            var tbH = Toolbar.DesiredSize.Height;
            var tbX = Math.Clamp(left + (w - tbW) / 2, 0, Math.Max(0, width - tbW));
            var tbY = top + h + ToolbarGap;
            if (tbY + tbH > height)
            {
                tbY = Math.Max(0, top - tbH - ToolbarGap);
                if (tbY < badgeY + badgeH && tbY + tbH > badgeY)
                {
                    tbY = Math.Max(0, top + h - tbH - ToolbarGap);
                }
            }

            Canvas.SetLeft(Toolbar, tbX);
            Canvas.SetTop(Toolbar, tbY);
        }
    }

    private void PositionHint(double width, double height)
    {
        Hint.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(Hint, Math.Max(0, (width - Hint.DesiredSize.Width) / 2));
        Canvas.SetTop(Hint, Math.Max(0, height * 0.08));
    }

    private static void PlaceHandle(Rectangle handle, double x, double y)
    {
        Canvas.SetLeft(handle, x - handle.Width / 2);
        Canvas.SetTop(handle, y - handle.Height / 2);
    }

    private void SetHandlesVisible(bool visible)
    {
        var visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        HandleTL.Visibility = visibility;
        HandleT.Visibility = visibility;
        HandleTR.Visibility = visibility;
        HandleR.Visibility = visibility;
        HandleBR.Visibility = visibility;
        HandleB.Visibility = visibility;
        HandleBL.Visibility = visibility;
        HandleL.Visibility = visibility;
    }

    private (int X, int Y) ToPixels(double dipX, double dipY)
        => RegionMath.ClampPoint(RegionMath.DipToPixel(dipX, _scale), RegionMath.DipToPixel(dipY, _scale), _bounds);

    private static WriteableBitmap CreateBitmap(PixelBuffer pixels)
    {
        var bitmap = new WriteableBitmap(pixels.Width, pixels.Height);
        using (var stream = bitmap.PixelBuffer.AsStream())
        {
            stream.Write(pixels.Data, 0, pixels.Data.Length);
        }

        bitmap.Invalidate();
        return bitmap;
    }
}
