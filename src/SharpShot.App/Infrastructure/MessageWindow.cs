using System.ComponentModel;
using System.Runtime.InteropServices;
using SharpShot.App.Interop;

namespace SharpShot.App.Infrastructure;

/// <summary>
/// A hidden top-level window owned by the UI thread. It receives WM_HOTKEY for global hotkeys, the
/// Shell_NotifyIcon callback for the tray icon and the TaskbarCreated broadcast, and re-dispatches them as .NET events.
/// The WndProc delegate is kept in a field so the GC never collects it while the window exists.
/// </summary>
internal sealed class MessageWindow : IDisposable
{
    private const string ClassName = "SharpShot.MessageWindow";
    private static ushort _classAtom;

    private readonly User32.WndProc _wndProc;
    private readonly uint _taskbarCreatedMessage;
    private bool _disposed;

    public MessageWindow()
    {
        _wndProc = WindowProc;
        var instance = User32.GetModuleHandleW(null);

        if (_classAtom == 0)
        {
            var wc = new User32.WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<User32.WNDCLASSEXW>(),
                lpfnWndProc = _wndProc,
                hInstance = instance,
                lpszClassName = ClassName,
            };
            _classAtom = User32.RegisterClassExW(ref wc);
            if (_classAtom == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "RegisterClassEx failed for the message window.");
            }
        }

        // A hidden top-level window (not HWND_MESSAGE): message-only windows never receive the
        // "TaskbarCreated" broadcast and cannot own a popup menu with keyboard focus.
        Handle = User32.CreateWindowExW(
            User32.WsExToolWindow, ClassName, "SharpShot", User32.WsPopup,
            0, 0, 0, 0, 0, 0, instance, 0);
        if (Handle == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateWindowEx failed for the message window.");
        }

        _taskbarCreatedMessage = User32.RegisterWindowMessageW("TaskbarCreated");
    }

    public nint Handle { get; private set; }

    /// <summary>Raised with the hotkey id passed to RegisterHotKey.</summary>
    public event Action<int>? HotkeyPressed;

    /// <summary>Raised for Shell_NotifyIcon callbacks: (notification code, x, y) with NOTIFYICON_VERSION_4 semantics.</summary>
    public event Action<int, int, int>? TrayIconMessage;

    /// <summary>Raised when Explorer restarts, so the tray icon can be re-added.</summary>
    public event Action? TaskbarCreated;

    private nint WindowProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        try
        {
            switch (msg)
            {
                case User32.WmHotkey:
                    HotkeyPressed?.Invoke((int)wParam);
                    return 0;

                case User32.WmTrayCallback:
                    // NOTIFYICON_VERSION_4: LOWORD(lParam) = event, HIWORD(lParam) = icon id, wParam = anchor x/y.
                    var notification = (int)(lParam & 0xFFFF);
                    var x = (short)(wParam & 0xFFFF);
                    var y = (short)((wParam >> 16) & 0xFFFF);
                    TrayIconMessage?.Invoke(notification, x, y);
                    return 0;

                default:
                    if (_taskbarCreatedMessage != 0 && msg == _taskbarCreatedMessage)
                    {
                        TaskbarCreated?.Invoke();
                        return 0;
                    }

                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MessageWindow handler failed: {ex}");
        }

        return User32.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (Handle != 0)
        {
            User32.DestroyWindow(Handle);
            Handle = 0;
        }

        GC.KeepAlive(_wndProc);
    }
}
