using Microsoft.UI.Dispatching;
using SharpShot.App.Services;
using SharpShot.Core.Interfaces;
using SharpShot.Core.Models;

namespace SharpShot.App.ViewModels;

public sealed class HomeViewModel : ObservableObject, IDisposable
{
    private readonly ICaptureCoordinator _coordinator;
    private readonly ISettingsService _settings;
    private readonly IScreenshotStorage _storage;
    private readonly ShellService _shell;
    private readonly IHotkeyService _hotkeys;
    private readonly DispatcherQueue _dispatcher;

    private string _hotkeyWarning = string.Empty;
    private bool _isHotkeyWarningOpen;
    private string _statusText = "Pronto para capturar";
    private CaptureStatusKind _statusKind = CaptureStatusKind.Ready;
    private string? _lastFilePath;
    private string _errorMessage = string.Empty;
    private bool _isErrorOpen;
    private HotkeyBinding _fullScreenHotkey = HotkeyBinding.DefaultFullScreen;
    private HotkeyBinding _regionHotkey = HotkeyBinding.DefaultRegion;
    private string _saveDirectory = string.Empty;

    public HomeViewModel(
        ICaptureCoordinator coordinator,
        ISettingsService settings,
        IScreenshotStorage storage,
        ShellService shell,
        IHotkeyService hotkeys,
        DispatcherQueue dispatcher)
    {
        _coordinator = coordinator;
        _settings = settings;
        _storage = storage;
        _shell = shell;
        _hotkeys = hotkeys;
        _dispatcher = dispatcher;

        CaptureFullScreenCommand = new RelayCommand(_coordinator.CaptureFullScreenAsync);
        CaptureRegionCommand = new RelayCommand(_coordinator.CaptureRegionAsync);
        OpenFolderCommand = new RelayCommand(() => _shell.OpenFolder(_storage.ResolveDirectory(_settings.Current.SaveDirectory)));
        OpenLastCaptureCommand = new RelayCommand(() =>
        {
            if (!string.IsNullOrEmpty(_lastFilePath))
            {
                _shell.OpenFile(_lastFilePath);
            }
        });
        DismissErrorCommand = new RelayCommand(() => IsErrorOpen = false);

        ApplySettings(_settings.Current);
        ApplyStatus(_coordinator.Status);
        _settings.Changed += OnSettingsChanged;
        _coordinator.StatusChanged += OnStatusChanged;
    }

    public RelayCommand CaptureFullScreenCommand { get; }
    public RelayCommand CaptureRegionCommand { get; }
    public RelayCommand OpenFolderCommand { get; }
    public RelayCommand OpenLastCaptureCommand { get; }
    public RelayCommand DismissErrorCommand { get; }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public CaptureStatusKind StatusKind
    {
        get => _statusKind;
        private set
        {
            if (SetProperty(ref _statusKind, value))
            {
                OnPropertyChanged(nameof(StatusGlyph));
                OnPropertyChanged(nameof(IsStatusSuccess));
                OnPropertyChanged(nameof(IsStatusFailure));
                OnPropertyChanged(nameof(IsStatusWorking));
            }
        }
    }

    public string StatusGlyph => StatusKind switch
    {
        CaptureStatusKind.Success => "",
        CaptureStatusKind.Failure => "",
        CaptureStatusKind.Working => "",
        CaptureStatusKind.Cancelled => "",
        _ => "",
    };

    public bool IsStatusSuccess => StatusKind is CaptureStatusKind.Success or CaptureStatusKind.Ready;
    public bool IsStatusFailure => StatusKind == CaptureStatusKind.Failure;
    public bool IsStatusWorking => StatusKind == CaptureStatusKind.Working;

    public bool HasLastCapture => !string.IsNullOrEmpty(_lastFilePath);

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsErrorOpen
    {
        get => _isErrorOpen;
        set => SetProperty(ref _isErrorOpen, value);
    }

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

    /// <summary>Resolved save directory shown in the "Salvar em" row.</summary>
    public string SaveDirectory
    {
        get => _saveDirectory;
        private set => SetProperty(ref _saveDirectory, value);
    }

    public string FormatText => "PNG (sem perda de qualidade)";

    /// <summary>Shown when a configured hotkey could not be registered (taken by another application).</summary>
    public string HotkeyWarning
    {
        get => _hotkeyWarning;
        private set => SetProperty(ref _hotkeyWarning, value);
    }

    public bool IsHotkeyWarningOpen
    {
        get => _isHotkeyWarningOpen;
        set => SetProperty(ref _isHotkeyWarningOpen, value);
    }

    private void RefreshHotkeyWarning(AppSettings settings)
    {
        var missing = new List<string>(2);
        if (_hotkeys.GetCurrent(Core.Enums.CaptureMode.FullScreen) is null)
        {
            missing.Add($"{settings.FullScreenHotkey.ToDisplayString()} (tela inteira)");
        }

        if (_hotkeys.GetCurrent(Core.Enums.CaptureMode.Region) is null)
        {
            missing.Add($"{settings.RegionHotkey.ToDisplayString()} (região)");
        }

        HotkeyWarning = missing.Count == 0
            ? string.Empty
            : $"Atalho em uso por outro aplicativo: {string.Join("; ", missing)}. Altere em Configurações › Atalhos.";
        IsHotkeyWarningOpen = missing.Count > 0;
    }

    public void Dispose()
    {
        _settings.Changed -= OnSettingsChanged;
        _coordinator.StatusChanged -= OnStatusChanged;
    }

    private void OnSettingsChanged(object? sender, AppSettings settings)
        => _dispatcher.TryEnqueue(() => ApplySettings(settings));

    private void OnStatusChanged(object? sender, CaptureStatus status)
    {
        if (_dispatcher.HasThreadAccess)
        {
            ApplyStatus(status);
        }
        else
        {
            _dispatcher.TryEnqueue(() => ApplyStatus(status));
        }
    }

    private void ApplySettings(AppSettings settings)
    {
        FullScreenHotkey = settings.FullScreenHotkey;
        RegionHotkey = settings.RegionHotkey;
        SaveDirectory = _storage.ResolveDirectory(settings.SaveDirectory);
        RefreshHotkeyWarning(settings);
    }

    private void ApplyStatus(CaptureStatus status)
    {
        StatusText = status.Message;
        StatusKind = status.Kind;
        if (status.Result is not null)
        {
            _lastFilePath = status.Result.FilePath;
            OnPropertyChanged(nameof(HasLastCapture));
        }

        if (status.Kind == CaptureStatusKind.Failure)
        {
            ErrorMessage = status.Message;
            IsErrorOpen = true;
        }
        else if (status.Kind == CaptureStatusKind.Working)
        {
            IsErrorOpen = false;
        }
    }
}
