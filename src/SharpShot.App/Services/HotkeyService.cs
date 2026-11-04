using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SharpShot.App.Infrastructure;
using SharpShot.App.Interop;
using SharpShot.Core.Enums;
using SharpShot.Core.Models;

namespace SharpShot.App.Services;

/// <summary>Result of a hotkey registration attempt.</summary>
public sealed record HotkeyRegistrationResult(bool Success, bool Conflict, string? Error)
{
    public static HotkeyRegistrationResult Ok { get; } = new(true, false, null);
}

public interface IHotkeyService
{
    /// <summary>Raised on the UI thread when a registered hotkey is pressed.</summary>
    event EventHandler<CaptureMode>? HotkeyPressed;

    HotkeyRegistrationResult Register(CaptureMode mode, HotkeyBinding binding);

    void Unregister(CaptureMode mode);

    /// <summary>Registers both bindings from settings; returns one result per mode.</summary>
    IReadOnlyDictionary<CaptureMode, HotkeyRegistrationResult> ApplySettings(AppSettings settings);

    HotkeyBinding? GetCurrent(CaptureMode mode);
}

/// <summary>Global hotkeys through RegisterHotKey on the hidden message window (no keyboard hooks).</summary>
internal sealed class HotkeyService : IHotkeyService, IDisposable
{
    private readonly MessageWindow _messageWindow;
    private readonly ILogger<HotkeyService> _logger;
    private readonly Dictionary<CaptureMode, HotkeyBinding> _registered = new();
    private bool _disposed;

    public HotkeyService(MessageWindow messageWindow, ILogger<HotkeyService> logger)
    {
        ArgumentNullException.ThrowIfNull(messageWindow);
        ArgumentNullException.ThrowIfNull(logger);
        _messageWindow = messageWindow;
        _logger = logger;
        _messageWindow.HotkeyPressed += OnHotkeyPressed;
    }

    public event EventHandler<CaptureMode>? HotkeyPressed;

    public HotkeyRegistrationResult Register(CaptureMode mode, HotkeyBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!binding.IsValid)
        {
            return new HotkeyRegistrationResult(false, false, "O atalho precisa de pelo menos um modificador (Ctrl, Shift, Alt ou Win) e uma tecla.");
        }

        Unregister(mode);

        var id = ToId(mode);
        var modifiers = (uint)binding.Modifiers | User32.ModNoRepeat;
        if (User32.RegisterHotKey(_messageWindow.Handle, id, modifiers, binding.VirtualKey))
        {
            _registered[mode] = binding;
            _logger.LogInformation("Hotkey {Binding} registered for {Mode}.", binding, mode);
            return HotkeyRegistrationResult.Ok;
        }

        var error = Marshal.GetLastWin32Error();
        if (error == User32.ErrorHotkeyAlreadyRegistered)
        {
            _logger.LogWarning("Hotkey {Binding} is already registered by another application.", binding);
            return new HotkeyRegistrationResult(false, true, $"O atalho {binding.ToDisplayString()} já está em uso por outro aplicativo.");
        }

        var message = new Win32Exception(error).Message;
        _logger.LogError("RegisterHotKey failed for {Binding}: {Error} ({Code}).", binding, message, error);
        return new HotkeyRegistrationResult(false, false, $"Não foi possível registrar {binding.ToDisplayString()}: {message}");
    }

    public void Unregister(CaptureMode mode)
    {
        if (!_registered.Remove(mode, out var binding))
        {
            return;
        }

        if (!User32.UnregisterHotKey(_messageWindow.Handle, ToId(mode)))
        {
            _logger.LogWarning("UnregisterHotKey failed for {Binding} ({Error}).", binding, Marshal.GetLastWin32Error());
        }
    }

    public IReadOnlyDictionary<CaptureMode, HotkeyRegistrationResult> ApplySettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new Dictionary<CaptureMode, HotkeyRegistrationResult>
        {
            [CaptureMode.FullScreen] = Register(CaptureMode.FullScreen, settings.FullScreenHotkey),
            [CaptureMode.Region] = Register(CaptureMode.Region, settings.RegionHotkey),
        };
    }

    public HotkeyBinding? GetCurrent(CaptureMode mode) => _registered.GetValueOrDefault(mode);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _messageWindow.HotkeyPressed -= OnHotkeyPressed;
        foreach (var mode in _registered.Keys.ToArray())
        {
            Unregister(mode);
        }
    }

    private void OnHotkeyPressed(int id)
    {
        var mode = FromId(id);
        if (mode is null || !_registered.ContainsKey(mode.Value))
        {
            return;
        }

        HotkeyPressed?.Invoke(this, mode.Value);
    }

    private static int ToId(CaptureMode mode) => 0x5500 + (int)mode;

    private static CaptureMode? FromId(int id)
    {
        var value = id - 0x5500;
        return Enum.IsDefined(typeof(CaptureMode), value) ? (CaptureMode)value : null;
    }
}
