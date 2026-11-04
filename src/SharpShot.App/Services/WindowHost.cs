using Microsoft.UI.Dispatching;
using SharpShot.App.Interop;
using SharpShot.Capture.Services;

namespace SharpShot.App.Services;

/// <summary>
/// Owns the main window lifetime: show/hide/activate, hide-to-tray, and the "hide before capture" dance that
/// is synchronised with desktop composition (DwmFlush) instead of arbitrary sleeps.
/// </summary>
public interface IWindowHost
{
    bool IsMainWindowVisible { get; }

    DispatcherQueue DispatcherQueue { get; }

    void ShowMainWindow();

    void HideMainWindow();

    /// <summary>
    /// Hides every SharpShot window and waits for the compositor to present frames without them.
    /// Returns a callback that restores the previous visibility.
    /// </summary>
    Task<Func<Task>> HideForCaptureAsync();
}

internal sealed class WindowHost : IWindowHost
{
    private readonly Func<MainWindow> _windowFactory;
    private MainWindow? _window;
    private bool _visible;

    public WindowHost(Func<MainWindow> windowFactory, DispatcherQueue dispatcherQueue)
    {
        ArgumentNullException.ThrowIfNull(windowFactory);
        ArgumentNullException.ThrowIfNull(dispatcherQueue);
        _windowFactory = windowFactory;
        DispatcherQueue = dispatcherQueue;
    }

    /// <summary>Set SHARPSHOT_ALLOW_SELF_CAPTURE=1 to let SharpShot's own windows appear in screen captures (development only).</summary>
    public static bool AllowSelfCapture =>
        string.Equals(Environment.GetEnvironmentVariable("SHARPSHOT_ALLOW_SELF_CAPTURE"), "1", StringComparison.Ordinal);

    public bool IsMainWindowVisible => _visible;

    public DispatcherQueue DispatcherQueue { get; }

    public MainWindow? Window => _window;

    public void ShowMainWindow()
    {
        _window ??= CreateWindow();
        _window.AppWindow.Show();
        _window.Activate();
        _visible = true;
    }

    public void HideMainWindow()
    {
        if (_window is null)
        {
            return;
        }

        _window.AppWindow.Hide();
        _visible = false;
    }

    public async Task<Func<Task>> HideForCaptureAsync()
    {
        var wasVisible = _visible;
        if (wasVisible)
        {
            HideMainWindow();
        }

        // Even with WDA_EXCLUDEFROMCAPTURE set, wait for the compositor so the frame reflects the hidden window.
        await CompositionSync.WaitForCompositionAsync();

        return () =>
        {
            if (wasVisible)
            {
                ShowMainWindow();
            }

            return Task.CompletedTask;
        };
    }

    public void CloseAndExit()
    {
        _window?.ForceClose();
        _window = null;
        _visible = false;
    }

    private MainWindow CreateWindow()
    {
        var window = _windowFactory();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        // Never let our own UI leak into a capture, whatever the timing.
        // Dev-only escape hatch so UI screenshots of SharpShot itself can be taken (docs/testing).
        if (!AllowSelfCapture)
        {
            User32.SetWindowDisplayAffinity(hwnd, User32.WdaExcludeFromCapture);
        }
        window.HideRequested += (_, _) =>
        {
            _visible = false;
        };
        return window;
    }
}
