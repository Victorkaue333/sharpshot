namespace SharpShot.Core.Enums;

/// <summary>Classified reasons why the capture engine could not produce a frame.</summary>
public enum CaptureErrorKind
{
    Unknown = 0,
    NotSupported,
    AccessDenied,
    DeviceFailure,
    Timeout,
    NoMonitor,
    Cancelled,
}
