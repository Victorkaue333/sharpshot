using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Extensions.Logging.Abstractions;
using SharpShot.Core.Enums;
using SharpShot.Core.Exceptions;
using SharpShot.Core.Models;
using SharpShot.Storage.Services;

namespace SharpShot.Core.Tests.Storage;

public sealed class ScreenshotStorageServiceTests : IDisposable
{
    private const string FileName = "SharpShot_2026-01-02_03-04-05-006.png";

    private readonly TempDirectory _temp = new();
    private readonly ScreenshotStorageService _service = new(NullLogger<ScreenshotStorageService>.Instance);

    public void Dispose() => _temp.Dispose();

    private static byte[] SampleBytes(int length, int seed = 1)
    {
        var data = new byte[length];
        new Random(seed).NextBytes(data);
        return data;
    }

    [Fact]
    public void Constructor_RejectsNullLogger()
    {
        Assert.Throws<ArgumentNullException>(() => new ScreenshotStorageService(null!));
    }

    [Fact]
    public void DefaultDirectory_IsPicturesSharpShot()
    {
        Assert.EndsWith(Path.DirectorySeparatorChar + ScreenshotStorageService.DefaultSubfolderName, _service.DefaultDirectory, StringComparison.Ordinal);
        Assert.True(Path.IsPathFullyQualified(_service.DefaultDirectory));

        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures, Environment.SpecialFolderOption.DoNotVerify);
        if (!string.IsNullOrWhiteSpace(pictures))
        {
            Assert.Equal(Path.Combine(pictures, "SharpShot"), _service.DefaultDirectory);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveDirectory_BlankUsesDefault(string? configured)
    {
        Assert.Equal(_service.DefaultDirectory, _service.ResolveDirectory(configured));
    }

    [Fact]
    public void ResolveDirectory_ExpandsEnvironmentVariables()
    {
        var temp = Environment.GetEnvironmentVariable("TEMP");
        Assert.False(string.IsNullOrEmpty(temp));

        var resolved = _service.ResolveDirectory(@"%TEMP%\SharpShot.Tests\Shots");

        Assert.Equal(Path.GetFullPath(Path.Combine(temp, "SharpShot.Tests", "Shots")), resolved);
        Assert.DoesNotContain("%", resolved);
    }

    [Fact]
    public void ResolveDirectory_TrimsAndNormalizes()
    {
        Assert.Equal(@"C:\Pics", _service.ResolveDirectory(@"  C:\Shots\..\Pics  "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(@"%TEMP%\Shots")]
    [InlineData(@"C:\Users\Me\Pictures\SharpShot")]
    public void ValidateDirectory_AcceptsBlankAndAbsolutePaths(string? directory)
    {
        var result = _service.ValidateDirectory(directory);

        Assert.True(result.IsValid, result.Error);
    }

    [Theory]
    [InlineData("relative\\folder")]
    [InlineData(@"C:\CON")]
    [InlineData(@"C:\bad|name")]
    [InlineData(@"C:\trailing.\x")]
    public void ValidateDirectory_RejectsInvalidPaths(string directory)
    {
        Assert.False(_service.ValidateDirectory(directory).IsValid);
    }

    [Fact]
    public async Task SaveAsync_WritesExactBytesAndReturnsSize()
    {
        var data = SampleBytes(70_000);

        var saved = await _service.SaveAsync(data, _temp.Path, FileName);

        Assert.Equal(_temp.Combine(FileName), saved.FilePath);
        Assert.Equal(data.LongLength, saved.SizeBytes);
        Assert.Equal(data, await File.ReadAllBytesAsync(saved.FilePath));
    }

    [Fact]
    public async Task SaveAsync_CreatesMissingDirectory()
    {
        var directory = Path.Combine(_temp.Path, "a", "b c", "d");

        var saved = await _service.SaveAsync([1, 2, 3], directory, FileName);

        Assert.True(Directory.Exists(directory));
        Assert.Equal(Path.Combine(directory, FileName), saved.FilePath);
    }

    [Fact]
    public async Task SaveAsync_EmptyData_WritesEmptyFile()
    {
        var saved = await _service.SaveAsync([], _temp.Path, FileName);

        Assert.Equal(0, saved.SizeBytes);
        Assert.Equal(0, new FileInfo(saved.FilePath).Length);
    }

    [Fact]
    public async Task SaveAsync_NeverOverwrites_AppendsSuffix()
    {
        var first = SampleBytes(100, seed: 1);
        var second = SampleBytes(100, seed: 2);
        var third = SampleBytes(100, seed: 3);

        var a = await _service.SaveAsync(first, _temp.Path, FileName);
        var b = await _service.SaveAsync(second, _temp.Path, FileName);
        var c = await _service.SaveAsync(third, _temp.Path, FileName);

        Assert.Equal(_temp.Combine(FileName), a.FilePath);
        Assert.Equal(_temp.Combine("SharpShot_2026-01-02_03-04-05-006_1.png"), b.FilePath);
        Assert.Equal(_temp.Combine("SharpShot_2026-01-02_03-04-05-006_2.png"), c.FilePath);
        Assert.Equal(first, await File.ReadAllBytesAsync(a.FilePath));
        Assert.Equal(second, await File.ReadAllBytesAsync(b.FilePath));
        Assert.Equal(third, await File.ReadAllBytesAsync(c.FilePath));
    }

    [Theory]
    [InlineData("relative\\folder")]
    [InlineData(@"C:\CON\shots")]
    [InlineData(@"C:\bad?name")]
    [InlineData(@"C:\trailing \x")]
    public async Task SaveAsync_InvalidPath_ThrowsInvalidPath(string directory)
    {
        var ex = await Assert.ThrowsAsync<ScreenshotSaveException>(() => _service.SaveAsync([1], directory, FileName));

        Assert.Equal(SaveErrorKind.InvalidPath, ex.Kind);
    }

    [Fact]
    public async Task SaveAsync_ValidatesArguments()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SaveAsync(null!, _temp.Path, FileName));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveAsync([1], " ", FileName));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveAsync([1], _temp.Path, ""));
    }

    [Fact]
    public async Task SaveAsync_TargetLockedByAnotherHandle_UsesAnotherName()
    {
        var lockedPath = _temp.Combine(FileName);
        byte[] original = [9, 9, 9];
        await File.WriteAllBytesAsync(lockedPath, original);
        var data = SampleBytes(64);

        SavedScreenshot saved;
        using (new FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            saved = await _service.SaveAsync(data, _temp.Path, FileName);
        }

        Assert.NotEqual(lockedPath, saved.FilePath);
        Assert.Equal(data, await File.ReadAllBytesAsync(saved.FilePath));
        Assert.Equal(original, await File.ReadAllBytesAsync(lockedPath));
    }

    [Fact]
    public async Task SaveAsync_ReadOnlyExistingFile_IsNotTouched()
    {
        var existing = _temp.Combine(FileName);
        byte[] original = [1, 2, 3];
        await File.WriteAllBytesAsync(existing, original);
        File.SetAttributes(existing, FileAttributes.ReadOnly);

        var saved = await _service.SaveAsync([4, 5, 6], _temp.Path, FileName);

        Assert.Equal(_temp.Combine("SharpShot_2026-01-02_03-04-05-006_1.png"), saved.FilePath);
        Assert.Equal(original, await File.ReadAllBytesAsync(existing));
    }

    [Fact]
    public async Task SaveAsync_DirectoryPathIsAFile_ThrowsClassifiedException()
    {
        var notADirectory = _temp.Combine("file.bin");
        await File.WriteAllBytesAsync(notADirectory, [0]);

        await Assert.ThrowsAsync<ScreenshotSaveException>(() => _service.SaveAsync([1], notADirectory, FileName));
    }

    [Fact]
    public async Task SaveAsync_DirectoryDeniesFileCreation_ThrowsPermissionDenied()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // ACL scenario is Windows-only.
        }

        var directory = Directory.CreateDirectory(_temp.Combine("denied"));
        var user = WindowsIdentity.GetCurrent().User;
        Assert.NotNull(user);
        var deny = new FileSystemAccessRule(user, FileSystemRights.CreateFiles | FileSystemRights.WriteData, AccessControlType.Deny);

        var acl = directory.GetAccessControl();
        acl.AddAccessRule(deny);
        directory.SetAccessControl(acl);
        try
        {
            var ex = await Assert.ThrowsAsync<ScreenshotSaveException>(() => _service.SaveAsync([1, 2], directory.FullName, FileName));

            Assert.Equal(SaveErrorKind.PermissionDenied, ex.Kind);
            Assert.False(File.Exists(Path.Combine(directory.FullName, FileName)));
        }
        finally
        {
            var restore = directory.GetAccessControl();
            restore.RemoveAccessRule(deny);
            directory.SetAccessControl(restore);
        }
    }
}
