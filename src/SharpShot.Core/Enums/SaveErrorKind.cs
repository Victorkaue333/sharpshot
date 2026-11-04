namespace SharpShot.Core.Enums;

/// <summary>Classified reasons why a screenshot could not be written to disk.</summary>
public enum SaveErrorKind
{
    Unknown = 0,
    PermissionDenied,
    DiskFull,
    InvalidPath,
    DirectoryNotFound,
    FileLocked,
    EncodingFailed,
}
