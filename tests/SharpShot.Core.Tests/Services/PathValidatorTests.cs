using SharpShot.Core.Services;

namespace SharpShot.Core.Tests.Services;

public sealed class PathValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_IsInvalid(string? path)
    {
        var result = PathValidator.ValidateDirectory(path);

        Assert.False(result.IsValid);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Theory]
    [InlineData("C:\\a\0b")]
    [InlineData("C:\\a\tb")]
    [InlineData("C:\\a|b")]
    [InlineData("C:\\a?b")]
    [InlineData("C:\\a*b")]
    [InlineData("C:\\a<b")]
    [InlineData("C:\\a>b")]
    [InlineData("C:\\a\"b")]
    [InlineData("C:\\a:b")]
    [InlineData("C:\\ok\\bad?name\\x")]
    public void InvalidCharacters_AreRejected(string path)
    {
        Assert.False(PathValidator.ValidateDirectory(path).IsValid);
    }

    [Theory]
    [InlineData("Pictures\\SharpShot")]
    [InlineData(".\\shots")]
    [InlineData("..\\shots")]
    [InlineData("C:Pictures")] // drive-relative
    [InlineData("\\Pictures")] // rooted but not fully qualified
    [InlineData("shots")]
    public void RelativePaths_AreRejected(string path)
    {
        Assert.False(PathValidator.ValidateDirectory(path).IsValid);
    }

    [Theory]
    [InlineData("C:\\CON")]
    [InlineData("C:\\con")]
    [InlineData("C:\\Users\\COM1")]
    [InlineData("C:\\x\\NUL.txt")]
    [InlineData("C:\\LPT9\\y")]
    [InlineData("C:\\aux.tar.gz")]
    [InlineData("C:\\PRN")]
    public void ReservedNames_AreRejected(string path)
    {
        var result = PathValidator.ValidateDirectory(path);

        Assert.False(result.IsValid);
        Assert.Contains("reservado", result.Error);
    }

    [Theory]
    [InlineData("C:\\CONSOLE")]
    [InlineData("C:\\COM10")]
    [InlineData("C:\\NULL")]
    [InlineData("C:\\my.con")]
    public void NamesThatOnlyResembleReservedNames_AreAccepted(string path)
    {
        Assert.True(PathValidator.ValidateDirectory(path).IsValid);
    }

    [Theory]
    [InlineData("C:\\foo \\bar")]
    [InlineData("C:\\foo.\\bar")]
    [InlineData("C:\\foo ")]
    [InlineData("C:\\foo.")]
    public void TrailingSpaceOrDot_IsRejected(string path)
    {
        Assert.False(PathValidator.ValidateDirectory(path).IsValid);
    }

    [Theory]
    [InlineData("C:\\Users\\Me\\Pictures\\SharpShot")]
    [InlineData("C:\\Shots\\")]
    [InlineData("D:\\")]
    [InlineData("C:\\a.b\\c d")]
    [InlineData("C:/Users/Me/Pictures")]
    [InlineData("\\\\server\\share\\shots")]
    [InlineData("C:\\Imagens\\Capturas de tela")]
    public void ValidAbsolutePaths_AreAccepted(string path)
    {
        var result = PathValidator.ValidateDirectory(path);

        Assert.True(result.IsValid, result.Error);
        Assert.Null(result.Error);
    }

    [Fact]
    public void TempPath_IsAccepted()
    {
        Assert.True(PathValidator.ValidateDirectory(Path.Combine(Path.GetTempPath(), "SharpShot.Tests")).IsValid);
    }
}
