using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SharpShot.App.Infrastructure;
using SharpShot.App.Interop;

namespace SharpShot.App.Services;

/// <summary>Actions available from the tray icon context menu.</summary>
public enum TrayAction
{
    None = 0,
    CaptureFullScreen = 1,
    CaptureRegion = 2,
    OpenWindow = 3,
    OpenFolder = 4,
    Exit = 5,
}

/// <summary>
/// System tray icon through Shell_NotifyIcon with a native Win32 popup menu. Left click opens the window,
/// right click shows the menu. The icon is re-added when Explorer restarts (TaskbarCreated).
/// </summary>
internal sealed class TrayIconService : IDisposable
{
    private const uint IconId = 1;
    private const string Tooltip = "SharpShot — Capturas de tela";

    private readonly MessageWindow _messageWindow;
    private readonly ILogger<TrayIconService> _logger;
    private readonly string _iconPath;
    private nint _icon;
    private bool _added;
    private bool _disposed;

    public TrayIconService(MessageWindow messageWindow, ILogger<TrayIconService> logger)
    {
        ArgumentNullException.ThrowIfNull(messageWindow);
        ArgumentNullException.ThrowIfNull(logger);
        _messageWindow = messageWindow;
        _logger = logger;
        _iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Tray.ico");
        _messageWindow.TrayIconMessage += OnTrayMessage;
        _messageWindow.TaskbarCreated += OnTaskbarCreated;
    }

    public event EventHandler<TrayAction>? ActionRequested;

    public void Show()
    {
        if (_added)
        {
            return;
        }

        EnsureIcon();
        var data = CreateData();
        data.uFlags = Shell32.NifMessage | Shell32.NifIcon | Shell32.NifTip | Shell32.NifShowTip;
        data.uCallbackMessage = User32.WmTrayCallback;
        data.hIcon = _icon;
        data.szTip = Tooltip;

        if (!Shell32.Shell_NotifyIconW(Shell32.NimAdd, ref data))
        {
            _logger.LogWarning("Shell_NotifyIcon(NIM_ADD) failed: {Error}.", new Win32Exception(Marshal.GetLastWin32Error()).Message);
            return;
        }

        data.uVersion = Shell32.NotifyIconVersion4;
        Shell32.Shell_NotifyIconW(Shell32.NimSetVersion, ref data);
        _added = true;
        _logger.LogInformation("Tray icon added.");
    }

    public void Hide()
    {
        if (!_added)
        {
            return;
        }

        var data = CreateData();
        Shell32.Shell_NotifyIconW(Shell32.NimDelete, ref data);
        _added = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _messageWindow.TrayIconMessage -= OnTrayMessage;
        _messageWindow.TaskbarCreated -= OnTaskbarCreated;
        Hide();
        if (_icon != 0)
        {
            User32.DestroyIcon(_icon);
            _icon = 0;
        }
    }

    private void OnTaskbarCreated()
    {
        _added = false;
        Show();
    }

    private void OnTrayMessage(int notification, int x, int y)
    {
        switch (notification)
        {
            case User32.WmLButtonUp:
            case User32.WmLButtonDblClk:
            case User32.NinSelect:
            case User32.NinKeySelect:
                ActionRequested?.Invoke(this, TrayAction.OpenWindow);
                break;

            case User32.WmContextMenu:
            case User32.WmRButtonUp:
                ShowContextMenu(x, y);
                break;
        }
    }

    private void ShowContextMenu(int x, int y)
    {
        var menu = User32.CreatePopupMenu();
        if (menu == 0)
        {
            return;
        }

        try
        {
            User32.AppendMenuW(menu, User32.MfString, (nuint)TrayAction.CaptureFullScreen, "Capturar tela inteira");
            User32.AppendMenuW(menu, User32.MfString, (nuint)TrayAction.CaptureRegion, "Selecionar região");
            User32.AppendMenuW(menu, User32.MfSeparator, 0, null);
            User32.AppendMenuW(menu, User32.MfString, (nuint)TrayAction.OpenWindow, "Abrir SharpShot");
            User32.AppendMenuW(menu, User32.MfString, (nuint)TrayAction.OpenFolder, "Abrir pasta de capturas");
            User32.AppendMenuW(menu, User32.MfSeparator, 0, null);
            User32.AppendMenuW(menu, User32.MfString, (nuint)TrayAction.Exit, "Sair");

            if (x == 0 && y == 0 && User32.GetCursorPos(out var point))
            {
                x = point.X;
                y = point.Y;
            }

            // Required so the menu closes when the user clicks elsewhere (KB 135788).
            User32.SetForegroundWindow(_messageWindow.Handle);
            var command = User32.TrackPopupMenuEx(menu, User32.TpmLeftAlign | User32.TpmRightButton | User32.TpmReturnCmd | User32.TpmNoNotify, x, y, _messageWindow.Handle, 0);
            User32.PostMessageW(_messageWindow.Handle, 0, 0, 0);

            if (command != 0 && Enum.IsDefined(typeof(TrayAction), command))
            {
                ActionRequested?.Invoke(this, (TrayAction)command);
            }
        }
        finally
        {
            User32.DestroyMenu(menu);
        }
    }

    private void EnsureIcon()
    {
        if (_icon != 0)
        {
            return;
        }

        var cx = User32.GetSystemMetrics(User32.SmCxSmIcon);
        var cy = User32.GetSystemMetrics(User32.SmCySmIcon);
        if (File.Exists(_iconPath))
        {
            _icon = User32.LoadImageW(0, _iconPath, User32.ImageIcon, cx, cy, User32.LrLoadFromFile | User32.LrDefaultColor);
        }

        if (_icon == 0)
        {
            _logger.LogWarning("Tray icon file not found or failed to load: {Path}.", _iconPath);
        }
    }

    private Shell32.NOTIFYICONDATAW CreateData() => new()
    {
        cbSize = (uint)Marshal.SizeOf<Shell32.NOTIFYICONDATAW>(),
        hWnd = _messageWindow.Handle,
        uID = IconId,
        szTip = string.Empty,
        szInfo = string.Empty,
        szInfoTitle = string.Empty,
    };
}
