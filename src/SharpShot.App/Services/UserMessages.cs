using SharpShot.Core.Enums;
using SharpShot.Core.Exceptions;

namespace SharpShot.App.Services;

/// <summary>Maps engine/storage failures to the pt-BR messages shown to the user.</summary>
internal static class UserMessages
{
    public static string ForException(Exception exception) => exception switch
    {
        CaptureException c => ForCapture(c.Kind),
        ScreenshotSaveException s => ForSave(s.Kind),
        OperationCanceledException => "A captura foi cancelada.",
        _ => "Ocorreu um erro inesperado. Consulte o log em %LOCALAPPDATA%\\SharpShot\\logs.",
    };

    public static string ForCapture(CaptureErrorKind kind) => kind switch
    {
        CaptureErrorKind.NotSupported => "A captura de tela não é suportada nesta versão do Windows (é necessário Windows 10 2004 ou superior).",
        CaptureErrorKind.AccessDenied => "O Windows negou o acesso à captura de tela.",
        CaptureErrorKind.DeviceFailure => "Falha no dispositivo gráfico. Tente novamente.",
        CaptureErrorKind.Timeout => "A tela não respondeu a tempo. Tente novamente.",
        CaptureErrorKind.NoMonitor => "Nenhum monitor ativo foi encontrado.",
        CaptureErrorKind.Cancelled => "A captura foi cancelada.",
        _ => "Não foi possível capturar a tela.",
    };

    public static string ForSave(SaveErrorKind kind) => kind switch
    {
        SaveErrorKind.PermissionDenied => "Sem permissão para gravar na pasta de destino. Escolha outra pasta nas configurações.",
        SaveErrorKind.DiskFull => "O disco está cheio. Libere espaço ou escolha outra pasta.",
        SaveErrorKind.InvalidPath => "A pasta de destino é inválida. Corrija o caminho nas configurações.",
        SaveErrorKind.DirectoryNotFound => "A pasta de destino não existe e não pôde ser criada.",
        SaveErrorKind.FileLocked => "O arquivo está em uso por outro programa.",
        SaveErrorKind.EncodingFailed => "Falha ao codificar a imagem em PNG.",
        _ => "Não foi possível salvar a captura.",
    };
}
