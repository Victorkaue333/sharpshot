# API — SharpShot (contratos internos)

> Camada **autoritativa e concisa** dos contratos internos. **Não há HTTP, REST nem rede no SharpShot** (ADR-15); a "API" deste projeto é o conjunto de **interfaces públicas de `SharpShot.Core`** e a forma como `Capture`, `Storage` e `App` as implementam e consomem. O arquivo mantém o nome do padrão de documentação para que o leitor saiba onde procurar o contrato entre camadas.
> Fontes: [`src/SharpShot.Core/Interfaces/`](../src/SharpShot.Core/Interfaces) · [`src/SharpShot.Core/Models/`](../src/SharpShot.Core/Models) · implementações em [`src/SharpShot.Capture`](../src/SharpShot.Capture) e [`src/SharpShot.Storage`](../src/SharpShot.Storage) (verificados) · [`ARCHITECTURE.md`](ARCHITECTURE.md) · [`DATABASE.md`](DATABASE.md).
> Última atualização: 04/nov/2026 · Verificado contra o código-fonte (5 interfaces, 5 implementações, 3 serviços estáticos).

## Visão geral

- **Namespace raiz:** `SharpShot.Core` (`Interfaces`, `Models`, `Enums`, `Exceptions`, `Services`).
- **Unidade de medida:** pixels **físicos** em tudo que cruza uma interface; DIP só dentro das janelas do App.
- **Formato de pixel:** `PixelBuffer` = BGRA8 compactado (`Stride = Width * 4`), sempre opaco depois da captura.
- **Assincronia:** métodos de E/S e captura são `Task`/`Task<T>` com `CancellationToken` opcional; `ConfigureAwait` não é usado (CA2007 desligado — o App depende do contexto capturado).
- **Erros:** sempre `CaptureException` (com `CaptureErrorKind`) ou `ScreenshotSaveException` (com `SaveErrorKind`); nunca `COMException`/`IOException` cruas para quem chama.
- **Composição (App):** `Microsoft.Extensions.DependencyInjection` em `App.xaml.cs`; lifetimes na tabela abaixo.

| Interface | Implementação | Projeto | Lifetime previsto | Status |
|-----------|---------------|---------|-------------------|--------|
| `IMonitorService` | `MonitorService` | `Capture` | Singleton (sem estado) | implementada |
| `IScreenCaptureService` | `WindowsGraphicsCaptureService` | `Capture` | Singleton (cacheia a permissão borderless) | implementada |
| `IImageEncoder` | `PngImageEncoder` | `Capture` | Singleton | implementada |
| `IScreenshotStorage` | `ScreenshotStorageService` | `Storage` | Singleton (calcula `DefaultDirectory` uma vez) | implementada |
| `ISettingsService` | `SettingsService` | `Storage` | Singleton (um arquivo, um `SemaphoreSlim`) | implementada |
| — | `Direct3DDeviceFactory` (`IDisposable`) | `Capture` | Singleton, descartado ao sair | implementada |
| `ICaptureCoordinator` *(App)* | orquestra captura → encoder → storage → notificação | `App` | Singleton | implementado |

## Convenções

- Interfaces recebem só tipos do `Core` (`MonitorInfo`, `PixelBuffer`, `AppSettings`, `byte[]`); nenhum tipo WinRT atravessa uma interface.
- `ArgumentNullException.ThrowIfNull` / `ThrowIfNullOrWhiteSpace` em toda entrada pública.
- Implementações recebem `ILogger<T>` por construtor (`Microsoft.Extensions.Logging.Abstractions`); logs em inglês.
- Records para resultados imutáveis; classes para modelos mutáveis (`AppSettings`, `PixelBuffer`).
- Nada aqui é estável no sentido SemVer: pré-1.0, os contratos podem mudar, mas **toda mudança atualiza este arquivo no mesmo PR**.

## Interfaces

### `IMonitorService`

```csharp
public interface IMonitorService
{
    IReadOnlyList<MonitorInfo> GetMonitors();          // primário primeiro, depois X, Y crescentes
    MonitorInfo GetPrimaryMonitor();
    MonitorInfo GetMonitorContaining(int x, int y);    // ou o mais próximo
    MonitorInfo GetMonitorUnderCursor();
    PixelRectangle GetVirtualDesktopBounds();          // caixa envolvente de todos
}
```

| Método | Implementação (`MonitorService`) | Erro |
|--------|----------------------------------|------|
| `GetMonitors` | `EnumDisplayMonitors` + `GetMonitorInfoW` + `GetDpiForMonitor(MDT_EFFECTIVE_DPI)`; monitor cujo `GetMonitorInfo` falha é pulado com warning | `CaptureException(NoMonitor)` se a enumeração falhar ou vier vazia |
| `GetMonitorContaining` | `MonitorFromPoint(DEFAULTTONEAREST)` → busca por handle → por `Bounds.Contains` → primário | — |
| `GetMonitorUnderCursor` | `GetCursorPos`; se falhar, primário | — |

Síncrono de propósito: são chamadas Win32 rápidas e a UI precisa do resultado antes de abrir overlays.

### `IScreenCaptureService`

```csharp
public interface IScreenCaptureService
{
    bool IsSupported { get; }   // GraphicsCaptureSession.IsSupported(), false se lançar
    Task<CapturedFrame> CaptureMonitorAsync(MonitorInfo monitor, CancellationToken cancellationToken = default);
}
```

Contrato de `CaptureMonitorAsync` (`WindowsGraphicsCaptureService`):

- Devolve **um** frame opaco BGRA8 com as dimensões físicas do monitor (ou do `ContentSize`, se menor).
- Não captura o cursor; tenta desligar a borda de captura.
- Timeout interno de **5 s** → `CaptureErrorKind.Timeout`.
- Retry **uma vez** com device novo após `DXGI_ERROR_DEVICE_REMOVED`/`RESET`.
- Pode ser aguardado de qualquer thread (frame pool free-threaded).
- Lança `CaptureException` com `NotSupported`, `NoMonitor`, `DeviceFailure`, `Timeout` ou `Cancelled`.

### `IImageEncoder`

```csharp
public interface IImageEncoder
{
    string FileExtension { get; }   // ".png"
    string FormatName { get; }      // "PNG"
    Task<byte[]> EncodeAsync(PixelBuffer pixels, CancellationToken cancellationToken = default);
}
```

`PngImageEncoder`: WIC `BitmapEncoder.PngEncoderId`, `Bgra8` + `BitmapAlphaMode.Ignore`, 96 DPI como metadado; falha → `ScreenshotSaveException(EncodingFailed)`. Detalhe: [`capture-engine/png.md`](capture-engine/png.md).

### `IScreenshotStorage`

```csharp
public interface IScreenshotStorage
{
    string DefaultDirectory { get; }                       // <Imagens do usuário>\SharpShot
    string ResolveDirectory(string? configuredDirectory);  // null/vazio → default; expande %VAR%; GetFullPath
    PathValidationResult ValidateDirectory(string? directory);
    Task<SavedScreenshot> SaveAsync(byte[] data, string directory, string preferredFileName, CancellationToken cancellationToken = default);
}
```

Contrato de `SaveAsync` (`ScreenshotStorageService`):

1. Valida sintaticamente (`PathValidator`) → `InvalidPath`.
2. `Directory.CreateDirectory` → erro classificado.
3. `FileNaming.MakeUnique` (`_1`, `_2`, … até `_1000`).
4. `FileStream(FileMode.CreateNew, FileShare.None, 64 KiB, async)` — **nunca sobrescreve**.
5. Colisão/arquivo bloqueado (`ERROR_SHARING_VIOLATION`, `ERROR_LOCK_VIOLATION`, `ERROR_FILE_EXISTS`, `ERROR_ALREADY_EXISTS`) → acrescenta `_x` ao nome e tenta de novo, até 3 vezes.
6. Qualquer outra falha → apaga o arquivo parcial e lança `ScreenshotSaveException` classificada.

`ValidateDirectory(null/"")` é **válido** (significa "usar o padrão"); `SaveAsync` com `directory` vazio lança `ArgumentException` — o App resolve antes com `ResolveDirectory`.

### `ISettingsService`

```csharp
public interface ISettingsService
{
    AppSettings Current { get; }              // snapshot; não mutar — use SaveAsync
    string SettingsFilePath { get; }
    event EventHandler<AppSettings>? Changed; // depois de salvar com sucesso
    Task LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
```

`SettingsService`: arquivo em `%LOCALAPPDATA%\SharpShot\settings.json` (construtor alternativo aceita outro caminho — é o que os testes usam); `LoadAsync` com arquivo ausente → padrões; corrompido → backup + padrões; `SaveAsync` normaliza (`SchemaVersion`, hotkeys inválidos → padrão, pasta vazia → `null`), grava atômico e dispara `Changed` **fora** do lock. Esquema do arquivo: [`DATABASE.md`](DATABASE.md).

## Modelos e records

| Tipo | Campos | Notas |
|------|--------|-------|
| `PixelRectangle` (record struct) | `X, Y, Width, Height` + `Left/Top/Right/Bottom` (exclusivos), `IsEmpty`, `Area` | `FromPoints` normaliza; `FromLTRB`; `Contains`; `Intersect`; `Offset`; `RelativeTo`; coordenadas podem ser negativas |
| `PixelBuffer` (class) | `Width, Height, Stride, Data (byte[]), SizeInBytes` | Construtor valida `Data.Length == W*H*4`; `Create`, `Crop` (lança fora dos limites), `ForceOpaque`, `IsOpaque`, `GetPixel`/`SetPixel` (B, G, R, A) |
| `MonitorInfo` (record) | `Handle, DeviceName, Bounds, WorkArea, IsPrimary, Dpi` | `ScaleFactor = Dpi / 96.0` |
| `HotkeyBinding` (record) | `Modifiers (HotkeyModifiers), VirtualKey (uint)` | `IsValid` (≥ 1 modificador + tecla atribuível); `Parts`; `ToDisplayString()` → `"Ctrl + Shift + 1"`; `ToString()` → `"Ctrl+Shift+1"`; `TryParse`; `DefaultFullScreen`/`DefaultRegion`; JSON como string via `HotkeyBindingJsonConverter` |
| `VirtualKeyNames` (static) | — | Nomes de 0–9, A–Z, F1–F24, `Num0`–`Num9`, `Num*`/`+`/`-`/`.`/`/`, teclas de edição/navegação, pontuação; `IsModifierKey`, `IsAssignable`, `GetName`, `TryGetVirtualKey` |
| `AppSettings` (class) | `SchemaVersion, StartWithWindows, MinimizeToTray, ShowNotificationAfterCapture, FullScreenHotkey, RegionHotkey, SaveDirectory, Theme` | `CreateDefault()`, `Clone()` (shallow — records são imutáveis) |
| `CapturedFrame` (record) | `Pixels, Monitor, Timestamp (DateTimeOffset local)` | Saída de `CaptureMonitorAsync` |
| `SavedScreenshot` (record) | `FilePath, SizeBytes` | Saída de `SaveAsync` |
| `CaptureResult` (record) | `FilePath, Width, Height, Mode (CaptureMode), Timestamp, FileSizeBytes` + `FileName`, `Directory` | Montado pelo App para status/toast/histórico futuro |
| `PathValidationResult` (record) | `IsValid, Error` | `Valid`, `Invalid(msg)`; mensagens já em pt-BR |

## Enums

| Enum | Valores | Uso |
|------|---------|-----|
| `CaptureMode` | `FullScreen = 0`, `Region = 1` | `CaptureResult.Mode` |
| `ThemeMode` | `System = 0`, `Light = 1`, `Dark = 2` | `AppSettings.Theme`; serializado como string |
| `HotkeyModifiers` (`[Flags]`) | `None = 0`, `Alt = 1`, `Control = 2`, `Shift = 4`, `Windows = 8` | Iguais aos `MOD_*` do `RegisterHotKey` |
| `ResizeHandle` | `None`, `TopLeft`, `Top`, `TopRight`, `Right`, `BottomRight`, `Bottom`, `BottomLeft`, `Left`, `Move` | `RegionMath.HitTest`/`Resize` |
| `CaptureErrorKind` | `Unknown`, `NotSupported`, `AccessDenied`, `DeviceFailure`, `Timeout`, `NoMonitor`, `Cancelled` | `CaptureException.Kind` |
| `SaveErrorKind` | `Unknown`, `PermissionDenied`, `DiskFull`, `InvalidPath`, `DirectoryNotFound`, `FileLocked`, `EncodingFailed` | `ScreenshotSaveException.Kind` |

## Serviços estáticos (`SharpShot.Core.Services`)

| Serviço | Assinaturas principais | Regra |
|---------|------------------------|-------|
| `RegionMath` | `DipToPixel(double, double)`, `PixelToDip(int, double)`, `ClampPoint`, `FromDrag(sx, sy, cx, cy, bounds)`, `Clamp`, `Move(rect, dx, dy, bounds)`, `HitTest(rect, x, y, tolerance)`, `Resize(rect, handle, x, y, bounds)`, `Flip(handle, h, v)` | Pura; pixels físicos; ver [`capture-engine/regiao.md`](capture-engine/regiao.md) |
| `FileNaming` | `CreateFileName(DateTimeOffset, extension)`, `MakeUnique(directory, fileName, Func<string,bool> fileExists)` | `SharpShot_yyyy-MM-dd_HH-mm-ss-fff.png`; sufixo `_n` até 1000, depois `IOException` |
| `PathValidator` | `ValidateDirectory(string?)` | Sem acesso a disco: vazio, caracteres inválidos, não absoluto, nomes reservados (`CON`, `COM1`…), segmento terminando em espaço/ponto |

## Exceções

```csharp
public sealed class CaptureException : Exception        { CaptureErrorKind Kind { get; } }
public sealed class ScreenshotSaveException : Exception { SaveErrorKind Kind { get; } }
```

Ambas carregam `InnerException` (a `COMException`/`IOException` original) para o log. A UI lê só `Kind` e consulta a tabela de mensagens em [`architecture/erros.md`](architecture/erros.md).

## Exemplo de uso (o que o `ICaptureCoordinator` do App fará)

```csharp
var monitor = monitors.GetMonitorUnderCursor();
CompositionSync.WaitForComposition();                         // janela já escondida
var frame   = await capture.CaptureMonitorAsync(monitor, ct);  // CapturedFrame
var bytes   = await encoder.EncodeAsync(frame.Pixels, ct);     // PNG
var dir     = storage.ResolveDirectory(settings.Current.SaveDirectory);
var name    = FileNaming.CreateFileName(frame.Timestamp, encoder.FileExtension);
var saved   = await storage.SaveAsync(bytes, dir, name, ct);   // nunca sobrescreve
var result  = new CaptureResult(saved.FilePath, frame.Pixels.Width, frame.Pixels.Height,
                                CaptureMode.FullScreen, frame.Timestamp, saved.SizeBytes);
```

## Planejado × implementado

| Item | Status |
|------|--------|
| 5 interfaces + 5 implementações + `Direct3DDeviceFactory` | implementado |
| `RegionMath`, `FileNaming`, `PathValidator`, modelos, enums, exceções | implementado |
| `ICaptureCoordinator`, ViewModels, registro no container DI | implementado (App) |
| `CaptureErrorKind.AccessDenied` / `Unknown` | declarados, nenhum caminho os lança hoje |
| Testes das interfaces (`tests/`) | implementados — 285 (Core/Storage) + 35 (Capture), todos verdes |

---

Arquitetura: [`ARCHITECTURE.md`](ARCHITECTURE.md) · Persistência: [`DATABASE.md`](DATABASE.md) · Erros: [`architecture/erros.md`](architecture/erros.md) · Decisões: [`DECISIONS.md`](DECISIONS.md)
