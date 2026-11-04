# DECISIONS — SharpShot

Registro das decisões técnicas do projeto (ADRs). Cada decisão é uma entrada **imutável**: em vez de editar uma decisão antiga, adiciona-se uma nova que a substitua — o histórico é a memória do projeto.

> Os números de ADR são **estáveis** e referenciados por outros docs (não renumerar). ADR-01..15 nasceram juntos, em 04/nov/2026, consolidando as decisões fixas tomadas durante o desenho do MVP; a partir daqui, toda decisão nova ganha o próximo número. "Implementada" significa **verificada no código** em 04/nov/2026 (todos os projetos, incluindo `SharpShot.App`, estão implementados).
> Fonte da verdade das decisões fixas: [`../CLAUDE.md`](../CLAUDE.md) · Arquitetura: [`ARCHITECTURE.md`](ARCHITECTURE.md) · Detalhes técnicos: [`architecture/`](architecture/README.md) e [`capture-engine/`](capture-engine/README.md).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Índice

| # | Data | Decisão | Status |
|---|------|---------|--------|
| ADR-01 | 04/nov/2026 | WinUI 3 unpackaged e self-contained como distribuição primária | Aceita · implementada no `.csproj` |
| ADR-02 | 04/nov/2026 | Windows Graphics Capture em vez de GDI ou Desktop Duplication | Aceita · implementada |
| ADR-03 | 04/nov/2026 | `SoftwareBitmap.CreateCopyFromSurfaceAsync` em vez de binding D3D11 | Aceita · implementada |
| ADR-04 | 04/nov/2026 | PNG sem perdas como único formato do MVP | Aceita · implementada |
| ADR-05 | 04/nov/2026 | `RegisterHotKey` em vez de hook de teclado de baixo nível | Aceita · implementada |
| ADR-06 | 04/nov/2026 | `Shell_NotifyIcon` e menu popup Win32 para a bandeja | Aceita · implementada |
| ADR-07 | 04/nov/2026 | `AppNotificationManager` para notificações | Aceita · implementada |
| ADR-08 | 04/nov/2026 | Chave `Run` do HKCU para iniciar com o Windows | Aceita · implementada |
| ADR-09 | 04/nov/2026 | Configurações em JSON com System.Text.Json source generation | Aceita · implementada |
| ADR-10 | 04/nov/2026 | Seleção de região sobre frame congelado, limitada a um monitor | Aceita · implementada |
| ADR-11 | 04/nov/2026 | Sem framework MVVM | Aceita · implementada |
| ADR-12 | 04/nov/2026 | Core sem dependência de Windows | Aceita · implementada |
| ADR-13 | 04/nov/2026 | Branding gerado de uma única fonte vetorial em Python | Aceita · implementada |
| ADR-14 | 04/nov/2026 | Interface só em pt-BR no MVP | Aceita · implementada |
| ADR-15 | 04/nov/2026 | Sem rede: nenhum backend, telemetria ou atualização automática | Aceita · implementada (por ausência) |

---

## ADR-01 — WinUI 3 unpackaged e self-contained como distribuição primária

- **Data:** 04/nov/2026 · **Status:** Aceita · implementada no `.csproj`

### Contexto

O SharpShot precisa rodar em qualquer Windows 10 2004+/11 "copiando uma pasta", sem instalador, sem conta de desenvolvedor na Store e sem exigir que o usuário instale o runtime do .NET ou do Windows App SDK. Ao mesmo tempo, a UI deve ser nativa e moderna (Fluent), e o app precisa de APIs WinRT (captura, imaging, notificações).

### Decisão

WinUI 3 (Windows App SDK 2.6.5) com `WindowsPackageType=None`, `SelfContained=true` e `WindowsAppSDKSelfContained=true`. O `dotnet publish` produz uma pasta com `SharpShot.exe`, o runtime do .NET e o runtime do Windows App SDK copiados. Sem trimming (`PublishTrimmed=false`) e sem ReadyToRun (`PublishReadyToRun=false`) no MVP, para evitar quebras em reflexão/XAML e manter o build reproduzível. MSIX fica como caminho **secundário**, documentado mas não usado ([`distribution/README.md`](distribution/README.md)).

### Alternativas consideradas

- **WPF** — maduro e sem dependência do Windows App SDK, mas visual datado sem bibliotecas de terceiros e sem acesso direto a `AppNotificationManager`/WinRT sem shims.
- **WinForms** — mesmo problema, com DPI ainda mais frágil.
- **MSIX como caminho principal** — exige certificado (ou Store), e a instalação "copiar pasta" era requisito.
- **Framework-dependent** — menor, mas exigiria o usuário instalar o runtime do Windows App SDK; "duplo clique e funciona" venceu.

### Consequências

Pasta publicada grande (dezenas de MB — tamanho exato **pendente de validação** no primeiro publish). Nenhum registro de sistema: tudo que o app escreve é em `HKCU` e `%LOCALAPPDATA%`. Recursos que só existem com identidade de pacote (`StartupTask`, alguns cenários de notificação) precisam de alternativa unpackaged (ADR-07, ADR-08).

Fonte: [`src/SharpShot.App/SharpShot.App.csproj`](../src/SharpShot.App/SharpShot.App.csproj) · [`DEPLOY.md`](DEPLOY.md).

## ADR-02 — Windows Graphics Capture em vez de GDI ou Desktop Duplication

- **Data:** 04/nov/2026 · **Status:** Aceita · implementada

### Contexto

A promessa do produto é "a captura é exatamente o que está na tela", em pixels físicos, incluindo aplicativos DirectX, jogos em janela e conteúdo acelerado — em monitores com DPI diferentes.

### Decisão

`Windows.Graphics.Capture` (WGC): `GraphicsCaptureItem` criado por `IGraphicsCaptureItemInterop.CreateForMonitor` a partir do `HMONITOR`, frame pool free-threaded em `B8G8R8A8UIntNormalized`, uma sessão por captura, cursor desligado, borda desligada quando `GraphicsCaptureAccess` permite. Combinado com DPI awareness `PerMonitorV2` no manifesto.

### Alternativas consideradas

- **GDI (`BitBlt`)** — retorna preto ou conteúdo parcial com apps DirectX/fullscreen; não respeita `WDA_EXCLUDEFROMCAPTURE`.
- **Desktop Duplication (DXGI)** — funciona, mas é uma API de *streaming* (frames "sem mudança", rotação, perda de acesso em troca de desktop) e exige muito mais interop para uma captura única.
- **`PrintWindow`/`DwmGetDxSharedSurface`** — não documentado ou limitado a janelas.

### Consequências

O frame chega sempre em SDR 8-bit, mesmo em monitor HDR ([`capture-engine/hdr.md`](capture-engine/hdr.md)) — limitação aceita e documentada. Exige sessão de desktop (`IsSupported`). Mínimo Windows 10 2004 (19041) pelas propriedades `IsCursorCaptureEnabled`/`IsBorderRequired` e pelo `WDA_EXCLUDEFROMCAPTURE`.

Fonte: [`capture-engine/README.md`](capture-engine/README.md) · [`src/SharpShot.Capture/Services/WindowsGraphicsCaptureService.cs`](../src/SharpShot.Capture/Services/WindowsGraphicsCaptureService.cs).

## ADR-03 — `SoftwareBitmap.CreateCopyFromSurfaceAsync` em vez de binding D3D11

- **Data:** 04/nov/2026 · **Status:** Aceita · implementada

### Contexto

Depois que o WGC entrega um `Direct3D11CaptureFrame`, os pixels precisam sair da GPU para um `byte[]`. O caminho clássico é `ID3D11Texture2D` → staging texture → `Map` → copiar linha a linha, o que exige um binding D3D11 (Vortice, SharpDX, Win2D ou CsWin32 com centenas de tipos gerados).

### Decisão

`SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface, BitmapAlphaMode.Ignore)` seguido de `CopyToBuffer`. Só o `IDirect3DDevice` é criado por interop manual (`D3D11CreateDevice` + `CreateDirect3D11DeviceFromDXGIDevice`), porque o frame pool exige um. Nenhum pacote de binding D3D entra no projeto.

### Alternativas consideradas

- **Vortice.Direct3D11** — completo e mantido, mas ~10 MB de dependência e API inteira para usar duas chamadas.
- **Win2D (`CanvasBitmap.CreateFromDirect3D11Surface`)** — traz o Win2D runtime inteiro.
- **CsWin32** — gera só o necessário, mas o código de staging/Map/stride continua manual.

### Consequências

Menos controle sobre a cópia (sem escolher staging/usage) e uma conversão interna do WinRT que não é visível. Para captura única isso não importa. Se um dia houver captura contínua (vídeo/GIF), revisitar.

Fonte: [`capture-engine/interop.md`](capture-engine/interop.md).

## ADR-04 — PNG sem perdas como único formato do MVP

- **Data:** 04/nov/2026 · **Status:** Aceita · implementada

### Contexto

A proposta de valor é "sem perda de qualidade". Oferecer JPEG abriria uma decisão de qualidade (quanto comprimir?) e um caminho em que o usuário perde pixels sem perceber.

### Decisão

`PngImageEncoder` via `Windows.Graphics.Imaging.BitmapEncoder` (WIC), entrada BGRA8, alpha ignorado, sem reamostragem, 96 DPI só como metadado. A UI mostra a linha fixa "PNG (sem perda de qualidade)" sem seletor. A interface `IImageEncoder` fica pronta para outros formatos.

### Alternativas consideradas

- **Seletor PNG/JPEG desde o MVP** — rejeitada: dilui a promessa e dobra a superfície de teste.
- **Biblioteca gerenciada (ImageSharp, SkiaSharp)** — rejeitada: dependência nova para fazer o que o WIC já faz nativamente.

### Consequências

Arquivos maiores que JPEG em conteúdo fotográfico. JPEG/WebP no roadmap, atrás da mesma interface.

Fonte: [`capture-engine/png.md`](capture-engine/png.md).

## ADR-05 — `RegisterHotKey` em vez de hook de teclado de baixo nível

- **Data:** 04/nov/2026 · **Status:** Aceita · implementada

### Contexto

Os atalhos `Ctrl+Shift+1` e `Ctrl+Shift+2` precisam funcionar com o SharpShot na bandeja, sem foco.

### Decisão

`RegisterHotKey(hwnd, id, modifiers | MOD_NOREPEAT, vk)` em uma janela message-only oculta que recebe `WM_HOTKEY`. Conflito (`ERROR_HOTKEY_ALREADY_REGISTERED`, 1409) é detectado no registro e mostrado na página Configurações via `InfoBar`. `HotkeyBinding`/`HotkeyModifiers` no Core usam os mesmos valores `MOD_*`.

### Alternativas consideradas

- **`SetWindowsHookEx(WH_KEYBOARD_LL)`** — vê todas as teclas (pode detectar o atalho mesmo "ocupado"), mas é um hook global: antivírus desconfiam, latência de teclado para o sistema inteiro, e precisa de thread com message loop dedicada.
- **Raw Input** — não recebe entrada sem foco em janela comum.

### Consequências

Se outro app já registrou o atalho, o SharpShot não consegue "roubá-lo" — o usuário precisa escolher outro. É o comportamento correto.

Fonte: [`architecture/README.md`](architecture/README.md) · [`src/SharpShot.Core/Models/HotkeyBinding.cs`](../src/SharpShot.Core/Models/HotkeyBinding.cs).

## ADR-06 — `Shell_NotifyIcon` e menu popup Win32 para a bandeja

- **Data:** 04/nov/2026 · **Status:** Aceita · implementada

### Contexto

WinUI 3 não tem controle de bandeja. Bibliotecas de terceiros (H.NotifyIcon etc.) existem, mas trazem dependência e abstrações que o MVP não precisa.

### Decisão

`Shell_NotifyIconW` (`NIM_ADD`/`NIM_MODIFY`/`NIM_DELETE`) na mesma janela message-only dos hotkeys, com `TrackPopupMenuEx` para o menu (Capturar tela inteira · Selecionar região · Abrir SharpShot · Abrir pasta de capturas · Sair). Readiciona o ícone ao receber a mensagem `TaskbarCreated` (Explorer reiniciado). Ícone de `assets/icons/tray.ico` (gerado, ADR-13).

### Alternativas consideradas

- **H.NotifyIcon.WinUI** — funciona, mas é uma dependência a mais para ~150 linhas de Win32.
- **Menu em XAML (`MenuFlyout`) ancorado na bandeja** — exige janela WinUI invisível posicionada; o menu Win32 é o que o Windows espera da bandeja.

### Consequências

O menu segue o tema do sistema (claro/escuro), não o tema do app. Aceito.

Fonte: [`architecture/README.md`](architecture/README.md).

## ADR-07 — `AppNotificationManager` para notificações

- **Data:** 04/nov/2026 · **Status:** Aceita · implementada

### Contexto

Após salvar, o usuário precisa de um toast "Captura salva com sucesso!" com o nome do arquivo e ações **Abrir pasta** / **Ver imagem**. O app é unpackaged (ADR-01).

### Decisão

`Microsoft.Windows.AppNotifications.AppNotificationManager` do Windows App SDK, que suporta apps unpackaged (registra um `AUMID` e um COM server para ativação). Se o registro falhar (política de notificações, sessão sem suporte), o app loga e segue sem toast — a captura nunca depende da notificação.

### Alternativas consideradas

- **Balão da bandeja (`NIF_INFO`)** — sem botões de ação, visual legado.
- **`ToastNotificationManager` (WinRT clássico)** — exige identidade de pacote.
- **Janela própria estilo toast** — reinventa o sistema e ignora o Foco Assistido.

### Consequências

O toast aparece na Central de Notificações do Windows; desinstalar manualmente deve chamar `Unregister()` ou limpar o registro ([`distribution/instalacao.md`](distribution/instalacao.md)).

Fonte: [`architecture/README.md`](architecture/README.md).

## ADR-08 — Chave `Run` do HKCU para iniciar com o Windows

- **Data:** 04/nov/2026 · **Status:** Aceita · implementada

### Contexto

"Iniciar com o Windows" é opção nas Configurações. App unpackaged não tem `StartupTask`.

### Decisão

Valor `SharpShot` em `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` apontando para `"<caminho>\SharpShot.exe" --minimized`. Criado/removido quando a opção muda; o caminho é reescrito a cada inicialização para acompanhar pastas movidas. `--minimized` inicia direto na bandeja. Em um futuro pacote MSIX, trocar por `StartupTask` no manifesto.

### Alternativas consideradas

- **Pasta Inicializar (atalho `.lnk`)** — visível e editável pelo usuário, mas exige criar atalhos via COM (`IShellLink`).
- **Tarefa Agendada** — exagero e pede privilégios em alguns cenários.

### Consequências

Só por usuário (HKCU) — correto para um app sem instalador. O Gerenciador de Tarefas mostra o item em "Aplicativos de inicialização" e permite desativar; o app deve ler o estado real da chave em vez de confiar só em `settings.json`.

Fonte: [`DATABASE.md`](DATABASE.md) · [`distribution/instalacao.md`](distribution/instalacao.md).

## ADR-09 — Configurações em JSON com System.Text.Json source generation

- **Data:** 04/nov/2026 · **Status:** Aceita · implementada

### Contexto

Preferências do usuário (atalhos, pasta, tema, flags) precisam sobreviver a reinstalações "copiar pasta", ser legíveis por humanos e não depender de reflexão (compatível com trimming futuro).

### Decisão

`AppSettings` serializado por `SettingsJsonContext` (`[JsonSerializable]`, camelCase, enums como string, indentado, comentários e vírgulas finais tolerados na leitura) em `%LOCALAPPDATA%\SharpShot\settings.json`. Escrita atômica (`settings.json.tmp` + `File.Move(overwrite)`); arquivo corrompido é renomeado para `settings.json.corrupt-<timestamp>` e os padrões assumem. `HotkeyBinding` tem conversor próprio (`"Ctrl+Shift+1"`). `SchemaVersion` para migrações futuras.

### Alternativas consideradas

- **Registro do Windows** — não é portátil, difícil de inspecionar e de fazer backup.
- **`ApplicationData.LocalSettings`** — exige identidade de pacote.
- **XML / INI** — sem ganho sobre JSON e sem source generation.
- **SQLite** — banco para 8 campos é exagero; fica para o histórico (roadmap).

### Consequências

Um único arquivo, editável à mão. Sem criptografia — não há segredo nas configurações.

Fonte: [`DATABASE.md`](DATABASE.md) · [`src/SharpShot.Storage/Configuration/SettingsService.cs`](../src/SharpShot.Storage/Configuration/SettingsService.cs).

## ADR-10 — Seleção de região sobre frame congelado, limitada a um monitor

- **Data:** 04/nov/2026 · **Status:** Aceita · implementada

### Contexto

O modo região precisa de um retângulo exato em pixels físicos, sem flicker e sem capturar o próprio overlay — em setups com vários monitores e DPIs diferentes.

### Decisão

Capturar primeiro (um frame por monitor), mostrar cada frame como fundo de um `RegionOverlayWindow` por monitor, e recortar em memória com `PixelBuffer.Crop`. A seleção é limitada ao monitor onde começou (`RegionMath.FromDrag` clampa ao `Bounds`). Toda a geometria (`RegionMath`, `ResizeHandle`) vive no Core, testável sem Windows.

### Alternativas consideradas

- **Overlay transparente + captura depois** — flicker ao esconder o overlay, conteúdo pode mudar, depende de `WDA_EXCLUDEFROMCAPTURE`.
- **Seleção atravessando monitores sobre um buffer composto do desktop virtual** — exige reamostrar quando os DPIs diferem, contra a promessa de fidelidade; lacunas entre monitores não alinhados.

### Consequências

Memória de N buffers durante a seleção; o cursor nunca aparece (frame congelado). Janela que atravessa dois monitores precisa de duas capturas.

Fonte: [`capture-engine/regiao.md`](capture-engine/regiao.md).

## ADR-11 — Sem framework MVVM

- **Data:** 04/nov/2026 · **Status:** Aceita · implementada

### Contexto

Três páginas (Início, Configurações, Sobre), um overlay e um punhado de comandos. `CommunityToolkit.Mvvm` resolveria, mas traz source generators, atributos e uma camada de convenção para um app pequeno.

### Decisão

`ObservableObject` (implementa `INotifyPropertyChanged` com `SetProperty`) e `RelayCommand`/`AsyncRelayCommand` escritos à mão em `SharpShot.App`. ViewModels recebem serviços por construtor via `Microsoft.Extensions.DependencyInjection`. Sem navigation service genérico: o `NavigationView` navega por tipo de página.

### Alternativas consideradas

- **CommunityToolkit.Mvvm** — ótimo, mas dependência + geradores para ~100 linhas de infraestrutura.
- **Code-behind puro** — torna a lógica de UI intestável; o meio-termo escolhido mantém ViewModels testáveis.

### Consequências

Se o app crescer (histórico, editor), reavaliar — migrar para o Toolkit é mecânico.

Fonte: [`ARCHITECTURE.md`](ARCHITECTURE.md).

## ADR-12 — Core sem dependência de Windows

- **Data:** 04/nov/2026 · **Status:** Aceita · implementada

### Contexto

Geometria de região, nomes de arquivo, validação de caminho, parse de atalhos e o modelo de configurações são lógica pura. Se dependessem de WinRT, os testes exigiriam sessão de desktop e TFM Windows.

### Decisão

`SharpShot.Core` tem TFM `net10.0` (sem `-windows`), zero `DllImport`, zero `using Windows.*`. Só `Microsoft.Extensions.Logging.Abstractions`. Interfaces (`IMonitorService`, `IScreenCaptureService`, `IImageEncoder`, `IScreenshotStorage`, `ISettingsService`) vivem aqui; implementações em `Capture` e `Storage`. `SharpShot.Storage` também é `net10.0` (só System.IO e STJ).

### Alternativas consideradas

- **Projeto único** — mais simples, mas sem fronteira: um `using Windows.Graphics` em `RegionMath` passaria despercebido.

### Consequências

`tests/SharpShot.Core.Tests` roda em qualquer runner .NET, sem Windows App SDK. Modelos como `PixelBuffer` precisam de métodos próprios (`Crop`, `ForceOpaque`) em vez de delegar ao WinRT — é o preço, e é pequeno.

Fonte: [`API.md`](API.md) · [`src/SharpShot.Core/SharpShot.Core.csproj`](../src/SharpShot.Core/SharpShot.Core.csproj).

## ADR-13 — Branding gerado de uma única fonte vetorial em Python

- **Data:** 04/nov/2026 · **Status:** Aceita · implementada

### Contexto

O app precisa de ícone `.ico` multi-tamanho, ícone de bandeja, logo horizontal claro/escuro, tiles MSIX (para o caminho secundário), logo de notificação e o path XAML do logo na barra de título — tudo consistente e regenerável.

### Decisão

`tools/branding/generate_branding.py` (Python 3.10+, Pillow) é a **única fonte** da geometria do logo. Gera `assets/branding/*.svg|png`, `assets/icons/*.ico|png`, `src/SharpShot.App/Assets/*` e `src/SharpShot.App/Components/LogoGeometry.cs` (marcado `<auto-generated>`). Artefatos gerados são versionados para o build não depender de Python.

### Alternativas consideradas

- **Arquivos de design (Figma/Illustrator) exportados à mão** — divergência inevitável entre os 20+ artefatos.
- **Gerar em C# no build** — acoplaria o build do app a uma ferramenta de design.

### Consequências

Editar o logo = editar o script e rodar. Nunca editar `LogoGeometry.cs` ou os PNGs à mão. Detalhes: [`design-system/branding.md`](design-system/branding.md) e [`design-system/assets.md`](design-system/assets.md).

Fonte: [`tools/branding/generate_branding.py`](../tools/branding/generate_branding.py).

## ADR-14 — Interface só em pt-BR no MVP

- **Data:** 04/nov/2026 · **Status:** Aceita · implementada

### Contexto

Público inicial é brasileiro; i18n completa (resw por idioma, `ResourceLoader`, textos de notificação e de menu Win32) é trabalho real e sem demanda confirmada.

### Decisão

Strings de UI em pt-BR como literais (`NeutralLanguage=pt-BR`; o analisador CA1303 está desligado no `.editorconfig` por isso). Mensagens de erro ao usuário vêm de uma tabela única em `SharpShot.App` (ver [`architecture/erros.md`](architecture/erros.md)). Mensagens de **exceção** e **logs** ficam em inglês (são para o desenvolvedor), exceto as de `PathValidator`, que já são o texto mostrado ao usuário.

### Alternativas consideradas

- **`.resw` desde o início** — custo de infraestrutura sem segundo idioma para justificar.

### Consequências

Quando a i18n entrar (roadmap), as literais precisam ser extraídas — tarefa mecânica, mas transversal.

Fonte: [`Code-Style.md`](Code-Style.md) · [`../Directory.Build.props`](../Directory.Build.props).

## ADR-15 — Sem rede: nenhum backend, telemetria ou atualização automática

- **Data:** 04/nov/2026 · **Status:** Aceita · implementada (por ausência)

### Contexto

Uma ferramenta de captura vê tudo o que está na tela. Confiança é o produto.

### Decisão

O SharpShot não abre conexão de rede: sem telemetria, sem verificação de versão, sem upload, sem crash reporting remoto. Logs ficam em `%LOCALAPPDATA%\SharpShot\logs`. Atualização = baixar a nova pasta no GitHub Releases. Nenhum pacote de rede entra em `Directory.Packages.props`.

### Alternativas consideradas

- **Verificação de atualização opcional** — pode voltar como opção *opt-in* explícita, com ADR próprio.

### Consequências

Nenhum dado sobre uso. Bugs dependem do usuário mandar o log. Aceito.

Fonte: [`../PROJECT_CONTEXT.md`](../PROJECT_CONTEXT.md) · [`../Directory.Packages.props`](../Directory.Packages.props).

---

Contexto: [`../PROJECT_CONTEXT.md`](../PROJECT_CONTEXT.md) · Arquitetura: [`ARCHITECTURE.md`](ARCHITECTURE.md) · Publicação: [`DEPLOY.md`](DEPLOY.md) · Histórico: [`CHANGELOG.md`](CHANGELOG.md)
