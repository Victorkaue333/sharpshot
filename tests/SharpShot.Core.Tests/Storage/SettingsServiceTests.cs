using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SharpShot.Core.Enums;
using SharpShot.Core.Models;
using SharpShot.Storage.Configuration;

namespace SharpShot.Core.Tests.Storage;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    private string SettingsPath => _temp.Combine(SettingsService.SettingsFileName);

    public void Dispose() => _temp.Dispose();

    private SettingsService CreateService(string? path = null)
        => new(NullLogger<SettingsService>.Instance, path ?? SettingsPath);

    private static void AssertDefaults(AppSettings settings)
    {
        var defaults = AppSettings.CreateDefault();
        Assert.Equal(defaults.SchemaVersion, settings.SchemaVersion);
        Assert.Equal(defaults.StartWithWindows, settings.StartWithWindows);
        Assert.Equal(defaults.MinimizeToTray, settings.MinimizeToTray);
        Assert.Equal(defaults.ShowNotificationAfterCapture, settings.ShowNotificationAfterCapture);
        Assert.Equal(defaults.FullScreenHotkey, settings.FullScreenHotkey);
        Assert.Equal(defaults.RegionHotkey, settings.RegionHotkey);
        Assert.Equal(defaults.SaveDirectory, settings.SaveDirectory);
        Assert.Equal(defaults.Theme, settings.Theme);
    }

    [Fact]
    public void Constructor_ExposesSettingsPath()
    {
        using var service = CreateService();

        Assert.Equal(SettingsPath, service.SettingsFilePath);
        AssertDefaults(service.Current);
    }

    [Fact]
    public void DefaultSettingsPath_IsUnderLocalAppDataSharpShot()
    {
        var path = SettingsService.GetDefaultSettingsPath();

        Assert.Equal(SettingsService.SettingsFileName, Path.GetFileName(path));
        Assert.Equal(SettingsService.ApplicationFolderName, Path.GetFileName(Path.GetDirectoryName(path)));
    }

    [Fact]
    public async Task LoadAsync_WithoutFile_UsesDefaults()
    {
        using var service = CreateService();

        await service.LoadAsync();

        AssertDefaults(service.Current);
        Assert.False(File.Exists(SettingsPath));
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsAllFields()
    {
        var saveDirectory = _temp.Combine("Shots");
        var settings = new AppSettings
        {
            StartWithWindows = true,
            MinimizeToTray = false,
            ShowNotificationAfterCapture = false,
            FullScreenHotkey = new HotkeyBinding(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x74), // Ctrl+Alt+F5
            RegionHotkey = new HotkeyBinding(HotkeyModifiers.Windows | HotkeyModifiers.Shift, 0x53), // Shift+Win+S
            SaveDirectory = saveDirectory,
            Theme = ThemeMode.Dark,
        };

        using (var writer = CreateService())
        {
            await writer.SaveAsync(settings);
        }

        using var reader = CreateService();
        await reader.LoadAsync();
        var loaded = reader.Current;

        Assert.Equal(AppSettings.CurrentSchemaVersion, loaded.SchemaVersion);
        Assert.True(loaded.StartWithWindows);
        Assert.False(loaded.MinimizeToTray);
        Assert.False(loaded.ShowNotificationAfterCapture);
        Assert.Equal(settings.FullScreenHotkey, loaded.FullScreenHotkey);
        Assert.Equal(settings.RegionHotkey, loaded.RegionHotkey);
        Assert.Equal(saveDirectory, loaded.SaveDirectory);
        Assert.Equal(ThemeMode.Dark, loaded.Theme);
    }

    [Fact]
    public async Task SaveAsync_WritesHotkeysAsStrings()
    {
        using var service = CreateService();

        await service.SaveAsync(AppSettings.CreateDefault());

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(SettingsPath));
        var root = document.RootElement;
        Assert.Equal("Ctrl+Shift+1", root.GetProperty("fullScreenHotkey").GetString());
        Assert.Equal("Ctrl+Shift+2", root.GetProperty("regionHotkey").GetString());
        Assert.Equal(JsonValueKind.String, root.GetProperty("theme").ValueKind);
        Assert.Equal(AppSettings.CurrentSchemaVersion, root.GetProperty("schemaVersion").GetInt32());
    }

    [Fact]
    public async Task SaveAsync_CreatesMissingDirectory()
    {
        var nested = Path.Combine(_temp.Path, "a", "b", SettingsService.SettingsFileName);
        using var service = CreateService(nested);

        await service.SaveAsync(AppSettings.CreateDefault());

        Assert.True(File.Exists(nested));
    }

    [Fact]
    public async Task SaveAsync_UpdatesCurrentWithIndependentSnapshot()
    {
        using var service = CreateService();
        var settings = AppSettings.CreateDefault();
        settings.Theme = ThemeMode.Light;

        await service.SaveAsync(settings);
        settings.Theme = ThemeMode.Dark; // mutating the caller's object must not leak into Current

        Assert.NotSame(settings, service.Current);
        Assert.Equal(ThemeMode.Light, service.Current.Theme);
    }

    [Fact]
    public async Task SaveAsync_RaisesChangedWithSavedSnapshot()
    {
        using var service = CreateService();
        var raised = new List<(object? Sender, AppSettings Args)>();
        service.Changed += (sender, args) => raised.Add((sender, args));
        var settings = AppSettings.CreateDefault();
        settings.StartWithWindows = true;

        await service.SaveAsync(settings);

        var (sender, args) = Assert.Single(raised);
        Assert.Same(service, sender);
        Assert.Same(service.Current, args);
        Assert.True(args.StartWithWindows);
    }

    [Fact]
    public async Task SaveAsync_IsAtomic_LeavesNoTempFile()
    {
        using var service = CreateService();

        await service.SaveAsync(AppSettings.CreateDefault());
        var updated = AppSettings.CreateDefault();
        updated.Theme = ThemeMode.Dark;
        await service.SaveAsync(updated); // replaces an existing file

        Assert.False(File.Exists(SettingsPath + ".tmp"));
        Assert.Equal([SettingsService.SettingsFileName], Directory.GetFiles(_temp.Path).Select(Path.GetFileName));

        using var reader = CreateService();
        await reader.LoadAsync();
        Assert.Equal(ThemeMode.Dark, reader.Current.Theme);
    }

    [Fact]
    public async Task SaveAsync_NormalizesInvalidValues()
    {
        using var service = CreateService();
        var settings = AppSettings.CreateDefault();
        settings.SchemaVersion = 99;
        settings.SaveDirectory = "   ";
        settings.FullScreenHotkey = new HotkeyBinding(HotkeyModifiers.None, 0x41); // no modifier
        settings.RegionHotkey = new HotkeyBinding(HotkeyModifiers.Control, 0x10); // modifier as main key

        await service.SaveAsync(settings);

        Assert.Equal(AppSettings.CurrentSchemaVersion, service.Current.SchemaVersion);
        Assert.Null(service.Current.SaveDirectory);
        Assert.Equal(HotkeyBinding.DefaultFullScreen, service.Current.FullScreenHotkey);
        Assert.Equal(HotkeyBinding.DefaultRegion, service.Current.RegionHotkey);
    }

    [Fact]
    public async Task SaveAsync_RejectsNull()
    {
        using var service = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SaveAsync(null!));
    }

    [Fact]
    public async Task LoadAsync_CorruptJson_RestoresDefaultsAndBacksUpFile()
    {
        const string corrupt = "{ this is not json";
        await File.WriteAllTextAsync(SettingsPath, corrupt);
        using var service = CreateService();

        await service.LoadAsync();

        AssertDefaults(service.Current);
        Assert.False(File.Exists(SettingsPath));
        var backup = Assert.Single(Directory.GetFiles(_temp.Path, SettingsService.SettingsFileName + ".corrupt-*"));
        Assert.Equal(corrupt, await File.ReadAllTextAsync(backup));
    }

    [Fact]
    public async Task LoadAsync_NullHotkey_IsNormalizedToDefault_OtherFieldsKept()
    {
        await File.WriteAllTextAsync(SettingsPath, """
            {
              "schemaVersion": 1,
              "startWithWindows": true,
              "fullScreenHotkey": null,
              "regionHotkey": "Alt+F2",
              "theme": "Light"
            }
            """);
        using var service = CreateService();

        await service.LoadAsync();

        Assert.Equal(HotkeyBinding.DefaultFullScreen, service.Current.FullScreenHotkey);
        Assert.Equal(new HotkeyBinding(HotkeyModifiers.Alt, 0x71), service.Current.RegionHotkey);
        Assert.True(service.Current.StartWithWindows);
        Assert.Equal(ThemeMode.Light, service.Current.Theme);
    }

    [Theory]
    [InlineData("Ctrl+Banana")]
    [InlineData("A")] // no modifier
    [InlineData("Ctrl+Shift")] // no main key
    public async Task LoadAsync_InvalidHotkey_FallsBackToDefaultHotkey(string hotkey)
    {
        await File.WriteAllTextAsync(SettingsPath, $$"""
            { "fullScreenHotkey": "{{hotkey}}", "regionHotkey": "Ctrl+Shift+2" }
            """);
        using var service = CreateService();

        await service.LoadAsync();

        Assert.Equal(HotkeyBinding.DefaultFullScreen, service.Current.FullScreenHotkey);
        Assert.Equal(HotkeyBinding.DefaultRegion, service.Current.RegionHotkey);
    }

    [Fact]
    public async Task LoadAsync_InvalidHotkey_KeepsOtherFields()
    {
        await File.WriteAllTextAsync(SettingsPath, """
            { "startWithWindows": true, "theme": "Dark", "fullScreenHotkey": "Ctrl+Banana" }
            """);
        using var service = CreateService();

        await service.LoadAsync();

        Assert.Equal(HotkeyBinding.DefaultFullScreen, service.Current.FullScreenHotkey);
        Assert.True(service.Current.StartWithWindows);
        Assert.Equal(ThemeMode.Dark, service.Current.Theme);
        Assert.True(File.Exists(SettingsPath));
    }

    [Fact]
    public async Task LoadAsync_BlankSaveDirectory_IsNormalizedToNull()
    {
        await File.WriteAllTextAsync(SettingsPath, """{ "saveDirectory": "  " }""");
        using var service = CreateService();

        await service.LoadAsync();

        Assert.Null(service.Current.SaveDirectory);
    }

    [Fact]
    public async Task LoadAsync_AcceptsCommentsAndTrailingCommas()
    {
        await File.WriteAllTextAsync(SettingsPath, """
            {
              // user edited this file by hand
              "minimizeToTray": false,
              "theme": "Dark",
            }
            """);
        using var service = CreateService();

        await service.LoadAsync();

        Assert.False(service.Current.MinimizeToTray);
        Assert.Equal(ThemeMode.Dark, service.Current.Theme);
        Assert.Empty(Directory.GetFiles(_temp.Path, "*.corrupt-*"));
    }

    [Fact]
    public async Task LoadAsync_JsonNullDocument_UsesDefaults()
    {
        await File.WriteAllTextAsync(SettingsPath, "null");
        using var service = CreateService();

        await service.LoadAsync();

        AssertDefaults(service.Current);
    }
}
