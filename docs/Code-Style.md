# Code Style — SharpShot

Como o código deste repositório é escrito. Vale para gente e para agente de IA. Quando uma regra daqui
bate com uma configuração executável (`.editorconfig`, `Directory.Build.props`, analisadores), **a
configuração manda** e este arquivo é que está errado.

> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves
> Relacionados: [`../CLAUDE.md`](../CLAUDE.md) (decisões fixas e guardrails) · [`../CONTRIBUTING.md`](../CONTRIBUTING.md)
> (processo) · [`DESIGN.md`](DESIGN.md) (UI) · [`development/README.md`](development/README.md) (setup) ·
> [`API.md`](API.md) (contratos)

---

## 1. Regra de ouro

**Escreva como o código ao redor.** Antes de criar um arquivo, abra um vizinho do mesmo tipo (um serviço
de `Capture`, um modelo de `Core`) e siga nomes, estrutura, densidade de comentário e idioma.

Três regras que atravessam o repo inteiro:

1. **Nullable habilitado, zero supressão sem motivo.** `!` (null-forgiving) e `#pragma warning disable`
   só com comentário dizendo por quê na linha.
2. **Lógica de captura nunca na interface; Windows nunca no `Core`.** `SharpShot.Core` e
   `SharpShot.Storage` não têm `using Windows.*`, `DllImport` nem TFM `-windows` ([ADR-12](DECISIONS.md)).
3. **Honestidade.** Nada de "testado" sem execução; nada de controle de UI sem função; nada de número de
   desempenho sem medição. Dúvida vira "pendente de validação" no doc, nunca uma escolha silenciosa.

## 2. Portões

| Comando | O que garante | Roda no CI |
|---------|---------------|:----------:|
| `dotnet build SharpShot.slnx -c Debug -p:Platform=x64` | Compila com `AnalysisLevel=latest-recommended`; warnings dos analisadores aparecem (não falham o build — `TreatWarningsAsErrors` desligado — mas **devem ser tratados**) | ✅ |
| `dotnet test tests/SharpShot.Core.Tests -c Debug -p:Platform=x64` | xUnit sobre `Core` + `Storage` | ✅ |
| `dotnet test tests/SharpShot.Capture.Tests -c Debug -p:Platform=x64` | xUnit sobre `Capture` (integração pulada sem WGC) | ✅ |
| `dotnet publish …` | A pasta self-contained é gerada | ✅ |
| `dotnet format --verify-no-changes SharpShot.slnx` | Formatação conforme `.editorconfig` | ⚠️ sugerido — **não configurado ainda** no `build.yml` |

O CI é `.github/workflows/build.yml` (escrito pelo lead). **Não rode dois builds ao mesmo tempo no mesmo
checkout** (agentes em paralelo): `obj/` corrompe.

> Falha de ambiente (SDK ausente, runner sem sessão de desktop) não se resolve mexendo no código. Separe
> as duas coisas no relato.

## 3. C# — o que o `.editorconfig` impõe

Base: `.editorconfig` na raiz (CRLF, UTF-8, 4 espaços; 2 em XML/XAML/`.csproj`/`.props`/JSON/YAML). Regras com severidade `warning` são as que o build mostra:

| Regra | Valor | Exemplo |
|-------|-------|---------|
| Namespace | **file-scoped** (`warning`) | `namespace SharpShot.Core.Models;` |
| Chaves | **sempre** (`csharp_prefer_braces = true:warning`) | `if (x) { return; }` — nunca `if (x) return;` |
| Estilo de chave | Allman (`new_line_before_open_brace = all`), `else`/`catch`/`finally` em linha nova | |
| `var` | **não** para tipos built-in (`int x = 0;`); sim quando o tipo é aparente ou em outros casos (`suggestion`) | `var buffer = PixelBuffer.Create(w, h);` |
| Tipos predefinidos | `int`, `string`, não `Int32`/`String` (`warning`) | |
| Campos privados | `_camelCase` (`warning`) | `private readonly ILogger<T> _logger;` |
| `readonly` | Campo que não muda é `readonly` (`warning`) | |
| `this.` | Não usar para campos | |
| Primary constructors | **não** (`csharp_style_prefer_primary_constructors = false:none`) — construtor explícito com `ArgumentNullException.ThrowIfNull` | ver qualquer serviço |
| `using` | `System.*` primeiro, sem grupos separados | |
| `using` declaration | Preferir `using var x = …;` | |
| Inicializadores de objeto/coleção, auto-properties | Preferir (`suggestion`) | `[1, 2, 3]`, `new() { X = 1 }` |

Analisadores desligados de propósito (e por quê):

| Regra | Motivo |
|-------|--------|
| `CA1848` (LoggerMessage delegates) | Volume de log não justifica source-gen de mensagens |
| `CA2007` (`ConfigureAwait`) | App de UI depende do contexto capturado; `Core`/`Storage` seguem a mesma regra por simplicidade |
| `CA1031` (catch genérico) | `catch (Exception)` é intencional nas **bordas** (storage, encoder, interop) — sempre com classificação em `*ErrorKind` |
| `CA1303` (literais de UI) | Strings de UI são literais pt-BR no MVP ([ADR-14](DECISIONS.md)) |

Outras convenções não verificadas por ferramenta:

- `sealed` em toda classe que não é desenhada para herança (é o padrão do repo).
- `record` para dados imutáveis; `readonly record struct` para valores pequenos (`PixelRectangle`).
- Enums com valor explícito no primeiro membro (`Unknown = 0`, `None = 0`) para serialização estável.
- Constantes de interop com o nome da API em PascalCase (`MonitorDefaultToNearest`, `DxgiErrorDeviceRemoved`) e o valor original em comentário quando não é óbvio.
- `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace` /
  `ArgumentOutOfRangeException.ThrowIfNegativeOrZero` na entrada de todo método público.
- `TimeProvider` injetável onde há tempo (`WindowsGraphicsCaptureService`) — testes controlam o relógio.
- Sem `async void` fora de handlers de evento de UI; sem `.Result`/`.Wait()` exceto onde documentado
  (`TryRequestBorderlessAccess` usa `GetAwaiter().GetResult()` por ser chamada única no setup da sessão).

## 4. Nomes

| Coisa | Convenção | Exemplo |
|-------|-----------|---------|
| Projeto / assembly | `SharpShot.<Camada>`; App publica como `SharpShot.exe` | `SharpShot.Capture` |
| Namespace | = pasta | `SharpShot.Capture.Interop` |
| Pastas de `Core` | `Models`, `Enums`, `Interfaces`, `Exceptions`, `Services` | |
| Pastas de `Capture` | `Interop`, `Graphics`, `Services`, `Encoding` | |
| Pastas de `Storage` | `Configuration`, `Services` | |
| Pastas do `App` (convenção) | `Views`, `ViewModels`, `Services`, `Components`, `Interop`, `Assets` | `Views/SettingsPage.xaml` |
| Interface | `I` + substantivo + papel | `IScreenCaptureService` |
| Implementação | nome da tecnologia + papel | `WindowsGraphicsCaptureService`, `PngImageEncoder` |
| Serviço estático puro | substantivo | `RegionMath`, `FileNaming`, `PathValidator` |
| Enum de erro | `<Domínio>ErrorKind` | `SaveErrorKind` |
| Exceção | `<Domínio>Exception` com `Kind` | `CaptureException` |
| Record de resultado | particípio/substantivo | `CapturedFrame`, `SavedScreenshot` |
| Teste | `<Classe>Tests.cs`, método `Metodo_Cenario_Resultado` | `FileNamingTests.MakeUnique_WhenExists_AppendsSuffix` |
| Arquivo XAML | `<Nome>Page.xaml`, `<Nome>Window.xaml` | `RegionOverlayWindow.xaml` |
| Recurso XAML | prefixo `SharpShot` | `SharpShotPrimaryBrush` |
| Arquivo gerado | cabeçalho `<auto-generated>` e nunca editado à mão | `LogoGeometry.cs` |

## 5. Idioma

| Onde | Idioma |
|------|--------|
| Identificadores, comentários de código, mensagens de exceção, logs | **inglês** |
| Strings de UI, toasts, menu da bandeja, `PathValidationResult.Error` (texto que o usuário vê) | **pt-BR** |
| Documentação (`docs/`, `README.md`, `CONTRIBUTING.md`), mensagens de commit | **pt-BR** |

`PathValidator` é a exceção consciente no `Core`: o texto já é a mensagem ao usuário. Toda outra mensagem ao usuário sai da tabela de [`architecture/erros.md`](architecture/erros.md) no App.

## 6. Camadas

### `Core` e `Storage` (`net10.0`)

- Zero WinRT, zero `DllImport`, zero `System.Drawing`.
- Interface nova entra em `Core/Interfaces` **e** em [`API.md`](API.md) no mesmo PR.
- Modelos recebem o que o WinRT faria por eles (`PixelBuffer.Crop`), não o contrário.

### `Capture` (`net10.0-windows10.0.26100.0`)

- Interop em `Interop/NativeMethods.cs` com a tabela de [`capture-engine/interop.md`](capture-engine/interop.md) atualizada.
- Toda falha nativa vira `CaptureException(Kind)`; `COMException` nunca sobe crua.
- Todo objeto COM/WinRT descartável em `using`; ponteiro bruto em `finally` com `Marshal.Release`. Regras em [`architecture/recursos.md`](architecture/recursos.md).
- `AllowUnsafeBlocks` está ligado, mas `unsafe` só com justificativa em comentário (hoje: nenhum).

### `App` (WinUI 3)

- `Program.Main` custom (já existe). Composição em `App.xaml.cs`: `ServiceCollection` → `BuildServiceProvider` → resolver `ICaptureCoordinator`, janela e serviços.
- Sem framework MVVM ([ADR-11](DECISIONS.md)): `ObservableObject` + `RelayCommand` próprios; ViewModel recebe interfaces do `Core` por construtor.
- Win32 do App (hotkeys, bandeja, afinidade, registro) em `App/Interop`, com a mesma disciplina de `Capture`.
- Thread de UI: `DispatcherQueue.TryEnqueue` para voltar à UI depois de `await` fora do contexto; nunca bloquear a UI com `.Result`.
- Nenhuma string de erro inline: `ErrorMessages.For(CaptureErrorKind)` / `For(SaveErrorKind)`.

## 7. Testes

| Projeto | Cobre | Ferramenta | Onde |
|---------|-------|------------|------|
| `tests/SharpShot.Core.Tests` | `Core` **e** `Storage` (o `.csproj` referencia os dois) | xUnit 2.9.3 + `Microsoft.NET.Test.Sdk` | `tests/SharpShot.Core.Tests/<Classe>Tests.cs` |
| `tests/SharpShot.Capture.Tests` | `Capture` | idem (TFM Windows) | `tests/SharpShot.Capture.Tests/<Classe>Tests.cs` |

- `Core.Tests` não pode exigir Windows: `SettingsService` recebe um caminho temporário pelo construtor alternativo; nada toca `%LOCALAPPDATA%` real.
- `Capture.Tests`: teste que precisa de WGC verifica `IsSupported` e usa `Skip` com motivo claro quando falso (runner de CI sem desktop composto).
- Bug corrigido ganha teste que falharia sem a correção. Teste cobre o caminho de erro (`SaveErrorKind` classificado, `CaptureErrorKind`).
- Sem pasta `__tests__`/`Unit/`: um arquivo por classe testada.
- **Status:** nenhum teste existe ainda — só os `.csproj` (04/nov/2026).

## 8. Comentários

Comentário é **curto e sobre o código, não sobre o trabalho**. Uma ou duas linhas. Comente só o que o código não diz sozinho: motivo de um workaround, invariante, unidade/formato, referência de API.

```csharp
// ✅ Bom: diz o porquê que a linha não mostra
// pHYs metadata only; it never changes pixels. 96 DPI keeps 1:1 viewing in most viewers.
public const double MetadataDpi = 96.0;

// ❌ Ruim: narra a investigação
// We tried 72 and 144 first but viewers scaled the image, so after testing we settled on 96.
public const double MetadataDpi = 96.0;
```

XML doc (`/// <summary>`) em tipos e membros públicos do `Core` (é o contrato); opcional em privados. O histórico vai no commit ou no [`CHANGELOG.md`](CHANGELOG.md), nunca no arquivo.

## 9. Formatação

- `.editorconfig` é a fonte; `dotnet format` aplica. CRLF, final de linha no fim do arquivo, sem espaço à direita (exceto `.md`).
- Não reformate arquivo inteiro num PR de feature: o diff mostra só a mudança.
- XAML: um atributo por linha quando passa de ~3 atributos; `x:Name` primeiro; recursos em `ResourceDictionary` separado por tema.

## 10. Git

### Commits

Feitos **só pelo mantenedor** (agentes de IA nunca commitam — [`../CLAUDE.md`](../CLAUDE.md)). Conventional Commits, **em pt-BR**, minúsculo, dizendo o comportamento que passa a valer:

```text
feat(capture): captura de monitor via windows graphics capture com fallback warp
feat(storage): settings.json atômico com backup de arquivo corrompido
fix(core): FromDrag limita a seleção ao monitor de origem
docs: adr-01..15 e documentação canônica em pt-br
chore(build): versões centrais de pacote em Directory.Packages.props
```

- Tipos: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `build`.
- Escopo = projeto ou área (`core`, `capture`, `storage`, `app`, `branding`, `ci`, `docs`).
- Um commit, uma mudança coesa. Quebra de contrato interno: `feat!:` + `BREAKING CHANGE:`.

### Branches e PRs

`main` é a única branch de longa duração. Trabalho em `feat/`, `fix/`, `docs/`, `chore/` + slug kebab-case em pt-BR sem acento. PR segue [`../CONTRIBUTING.md`](../CONTRIBUTING.md): CI verde, docs afetadas no mesmo PR, `CHANGELOG.md` em *Não publicado*, ADR se houver decisão nova.

## 11. Regras escritas que nenhuma ferramenta verifica

Hoje dependem de review. Automatizar cada uma é trabalho em aberto:

- [ ] TODO: `dotnet format --verify-no-changes` não está no CI.
- [ ] TODO: nada impede `using Windows.*` em `Core`/`Storage` além do TFM (que já falha a compilação para WinRT, mas não para `System.Drawing.Common`, por exemplo). Avaliar um teste de arquitetura (reflexão sobre referências do assembly).
- [ ] TODO: `TreatWarningsAsErrors` desligado; warnings de analisador podem acumular sem ninguém ver. Avaliar ligar por projeto quando o App estabilizar.
- [ ] TODO: `.editorconfig` pede CRLF, mas os `.cs` atuais estão em LF (verificado em 04/nov/2026 com `file`); os `.md` estão em CRLF. Decidir e normalizar (`.gitattributes` com `* text=auto eol=crlf`?).
- [ ] TODO: a mensagem de `CaptureErrorKind.NotSupported` cita "Windows 10 1903+"; o mínimo do app é 2004 (19041).
- [ ] TODO: sem commitlint, sem hook de pre-commit.
