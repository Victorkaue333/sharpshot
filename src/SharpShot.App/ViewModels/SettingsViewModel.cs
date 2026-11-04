using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using SharpShot.App.Services;
using SharpShot.Core.Enums;
using SharpShot.Core.Interfaces;
using SharpShot.Core.Models;
using SharpShot.Core.Services;

namespace SharpShot.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IHotkeyService _hotkeys;
    private readonly IStartupService _startup;
    private readonly IScreenshotStorage _storage;
    private readonly ShellService _shell;
    private readonly ILogger<SettingsViewModel> _logger;

    private bool _startWithWindows;
    private bool _minimizeToTray;
    private bool _showNotification;
    private int _themeIndex;
    private string _saveDirectoryInput = string.Empty;
    private HotkeyBinding _fullScreenHotkey = HotkeyBinding.DefaultFullScreen;
    private HotkeyBinding _regionHotkey = HotkeyBinding.DefaultRegion;
    private string _infoMessage = string.Empty;
    private bool _isInfoOpen;
    private InfoBarSeverity _infoSeverity = InfoBarSeverity.Informational;
    private bool _suppressSave;

    public SettingsViewModel(
        ISettingsService settings,
        IHotkeyService hotkeys,
        IStartupService startup,
        IScreenshotStorage storage,
        ShellService shell,
        ILogger<SettingsViewModel> logger)
    {
        _settings = settings;
        _hotkeys = hotkeys;
        _startup = startup;
        _storage = storage;
        _shell = shell;
        _logger = logger;

        ApplyDirectoryCommand = new RelayCommand(ApplyDirectoryAsync);
        ResetDirectoryCommand = new RelayCommand(ResetDirectoryAsync);
        OpenFolderCommand = new RelayCommand(() => _shell.OpenFolder(_storage.ResolveDirectory(_settings.Current.SaveDirectory)));
        RestoreDefaultHotkeysCommand = new RelayCommand(RestoreDefaultHotkeysAsync);

        Load(_settings.Current);
    }

    public RelayCommand ApplyDirectoryCommand { get; }
    public RelayCommand ResetDirectoryCommand { get; }
    public RelayCommand OpenFolderCommand { get; }
    public RelayCommand RestoreDefaultHotkeysCommand { get; }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (SetProperty(ref _startWithWindows, value))
            {
                _ = SaveAsync(s =>
                {
                    s.StartWithWindows = value;
                    if (!_startup.SetEnabled(value))
                    {
                        ShowInfo("Não foi possível alterar a inicialização com o Windows.", InfoBarSeverity.Warning);
                    }
                });
            }
        }
    }

    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set
        {
            if (SetProperty(ref _minimizeToTray, value))
            {
                _ = SaveAsync(s => s.MinimizeToTray = value);
            }
        }
    }

    public bool ShowNotification
    {
        get => _showNotification;
        set
        {
            if (SetProperty(ref _showNotification, value))
            {
                _ = SaveAsync(s => s.ShowNotificationAfterCapture = value);
            }
        }
    }

    /// <summary>0 = seguir o sistema, 1 = claro, 2 = escuro (matches the ComboBox order).</summary>
    public int ThemeIndex
    {
        get => _themeIndex;
        set
        {
            if (SetProperty(ref _themeIndex, value))
            {
                _ = SaveAsync(s => s.Theme = (ThemeMode)Math.Clamp(value, 0, 2));
            }
        }
    }

    public string SaveDirectoryInput
    {
        get => _saveDirectoryInput;
        set => SetProperty(ref _saveDirectoryInput, value);
    }

    public string DefaultDirectory => _storage.DefaultDirectory;

    public string ResolvedDirectory => _storage.ResolveDirectory(_settings.Current.SaveDirectory);

    public string NamingExample => FileNaming.CreateFileName(DateTimeOffset.Now, ".png");

    public string NamingPattern => $"{FileNaming.Prefix}_AAAA-MM-DD_HH-mm-ss-fff.png";

    public HotkeyBinding FullScreenHotkey
    {
        get => _fullScreenHotkey;
        private set => SetProperty(ref _fullScreenHotkey, value);
    }

    public HotkeyBinding RegionHotkey
    {
        get => _regionHotkey;
        private set => SetProperty(ref _regionHotkey, value);
    }

    public string InfoMessage
    {
        get => _infoMessage;
        private set => SetProperty(ref _infoMessage, value);
    }

    public bool IsInfoOpen
    {
        get => _isInfoOpen;
        set => SetProperty(ref _isInfoOpen, value);
    }

    public InfoBarSeverity InfoSeverity
    {
        get => _infoSeverity;
        private set => SetProperty(ref _infoSeverity, value);
    }

    /// <summary>Called by the hotkey editors. Registers first; only persists when Windows accepted the hotkey.</summary>
    public async Task SetHotkeyAsync(CaptureMode mode, HotkeyBinding binding)
    {
        var other = mode == CaptureMode.FullScreen ? RegionHotkey : FullScreenHotkey;
        if (binding == other)
        {
            ShowInfo("Esse atalho já está sendo usado pela outra ação.", InfoBarSeverity.Warning);
            RefreshHotkeys();
            return;
        }

        var previous = _hotkeys.GetCurrent(mode);
        var result = _hotkeys.Register(mode, binding);
        if (!result.Success)
        {
            ShowInfo(result.Error ?? "Não foi possível registrar o atalho.", result.Conflict ? InfoBarSeverity.Warning : InfoBarSeverity.Error);
            if (previous is not null)
            {
                _hotkeys.Register(mode, previous);
            }

            RefreshHotkeys();
            return;
        }

        await SaveAsync(s =>
        {
            if (mode == CaptureMode.FullScreen)
            {
                s.FullScreenHotkey = binding;
            }
            else
            {
                s.RegionHotkey = binding;
            }
        });
        RefreshHotkeys();
        ShowInfo($"Atalho atualizado: {binding.ToDisplayString()}.", InfoBarSeverity.Success);
    }

    private async Task RestoreDefaultHotkeysAsync()
    {
        await SetHotkeyAsync(CaptureMode.FullScreen, HotkeyBinding.DefaultFullScreen);
        await SetHotkeyAsync(CaptureMode.Region, HotkeyBinding.DefaultRegion);
    }

    private async Task ApplyDirectoryAsync()
    {
        var input = SaveDirectoryInput.Trim();
        var validation = _storage.ValidateDirectory(input);
        if (!validation.IsValid)
        {
            ShowInfo(validation.Error ?? "Pasta inválida.", InfoBarSeverity.Error);
            return;
        }

        var resolved = _storage.ResolveDirectory(input);
        try
        {
            Directory.CreateDirectory(resolved);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _logger.LogWarning(ex, "Directory {Directory} could not be created.", resolved);
            ShowInfo("Não foi possível criar ou acessar essa pasta. Verifique o caminho e as permissões.", InfoBarSeverity.Error);
            return;
        }

        var value = string.IsNullOrWhiteSpace(input) || string.Equals(resolved, _storage.DefaultDirectory, StringComparison.OrdinalIgnoreCase)
            ? null
            : input;
        await SaveAsync(s => s.SaveDirectory = value);
        SaveDirectoryInput = resolved;
        OnPropertyChanged(nameof(ResolvedDirectory));
        ShowInfo("Pasta de salvamento atualizada.", InfoBarSeverity.Success);
    }

    private async Task ResetDirectoryAsync()
    {
        await SaveAsync(s => s.SaveDirectory = null);
        SaveDirectoryInput = _storage.DefaultDirectory;
        OnPropertyChanged(nameof(ResolvedDirectory));
        ShowInfo("Pasta padrão restaurada.", InfoBarSeverity.Success);
    }

    private void Load(AppSettings settings)
    {
        _suppressSave = true;
        try
        {
            // The Run key is the truth (the installer can create it too); the JSON flag is a fallback.
            StartWithWindows = _startup.IsEnabled() || settings.StartWithWindows;
            MinimizeToTray = settings.MinimizeToTray;
            ShowNotification = settings.ShowNotificationAfterCapture;
            ThemeIndex = (int)settings.Theme;
            SaveDirectoryInput = _storage.ResolveDirectory(settings.SaveDirectory);
            RefreshHotkeys();
        }
        finally
        {
            _suppressSave = false;
        }
    }

    private void RefreshHotkeys()
    {
        FullScreenHotkey = _hotkeys.GetCurrent(CaptureMode.FullScreen) ?? _settings.Current.FullScreenHotkey;
        RegionHotkey = _hotkeys.GetCurrent(CaptureMode.Region) ?? _settings.Current.RegionHotkey;
    }

    private async Task SaveAsync(Action<AppSettings> mutate)
    {
        if (_suppressSave)
        {
            return;
        }

        try
        {
            var snapshot = _settings.Current.Clone();
            mutate(snapshot);
            await _settings.SaveAsync(snapshot);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to save settings.");
            ShowInfo("Não foi possível salvar as configurações.", InfoBarSeverity.Error);
        }
    }

    private void ShowInfo(string message, InfoBarSeverity severity)
    {
        InfoMessage = message;
        InfoSeverity = severity;
        IsInfoOpen = true;
    }
}
