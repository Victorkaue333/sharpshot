using System.Globalization;
using SharpShot.Core.Services;

namespace SharpShot.Core.Tests.Services;

public sealed class FileNamingTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 3, 7, 9, 5, 4, 123, TimeSpan.FromHours(-3));

    [Theory]
    [InlineData(".png")]
    [InlineData("png")]
    public void CreateFileName_UsesInvariantTimestampFormat(string extension)
    {
        Assert.Equal("SharpShot_2026-03-07_09-05-04-123.png", FileNaming.CreateFileName(Timestamp, extension));
    }

    [Theory]
    [InlineData("fa-IR")] // Persian calendar
    [InlineData("th-TH")] // Buddhist calendar
    [InlineData("ar-SA")] // Hijri calendar, Arabic digits
    [InlineData("pt-BR")]
    public void CreateFileName_IgnoresCurrentCulture(string cultureName)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);

            Assert.Equal("SharpShot_2026-03-07_09-05-04-123.png", FileNaming.CreateFileName(Timestamp, ".png"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void CreateFileName_Uses24HourClockAndZeroPadding()
    {
        var evening = new DateTimeOffset(2026, 12, 31, 23, 59, 58, 7, TimeSpan.Zero);

        Assert.Equal("SharpShot_2026-12-31_23-59-58-007.png", FileNaming.CreateFileName(evening, ".png"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateFileName_RejectsBlankExtension(string extension)
    {
        Assert.Throws<ArgumentException>(() => FileNaming.CreateFileName(Timestamp, extension));
    }

    [Fact]
    public void CreateFileName_RejectsNullExtension()
    {
        Assert.Throws<ArgumentNullException>(() => FileNaming.CreateFileName(Timestamp, null!));
    }

    [Fact]
    public void MakeUnique_ReturnsOriginalWhenFree()
    {
        var checkedPaths = new List<string>();

        var result = FileNaming.MakeUnique(@"C:\Shots", "a.png", p =>
        {
            checkedPaths.Add(p);
            return false;
        });

        Assert.Equal("a.png", result);
        Assert.Equal([Path.Combine(@"C:\Shots", "a.png")], checkedPaths);
    }

    [Fact]
    public void MakeUnique_AppendsIncrementingSuffixBeforeExtension()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine(@"C:\Shots", "a.png"),
        };

        Assert.Equal("a_1.png", FileNaming.MakeUnique(@"C:\Shots", "a.png", existing.Contains));

        existing.Add(Path.Combine(@"C:\Shots", "a_1.png"));
        existing.Add(Path.Combine(@"C:\Shots", "a_2.png"));

        Assert.Equal("a_3.png", FileNaming.MakeUnique(@"C:\Shots", "a.png", existing.Contains));
    }

    [Fact]
    public void MakeUnique_ThrowsAfterMaxUniqueAttempts()
    {
        var calls = 0;

        var ex = Assert.Throws<IOException>(() => FileNaming.MakeUnique(@"C:\Shots", "a.png", _ =>
        {
            calls++;
            return true;
        }));

        Assert.Contains("a.png", ex.Message);
        Assert.Equal(FileNaming.MaxUniqueAttempts + 1, calls);
    }

    [Fact]
    public void MakeUnique_LastAttemptIsUsedWhenFree()
    {
        var last = $"a_{FileNaming.MaxUniqueAttempts}.png";

        var result = FileNaming.MakeUnique(@"C:\Shots", "a.png", p => !p.EndsWith(last, StringComparison.Ordinal));

        Assert.Equal(last, result);
    }

    [Fact]
    public void MakeUnique_ValidatesArguments()
    {
        Assert.Throws<ArgumentNullException>(() => FileNaming.MakeUnique(null!, "a.png", _ => false));
        Assert.Throws<ArgumentException>(() => FileNaming.MakeUnique(@"C:\Shots", " ", _ => false));
        Assert.Throws<ArgumentNullException>(() => FileNaming.MakeUnique(@"C:\Shots", "a.png", null!));
    }
}
