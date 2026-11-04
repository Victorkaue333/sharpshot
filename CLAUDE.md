# CLAUDE.md — regras para IA neste repositório

> Camada **autoritativa e concisa** de contexto para qualquer assistente de IA (Claude Code, agentes, subagentes) que atue no SharpShot. Leia primeiro em toda sessão.
> Fontes: [`README.md`](README.md) · [`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md) · [`docs/INDEX.md`](docs/INDEX.md) · [`docs/DECISIONS.md`](docs/DECISIONS.md).
> Última atualização: 04/nov/2026.

## 1. Uso de modelos (custo) — regra do dono do projeto

- **O modelo mais caro (Fable) só pensa.** Use-o apenas para arquitetura complexa, decisões difíceis, revisão final e orientação de outros agentes.
- **Implementação, documentação, testes, refatorações mecânicas, geração de assets e tarefas repetitivas vão para modelos mais baratos** (Opus; Haiku para o trivial). Ao delegar com a ferramenta de agentes, passe explicitamente `model: "opus"` ou `model: "haiku"` — nunca herde o Fable para trabalho simples.
- Padrão: **o caro planeja e revisa, os baratos executam.** Briefings curtos e objetivos; sem releituras desnecessárias de arquivos; sem saídas longas.

## 2. O projeto em uma linha

SharpShot é um utilitário nativo para Windows 10/11 (C#, .NET 10, WinUI 3 / Windows App SDK 2.6.5, Windows Graphics Capture) que captura a tela inteira ou uma região e salva PNG sem perdas. Sem backend, sem banco, sem rede, sem telemetria.

## 3. Estrutura

```text
src/SharpShot.Core      lógica pura (modelos, interfaces, RegionMath, FileNaming, PathValidator) — sem dependência de Windows
src/SharpShot.Capture   Windows Graphics Capture, interop Win32/COM, MonitorService, PngImageEncoder
src/SharpShot.Storage   settings.json (%LOCALAPPDATA%\SharpShot) e gravação dos PNGs
src/SharpShot.App       WinUI 3: janela, páginas, overlay de região, atalhos globais, bandeja, notificações, DI
tests/                  xUnit (Core.Tests, Capture.Tests)
tools/branding/         gerador Python dos assets (fonte única do logotipo)
assets/ · public/       assets gerados · mockups de referência (não são capturas do app)
docs/                   documentação canônica (pt-BR) — mapa em docs/INDEX.md
```

## 4. Comandos

```bash
dotnet restore SharpShot.slnx
dotnet build SharpShot.slnx -c Debug -p:Platform=x64
dotnet test tests/SharpShot.Core.Tests -c Debug -p:Platform=x64
dotnet test tests/SharpShot.Capture.Tests -c Debug -p:Platform=x64
dotnet publish src/SharpShot.App/SharpShot.App.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained -o artifacts/publish/win-x64
python tools/branding/generate_branding.py
```

Nunca rode dois `dotnet build`/`test` ao mesmo tempo no mesmo checkout (agentes em paralelo corrompem `obj/`). Saída do build Debug: `src\SharpShot.App\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\SharpShot.exe`.

## 5. Regras fixas

- **Git:** nunca `git add`, `git commit`, `git push`, tags, branches, releases ou comandos destrutivos sem pedido explícito do dono. Deixe tudo na árvore de trabalho.
- **Qualidade de imagem é prioridade:** PNG sem perdas, resolução nativa, sem redimensionar, sem filtros, alpha ignorado (capturas são opacas), DPI tratado em pixels físicos.
- **Captura:** Windows Graphics Capture (B8G8R8A8 UNORM → SDR). HDR não é preservado — documente, não prometa.
- **Lógica de captura nunca na interface.** Core sem WinRT; App orquestra via `ICaptureCoordinator`.
- **Sem dependências desnecessárias:** nada de MVVM frameworks, Vortice/Win2D, WebView, bibliotecas de UI de terceiros.
- **Sem controles falsos:** todo controle visível está ligado a uma funcionalidade real. Formato fixo PNG no MVP.
- **UI em pt-BR; README e docs em pt-BR; código, comentários e identificadores em inglês.**
- **Identidade visual:** Conceito A (S geométrico laranja + prata). Paleta `#0D1017 / #1B2230 / #FFAA35 / #FF7A00 / #F5F7FA / #94A3B8`. Segoe UI Variable. Nunca substituir por ícone genérico.
- **Assets:** só via `tools/branding/generate_branding.py`; não editar PNG/ICO à mão.
- **Honestidade:** não declarar validação sem evidência; marcar "pendente de validação" quando não executado.
- Nullable habilitado, `TreatWarningsAsErrors` desligado mas warnings devem ser tratados; sem TODOs críticos; sem código morto.
- **Erros sempre classificados** (`CaptureErrorKind`/`SaveErrorKind`) e com mensagem na tabela única de [`docs/architecture/erros.md`](docs/architecture/erros.md); nunca `COMException`/`IOException` crua na UI.
- **Docs no mesmo PR:** interface nova → `docs/API.md`; campo de `settings.json` → `docs/DATABASE.md`; `DllImport` → `docs/capture-engine/interop.md`; decisão → ADR novo (nunca renumerar); `docs/CHANGELOG.md` com o porquê. Estilo em [`docs/Code-Style.md`](docs/Code-Style.md).

## 6. Ordem de leitura

1. Este arquivo. 2. [`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md). 3. [`docs/INDEX.md`](docs/INDEX.md). 4. [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md). 5. [`docs/capture-engine/`](docs/capture-engine/README.md). 6. [`docs/DECISIONS.md`](docs/DECISIONS.md). 7. [`docs/API.md`](docs/API.md) e [`docs/DATABASE.md`](docs/DATABASE.md) antes de tocar em contratos ou persistência. 8. [`docs/Code-Style.md`](docs/Code-Style.md) antes de escrever código.
