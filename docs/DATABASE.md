# DATABASE — SharpShot (persistência local)

> Camada canônica, concisa e autoritativa da **persistência local**. **Não há banco de dados no SharpShot** (ADR-09, ADR-15): o que o app grava em disco são três coisas — `settings.json`, a pasta de capturas (PNGs) e logs. O arquivo mantém o nome do padrão de documentação para que "onde ficam os dados?" tenha uma resposta fixa.
> **Fonte da verdade viva do esquema:** [`src/SharpShot.Core/Models/AppSettings.cs`](../src/SharpShot.Core/Models/AppSettings.cs) + [`src/SharpShot.Storage/Configuration/SettingsJsonContext.cs`](../src/SharpShot.Storage/Configuration/SettingsJsonContext.cs) — em caso de divergência, o código prevalece.
> Relacionados: [`ARCHITECTURE.md`](ARCHITECTURE.md) · [`API.md`](API.md) · [`ENVIRONMENT.md`](ENVIRONMENT.md) · [`DECISIONS.md`](DECISIONS.md).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Visão geral

| O quê | Onde | Formato | Dono no código | Status |
|-------|------|---------|----------------|--------|
| Preferências | `%LOCALAPPDATA%\SharpShot\settings.json` | JSON (UTF-8, indentado) | `SettingsService` | implementado |
| Capturas | `<Imagens>\SharpShot\SharpShot_<timestamp>.png` (padrão) ou pasta escolhida | PNG RGB 8-bit | `ScreenshotStorageService` | implementado |
| Logs | `%LOCALAPPDATA%\SharpShot\logs\sharpshot-AAAAMMDD.log` | texto | `FileLoggerProvider` do App (um arquivo por dia, 7 mantidos) | implementado |
| Iniciar com o Windows | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` → valor `SharpShot` = `"<caminho>\SharpShot.exe" --minimized` | REG_SZ | `StartupService` do App | implementado e validado |
| Registro de notificações | feito pelo `AppNotificationManager` (AUMID + COM server, por usuário); desfeito em `Sair` | registro | `NotificationService` do App | implementado e validado |

Regra de ouro: **nada fora do perfil do usuário** — sem `HKLM`, sem `Program Files`, sem `ProgramData`. Desinstalar é apagar o que está nesta tabela ([`distribution/instalacao.md`](distribution/instalacao.md)).

```mermaid
flowchart LR
    App["SharpShot.App"] -->|LoadAsync / SaveAsync| S["SettingsService"]
    S -->|tmp + File.Move| J[("settings.json")]
    S -.->|se corrompido| B[("settings.json.corrupt-yyyyMMdd-HHmmss")]
    App -->|SaveAsync(bytes, dir, name)| St["ScreenshotStorageService"]
    St -->|FileMode.CreateNew| P[("SharpShot_….png")]
    App -->|ILogger| L[("logs\\*.log")]
    App -->|"StartWithWindows"| R[("HKCU\\…\\Run\\SharpShot")]
```

## `settings.json` — esquema campo a campo

Gerado por `SettingsJsonContext` com: `PropertyNamingPolicy = CamelCase`, `UseStringEnumConverter = true`, `WriteIndented = true`, `DefaultIgnoreCondition = Never` (todo campo é sempre escrito), e na leitura `ReadCommentHandling = Skip` + `AllowTrailingCommas = true` (o usuário pode anotar o arquivo à mão).

Exemplo com os **valores padrão** (o que `AppSettings.CreateDefault()` produz):

```json
{
  "schemaVersion": 1,
  "startWithWindows": false,
  "minimizeToTray": true,
  "showNotificationAfterCapture": true,
  "fullScreenHotkey": "Ctrl+Shift+1",
  "regionHotkey": "Ctrl+Shift+2",
  "saveDirectory": null,
  "theme": "System"
}
```

| Campo | Tipo JSON | Tipo C# | Padrão | Regras |
|-------|-----------|---------|--------|--------|
| `schemaVersion` | number | `int` | `1` | Sempre reescrito para `AppSettings.CurrentSchemaVersion` ao salvar. Reservado para migração; hoje não há migração |
| `startWithWindows` | boolean | `bool` | `false` | Espelha a chave `Run`; o App deve ler o registro como fonte da verdade (ADR-08) |
| `minimizeToTray` | boolean | `bool` | `true` | Fechar a janela esconde na bandeja em vez de sair |
| `showNotificationAfterCapture` | boolean | `bool` | `true` | Toast após salvar |
| `fullScreenHotkey` | string | `HotkeyBinding` | `"Ctrl+Shift+1"` | Formato `Mod+Mod+Tecla`; modificadores aceitos na leitura: `Ctrl`/`Control`, `Shift`, `Alt`, `Win`/`Windows` (sem distinção de maiúsculas); escrita sempre na ordem `Ctrl, Shift, Alt, Win`. Inválido ou `null` → padrão |
| `regionHotkey` | string | `HotkeyBinding` | `"Ctrl+Shift+2"` | Idem |
| `saveDirectory` | string \| null | `string?` | `null` | `null`/vazio/só espaços = pasta padrão. Pode conter `%VARIAVEL%` (expandido em `ResolveDirectory`). Não é validado ao salvar: a validação acontece na UI (`ValidateDirectory`) e no `SaveAsync` |
| `theme` | string | `ThemeMode` | `"System"` | `"System"` · `"Light"` · `"Dark"` |

| `trayHintShown` | bool | `false` | Vira `true` após o aviso único "O SharpShot continua na bandeja" (mostrado na primeira vez que a janela é fechada com *Minimizar para a bandeja* ligado). |
Nomes de tecla aceitos em hotkeys: `0`–`9`, `A`–`Z`, `F1`–`F24`, `Num0`–`Num9`, `Num*`, `Num+`, `Num-`, `Num.`, `Num/`, `Backspace`, `Tab`, `Enter`, `Pause`, `CapsLock`, `Esc`, `Space`, `PageUp`, `PageDown`, `End`, `Home`, `Left`, `Up`, `Right`, `Down`, `PrintScreen`, `Insert`, `Delete`, `NumLock`, `ScrollLock`, `;`, `=`, `,`, `-`, `.`, `/`, `` ` ``, `[`, `\`, `]`, `'` (ver `VirtualKeyNames`). Um hotkey **precisa de pelo menos um modificador**.

### Ciclo de vida

| Operação | Comportamento (`SettingsService`) |
|----------|-----------------------------------|
| **Carregar** | Arquivo ausente → padrões (log informativo). `JsonException`/`IOException`/`UnauthorizedAccessException` → arquivo renomeado para `settings.json.corrupt-yyyyMMdd-HHmmss` e padrões assumem (warning). Depois de ler, **normaliza** |
| **Normalizar** | `schemaVersion := 1`; hotkey `null` ou `!IsValid` → padrão; `saveDirectory` em branco → `null` |
| **Salvar** | Clona + normaliza; cria a pasta; escreve em `settings.json.tmp`; `File.Move(tmp, final, overwrite: true)` (atômico no mesmo volume); atualiza `Current`; dispara `Changed` fora do lock |
| **Concorrência** | `SemaphoreSlim(1,1)` serializa load/save no processo. Entre processos não há lock — a instância única (RF-11) evita o caso |
| **Chave desconhecida** | Ignorada na leitura e **perdida** na próxima gravação (não há `JsonExtensionData`) |
| **Campo ausente** | Assume o padrão do construtor de `AppSettings` |

### Migração de esquema

Não existe migração hoje. Quando `schemaVersion` subir, a regra é: ler o JSON como `JsonDocument`, converter campo a campo e só então desserializar — nunca quebrar um arquivo de versão anterior. Registrar em [`DECISIONS.md`](DECISIONS.md).

## Pasta de capturas e convenção de nomes

**Pasta padrão:** `Environment.GetFolderPath(SpecialFolder.MyPictures, DoNotVerify)` + `\SharpShot`. `GetFolderPath` honra o redirecionamento de Known Folders (ex.: Imagens dentro do OneDrive); se devolver vazio, cai para `%USERPROFILE%\Pictures\SharpShot`. No Explorer em pt-BR a pasta aparece como **Imagens\SharpShot**; no disco o nome costuma ser `Pictures`.

**Pasta configurada:** `ResolveDirectory` → `Trim` → `ExpandEnvironmentVariables` → `Path.GetFullPath`; se inválida, volta ao padrão com warning. A pasta é criada em `SaveAsync` se não existir.

**Nome do arquivo** (`FileNaming`):

```text
SharpShot_yyyy-MM-dd_HH-mm-ss-fff.png
SharpShot_2026-11-04_14-32-07-583.png      ← hora local do momento da captura (CapturedFrame.Timestamp)
SharpShot_2026-11-04_14-32-07-583_1.png    ← colisão no mesmo milissegundo
SharpShot_2026-11-04_14-32-07-583_x.png    ← arquivo bloqueado/existente detectado só na abertura (até 3 tentativas)
```

- Prefixo fixo `SharpShot`, timestamp com `CultureInfo.InvariantCulture` (ordena corretamente em qualquer locale).
- Sufixo `_1`…`_1000` quando o nome já existe; acima disso `IOException` → `SaveErrorKind.Unknown`.
- **Nunca sobrescreve**: `FileMode.CreateNew`.
- Tamanho típico: depende do conteúdo; **não medido** (pendente).

## Erros de gravação (classificação)

| `SaveErrorKind` | Origem | HRESULT / exceção |
|-----------------|--------|-------------------|
| `InvalidPath` | `PathValidator` reprovou; `PathTooLongException`, `NotSupportedException`, `ArgumentException` | — |
| `DirectoryNotFound` | `DirectoryNotFoundException` (unidade removida entre validar e gravar) | — |
| `PermissionDenied` | `UnauthorizedAccessException` | — |
| `DiskFull` | `IOException` | `0x80070070` (`ERROR_DISK_FULL`), `0x80070027` (`ERROR_HANDLE_DISK_FULL`) |
| `FileLocked` | `IOException` após esgotar as 3 tentativas | `0x80070020` (`ERROR_SHARING_VIOLATION`), `0x80070021` (`ERROR_LOCK_VIOLATION`) |
| `EncodingFailed` | `PngImageEncoder` (vem antes do storage) | `COMException` etc. |
| `Unknown` | Qualquer outra | — |

Arquivo parcial é apagado antes de lançar. Mensagens ao usuário: [`architecture/erros.md`](architecture/erros.md).

## Logs

- Pasta: `%LOCALAPPDATA%\SharpShot\logs\` (criada pelo App no primeiro log; formato `yyyy-MM-dd HH:mm:ss.fff [NÍVEL] Categoria: mensagem`).
- Conteúdo: eventos de captura (`Captured \\.\DISPLAY1: 2560x1440 px.`), gravação (`Screenshot saved: … (N bytes).`), settings (`Settings loaded from …`), warnings de fallback (WARP, DPI, corrupt settings) e exceções com stack. **Nunca** o conteúdo da imagem, nunca caminhos de outros apps.
- Rotação/retenção: **a definir** no App (sugestão: um arquivo por dia, manter 7).
- Nível `Debug` vai também para o `DebugLoggerProvider` (Output do Visual Studio).

## Registro do Windows

Única chave tocada (ADR-08), apenas quando "Iniciar com o Windows" está ligado:

```text
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
    SharpShot    REG_SZ    "C:\caminho\da\pasta\SharpShot.exe" --minimized
```

O `AppNotificationManager` registra, por conta própria e por usuário, o AUMID e o COM server de ativação quando `Register()` é chamado; `Unregister()` remove. Detalhe de desinstalação em [`distribution/instalacao.md`](distribution/instalacao.md).

## Backup e portabilidade

- **Backup:** copiar `settings.json` e a pasta de capturas. Nada mais tem estado.
- **Portabilidade entre máquinas:** `settings.json` é portátil; `saveDirectory` absoluto pode não existir na outra máquina → o app cai para a pasta padrão com warning.
- **Reset total:** apagar `%LOCALAPPDATA%\SharpShot` com o app fechado.

## Lacunas conhecidas

- [x] Logger de arquivo, rotação diária e retenção de 7 arquivos: implementado (`FileLoggerProvider`).
- [ ] `startWithWindows` pode divergir da chave `Run` se o usuário desativar pelo Gerenciador de Tarefas; o App lê o registro ao abrir Configurações (`StartupService.IsEnabled`), mas não corrige o JSON automaticamente — melhoria futura.
- [ ] Nenhum teste de round-trip de `settings.json` existe ainda (`tests/SharpShot.Core.Tests` tem só o `.csproj`).
- [ ] Histórico de capturas (roadmap) pode trazer um banco local (SQLite) — decisão futura, ADR próprio.

---

Contratos: [`API.md`](API.md) · Ambiente: [`ENVIRONMENT.md`](ENVIRONMENT.md) · Instalação/desinstalação: [`distribution/instalacao.md`](distribution/instalacao.md)
