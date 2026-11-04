using SharpShot.Core.Enums;

namespace SharpShot.Core.Exceptions;

/// <summary>Raised by the capture engine when a frame cannot be produced.</summary>
public sealed class CaptureException : Exception
{
    public CaptureException(CaptureErrorKind kind, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
    }

    public CaptureErrorKind Kind { get; }
}

/// <summary>Raised by the storage layer when a screenshot cannot be written.</summary>
public sealed class ScreenshotSaveException : Exception
{
    public ScreenshotSaveException(SaveErrorKind kind, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
    }

    public SaveErrorKind Kind { get; }
}
