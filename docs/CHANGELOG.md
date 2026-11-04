[Não publicado]: https://github.com/Victorkaue333/sharpshot/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/Victorkaue333/sharpshot/releases/tag/v0.1.0

# Changelog — SharpShot

Todas as mudanças relevantes deste projeto são registradas aqui.
Formato: [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) · Versões: [SemVer](https://semver.org/lang/pt-BR/).

Tipos usados: Adicionado, Alterado, Corrigido, Removido, Depreciado, Segurança. Versões da mais recente para a mais antiga. Cada entrada diz **o quê** e **o porquê**, e rastreia a um ADR, requisito ou arquivo.

> Contexto e decisões fixas: [`../CLAUDE.md`](../CLAUDE.md) · Visão geral: [`../README.md`](../README.md).
> Projeto em **desenvolvimento inicial** (pré-1.0); contratos internos podem mudar sem aviso.
> `0.1.0` publicada em 04/nov/2026 (tag `v0.1.0`, Release no GitHub com `SharpShot-0.1.0-win-x64.zip`). O que está em *Não publicado* sai na `0.1.1`.

## [Não publicado][Não publicado]

### Adicionado

- **Instalador Windows** (`SharpShot-Setup-<versão>.exe`, Inno Setup, `installer/SharpShot.iss`) — assistente em pt-BR, instalação por usuário (sem UAC) ou para todos (Program Files), atalho no Menu Iniciar, atalho na área de trabalho opcional, opção "iniciar com o Windows", entrada em *Aplicativos instalados*, desinstalador que encerra o app, remove a chave `Run` e pergunta se apaga configurações/logs. Gerado por `make-release.ps1` e anexado à Release pelo `release.yml`. O zip continua como versão portátil. *Porquê:* o dono quer o fluxo de instalação de qualquer software; o instalador é o download principal.
- **Aviso único de bandeja** — na primeira vez que a janela é fechada com *Minimizar para a bandeja* ligado, um toast explica que o SharpShot continua na bandeja (ícone laranja perto do relógio) e como abrir/capturar/sair; campo `trayHintShown` em `settings.json`. *Porquê:* no Windows 11 ícones novos ficam ocultos no menu "^" e o usuário não via o app.
- README com botão de download direto da release e instruções para fixar o ícone na bandeja.

### Corrigido

- Janela de mensagens da bandeja/atalhos passou de *message-only* (`HWND_MESSAGE`) para janela oculta comum (`WS_POPUP` + `WS_EX_TOOLWINDOW`): janelas message-only não recebem o broadcast `TaskbarCreated` (o ícone não voltaria após reiniciar o Explorer) nem podem ser *foreground* do menu popup. Menu da bandeja validado (Abrir, Sair) em 04/nov/2026.
- `PngImageEncoder` observa o `CancellationToken` antes de iniciar o WIC (teste de cancelamento era instável no runner do GitHub).

## [0.1.0][0.1.0] — 2026-11-04

### Adicionado

- **Estrutura da solução** — `SharpShot.slnx` (formato XML) com os projetos `src/SharpShot.Core`, `src/SharpShot.Capture`, `src/SharpShot.Storage`, `src/SharpShot.App` e os testes `tests/SharpShot.Core.Tests`, `tests/SharpShot.Capture.Tests`; plataforma x64. `Directory.Build.props` (versão `0.1.0`, nullable, analisadores `latest-recommended`, `NeutralLanguage=pt-BR`, fallback de plataforma para x64 porque WinUI não compila AnyCPU), `Directory.Packages.props` (versões centrais: Windows App SDK 2.6.5, Microsoft.Extensions 10.0.14, xunit 2.9.3), `global.json` (SDK 10.0.100, `latestFeature`), `.editorconfig` (CRLF, 4 espaços, namespaces file-scoped, chaves obrigatórias, `_camelCase`) e `.gitignore`. *Porquê:* fronteira estrita entre lógica pura e plataforma desde o primeiro commit ([ADR-12](DECISIONS.md)).
- **`SharpShot.Core`** (`net10.0`, sem Windows) — modelos `PixelRectangle`, `PixelBuffer` (BGRA8, `Crop`, `ForceOpaque`), `MonitorInfo`, `HotkeyBinding` + `VirtualKeyNames` (serializa como `"Ctrl+Shift+1"`), `AppSettings`, records `CaptureResult`/`CapturedFrame`/`SavedScreenshot`/`PathValidationResult`; enums `CaptureMode`, `ThemeMode`, `HotkeyModifiers` (valores `MOD_*`), `ResizeHandle`, `CaptureErrorKind`, `SaveErrorKind`; exceções `CaptureException`/`ScreenshotSaveException`; interfaces `IMonitorService`, `IScreenCaptureService`, `IImageEncoder`, `IScreenshotStorage`, `ISettingsService`; serviços `RegionMath` (arrasto, clamp, DIP↔pixel, hit-test, resize), `FileNaming` (`SharpShot_yyyy-MM-dd_HH-mm-ss-fff.png`, sufixo `_n`), `PathValidator` (mensagens em pt-BR). *Porquê:* tudo que pode ser testado sem sessão de desktop fica aqui.
- **`SharpShot.Capture`** (`net10.0-windows10.0.26100.0`, mínimo 19041) — `NativeMethods` (`user32`/`shcore`/`dwmapi`/`d3d11`), `GraphicsCaptureInterop` (`IGraphicsCaptureItemInterop.CreateForMonitor`), `Direct3DDeviceFactory` (hardware → WARP; recria após device-lost), `MonitorService` (`EnumDisplayMonitors` + DPI efetivo; primário primeiro), `WindowsGraphicsCaptureService` (frame pool free-threaded de 2 buffers em B8G8R8A8, cursor off, `IsBorderRequired=false` quando `GraphicsCaptureAccess` permite, `SoftwareBitmap.CreateCopyFromSurfaceAsync`, timeout 5 s, um retry após `DXGI_ERROR_DEVICE_REMOVED/RESET`, recorte para `ContentSize`), `CompositionSync` (`DwmFlush` ×2), `PngImageEncoder` (WIC, BGRA8, alpha ignorado, 96 DPI só metadado). *Porquê:* [ADR-02](DECISIONS.md), [ADR-03](DECISIONS.md), [ADR-04](DECISIONS.md).
- **`SharpShot.Storage`** (`net10.0`) — `SettingsService` (JSON source-generated via `SettingsJsonContext`, `%LOCALAPPDATA%\SharpShot\settings.json`, escrita atômica `tmp` + `Move`, backup `*.corrupt-<timestamp>`, normalização), `ScreenshotStorageService` (pasta padrão `Imagens\SharpShot` honrando redirecionamento, `FileMode.CreateNew` — nunca sobrescreve, até 3 tentativas com `_x` em colisão/bloqueio, classificação em `SaveErrorKind`: permissão, disco cheio, caminho inválido, pasta inexistente, arquivo bloqueado, falha de codificação). *Porquê:* [ADR-09](DECISIONS.md); o usuário nunca perde uma captura por sobrescrita.
- **`SharpShot.App`** (WinUI 3, unpackaged, self-contained) — `Program.Main` próprio com instância única (`AppInstance.FindOrRegisterForKey`) e ativação por toast; DI (`Microsoft.Extensions.DependencyInjection`) + logging em arquivo (`%LOCALAPPDATA%\SharpShot\logs`, 7 dias); barra de título custom com logo; `NavigationView` (Início · Configurações · Sobre); Início com cartões de captura, pasta, formato fixo PNG, status e aviso de atalho não registrado; Configurações (iniciar com o Windows via `HKCU\...\Run` + `--minimized`, minimizar para a bandeja, notificação, editor de atalhos com detecção de conflito e restaurar padrões, pasta de destino com validação, convenção de nomes, tema); `RegionOverlayWindow` por monitor (frame congelado, máscara, borda laranja tracejada, 8 handles, badge de tamanho, Capturar/Cancelar, Enter/Esc); `HotkeyService` (`RegisterHotKey` + `MOD_NOREPEAT`), `TrayIconService` (`Shell_NotifyIcon` + menu Win32, re-adiciona em `TaskbarCreated`), `NotificationService` (`AppNotificationManager`, botões Abrir pasta/Ver imagem), `CaptureCoordinator` (oculta janelas → `DwmFlush` → captura → recorte → PNG → salva → notifica), `WDA_EXCLUDEFROMCAPTURE` nas janelas próprias (`SHARPSHOT_ALLOW_SELF_CAPTURE=1` desliga, só para desenvolvimento). *Porquê:* captura nunca na UI; integração nativa sem hooks nem WebView ([DECISIONS](DECISIONS.md)).
- **Branding** — `tools/branding/generate_branding.py` (Python + Pillow) como fonte única do logo (Conceito A); gera `assets/branding/` (SVG/PNG), `assets/icons/` (`sharpshot.ico`, `tray.ico`, PNGs 16–256), os assets de runtime do App e `LogoGeometry.cs`. Mockups de referência em `public/assets/`. *Porquê:* [ADR-13](DECISIONS.md).
- **Testes automatizados** — `tests/SharpShot.Core.Tests` (285: geometria, `PixelBuffer.Crop` byte a byte, atalhos incl. `Num+`, nomes de arquivo, validação de caminhos, `SettingsService` com arquivo corrompido/backup, `ScreenshotStorageService` sem sobrescrita) e `tests/SharpShot.Capture.Tests` (35: PNG sem perdas via `BitmapDecoder`, enumeração de monitores, captura real com WGC — pulam sem desktop interativo/WGC). Todos verdes em Debug e Release.
- **CI e release** — `.github/workflows/build.yml` (restore, build Release x64, testes, publish, artefato `SharpShot-win-x64`; pendente de execução no GitHub) e `.github/workflows/release.yml` (tag `v*` → testes → `tools/release/make-release.ps1` → GitHub Release com `SharpShot-<versão>-win-x64.zip` + `.sha256`). O script empacota o publish self-contained, inclui o runtime VC++ (`vcruntime140*`, `msvcp140*`, `concrt140`) do Redist do Visual Studio e remove `.pdb`. Validado localmente: 66,5 MB, exe publicado captura.
- **Documentação canônica em pt-BR** — `README.md`, `PROJECT_CONTEXT.md`, `CLAUDE.md`, `CONTRIBUTING.md`, `LICENSE` (MIT) na raiz; `docs/INDEX.md`, `PRD.md`, `ARCHITECTURE.md`, `API.md` (contratos internos — não há HTTP), `DATABASE.md` (persistência local — não há banco), `DEPLOY.md`, `ENVIRONMENT.md`, `DECISIONS.md` (ADR-01..15), `CHANGELOG.md`, `DESIGN.md`, `Code-Style.md`; pastas `docs/project-context/`, `architecture/`, `capture-engine/`, `development/`, `distribution/`, `design-system/`. *Porquê:* padrão de documentação com camada canônica fina e autoritativa que resume e linka, em vez de duplicar; os arquivos `API.md` e `DATABASE.md` mantêm o nome do padrão com o conceito adaptado a um app desktop (declarado no cabeçalho de cada um).

### Corrigido

- Atalhos cujo nome de tecla contém `+` (`Num+`) voltam a ser lidos do `settings.json`; um atalho inválido no arquivo recai no padrão daquela ação em vez de descartar o arquivo inteiro; atalhos gravados sem escape JSON (`"Ctrl+Shift+1"`).
- `ScreenshotStorageService` nunca apaga um arquivo preexistente em colisão de nome; cancelamento propaga em vez de virar erro de gravação.
- Overlay de região ignora entrada e render após fechar e desregistra handlers (endurecimento após uma saída silenciosa do processo observada uma vez, não reproduzida).
- CI: etapas de teste reconstroem (sem `--no-build`), já que build de solução e de projeto usam pastas de saída diferentes; arm64 removido por nunca ter sido compilado.

### Alterado

- `README.md` — substituído o README de duas linhas do commit inicial pela versão completa (badges, visão geral, diagramas, setup, comandos, mapa da documentação, roadmap, créditos, licença).

### Removido

- Nada ainda.

---

## [0.1.0][0.1.0] - 2026-11-04 *(versão alvo — não publicada)*

Commit inicial `3ab422b` (04/nov/2026): apenas `README.md` com nome e descrição do projeto. A versão `0.1.0` em `Directory.Build.props` é o **alvo do MVP**; a release só existirá quando os critérios de aceite de [`project-context/criterios-de-aceitacao.md`](project-context/criterios-de-aceitacao.md) estiverem validados.

### Adicionado

- `README.md` inicial (2 linhas): nome e descrição em inglês.

---

## Convenções

- **Pré-1.0:** enquanto em `0.x`, mudanças incompatíveis podem ocorrer em qualquer versão menor (cláusula 4 do SemVer), inclusive no esquema de `settings.json` (com `schemaVersion` e migração, ver [`DATABASE.md`](DATABASE.md)).
- **Rastreabilidade:** cada entrada rastreia a um ADR em [`DECISIONS.md`](DECISIONS.md), a um requisito em [`project-context/requisitos.md`](project-context/requisitos.md) ou a um arquivo. Sem invenção; sem "verificado" sem execução real.
- **Commits:** [Conventional Commits](https://www.conventionalcommits.org/pt-br/) em pt-BR, feitos **apenas pelo mantenedor** (regra em [`../CLAUDE.md`](../CLAUDE.md)); ver [`Code-Style.md`](Code-Style.md#10-git).
- **Ao publicar:** mover o bloco *Não publicado* para `## [X.Y.Z] - AAAA-MM-DD`, atualizar os links de comparação no topo e criar a tag `vX.Y.Z` manualmente ([`DEPLOY.md`](DEPLOY.md)).

<!-- Sem git tags publicadas ainda; os links de versão apontam para o commit inicial real. -->
