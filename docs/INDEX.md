# docs/ — Índice

> Mapa da documentação do SharpShot. A fonte da verdade detalhada está em cada arquivo; em caso de divergência com o código, **o código prevalece** e o doc precisa ser corrigido. Legenda de status usada em todos os docs: **implementado** (verificado no código) · **pendente de validação** (existe, não foi executado) · **roadmap**.
> Última atualização: 04/nov/2026 *(documentação sincronizada com a implementação completa; 320 testes verdes; pacote 0.1.0 gerado localmente, release no GitHub ainda não publicada)*

## Estrutura

```text
docs/
├── INDEX.md                       ← você está aqui
│   ┄ canônicos (camada fina e autoritativa) ┄
├── PRD.md                         requisitos do produto (resumo), fluxos, critérios de aceite, roadmap
├── ARCHITECTURE.md                arquitetura, projetos, integrações com o sistema, decisões-chave
├── API.md                         contratos internos (interfaces do Core ⇄ Capture/Storage/App) — não há HTTP
├── DATABASE.md                    persistência local (settings.json campo a campo, capturas, logs, registro) — não há banco
├── DEPLOY.md                      publicação (dotnet publish + GitHub Release) e CI
├── ENVIRONMENT.md                 ambiente de build e execução (SDKs, propriedades, pastas) — não há .env
├── DECISIONS.md                   ADR-01..15 (registro de decisões)
├── CHANGELOG.md                   histórico por versão, com o porquê
├── DESIGN.md                      guia rápido de design para quem escreve tela
├── Code-Style.md                  estilo de código (espelha o .editorconfig)
│   ┄ detalhamento ┄
├── project-context/               visão, problema, público, objetivos, RF/RNF, critérios de aceitação
├── architecture/                  responsabilidades, NuGet, fluxos (sequência), recursos nativos, erros
├── capture-engine/                WGC, interop, região, DPI/monitores, SDR/HDR, PNG, limitações
├── development/                   setup, Visual Studio, comandos, debug
├── distribution/                  publicação/empacotamento, instalação/desinstalação, compatibilidade
└── design-system/                 cores, temas, tipografia, espaçamento, componentes, estados, ícones · branding, assets
```

Raiz do repositório: [`../README.md`](../README.md) · [`../PROJECT_CONTEXT.md`](../PROJECT_CONTEXT.md) · [`../CLAUDE.md`](../CLAUDE.md) · [`../CONTRIBUTING.md`](../CONTRIBUTING.md) · [`../LICENSE`](../LICENSE).

## Ordem de leitura

| Perfil | Sequência |
|--------|-----------|
| **Qualquer pessoa** | [`../README.md`](../README.md) → [`../PROJECT_CONTEXT.md`](../PROJECT_CONTEXT.md) → [`PRD.md`](PRD.md) |
| **Desenvolvedor novo** | acima → [`ARCHITECTURE.md`](ARCHITECTURE.md) → [`capture-engine/README.md`](capture-engine/README.md) → [`DECISIONS.md`](DECISIONS.md) → [`development/README.md`](development/README.md) → [`Code-Style.md`](Code-Style.md) → [`../CONTRIBUTING.md`](../CONTRIBUTING.md) |
| **Quem mexe no motor** | [`capture-engine/`](capture-engine/README.md) inteiro → [`architecture/recursos.md`](architecture/recursos.md) → [`architecture/erros.md`](architecture/erros.md) → [`API.md`](API.md) |
| **Quem mexe na UI** | [`DESIGN.md`](DESIGN.md) → [`design-system/`](design-system/README.md) → [`architecture/fluxos.md`](architecture/fluxos.md) → [`architecture/erros.md`](architecture/erros.md) |
| **Quem publica** | [`DEPLOY.md`](DEPLOY.md) → [`distribution/`](distribution/README.md) → [`CHANGELOG.md`](CHANGELOG.md) |
| **Assistente de IA** | [`../CLAUDE.md`](../CLAUDE.md) primeiro, depois a sequência de desenvolvedor |

## Por tema

### Canônicos

Camada fina e autoritativa (11 docs em `docs/` + 3 na raiz). Resumem e linkam o detalhamento das subpastas em vez de duplicar.

- [`PRD.md`](PRD.md) — requisitos (resumo + link para os numerados), personas, fluxos, critérios de aceite, roadmap, fora de escopo
- [`ARCHITECTURE.md`](ARCHITECTURE.md) — diagrama de componentes, camadas, contratos, integrações com o sistema, decisões-chave, evolução
- [`API.md`](API.md) — **contratos internos**: 5 interfaces, implementações, modelos, enums, serviços estáticos, exceções (nome mantido do padrão; conceito adaptado)
- [`DATABASE.md`](DATABASE.md) — **persistência local**: `settings.json` esquema campo a campo, ciclo de vida, pasta de capturas e convenção de nomes, erros de gravação, logs, registro (nome mantido; conceito adaptado)
- [`DEPLOY.md`](DEPLOY.md) — pipeline do CI, publish por arquitetura, release manual, rollback, checklist
- [`ENVIRONMENT.md`](ENVIRONMENT.md) — SDKs e versões, propriedades de build, pastas de saída, o que o app lê do sistema, argumentos, CI
- [`DECISIONS.md`](DECISIONS.md) — **ADR-01..15** (WinUI unpackaged, WGC, SoftwareBitmap, PNG, RegisterHotKey, bandeja, toasts, chave Run, settings JSON, região congelada, sem MVVM, Core sem Windows, branding gerado, pt-BR, sem rede)
- [`CHANGELOG.md`](CHANGELOG.md) — *Não publicado* (todo o trabalho atual) e `0.1.0` como versão alvo
- [`DESIGN.md`](DESIGN.md) — princípios, fontes da verdade, cor, tipografia, forma, componentes, DPI, ícones, temas, acessibilidade, "nunca faça", divergências
- [`Code-Style.md`](Code-Style.md) — regra de ouro, portões, C# conforme `.editorconfig`, nomes, idioma, camadas, testes, comentários, git

### Contexto e requisitos

- [`project-context/README.md`](project-context/README.md) — visão, problema, público-alvo, objetivos, escopo do MVP, restrições, estado atual
- [`project-context/requisitos.md`](project-context/requisitos.md) — **RF-01..22** e **RNF-01..11** com prioridade, local no código, status e CA
- [`project-context/criterios-de-aceitacao.md`](project-context/criterios-de-aceitacao.md) — **CA-01..22**, automatizados ou manuais; 0 de 22 validados

### Arquitetura

- [`architecture/README.md`](architecture/README.md) — grafo de dependências, responsabilidades por projeto/pasta, NuGet real (inclusive transitivos), composição DI, modelo de threads, onde cada regra mora
- [`architecture/fluxos.md`](architecture/fluxos.md) — sequências: inicialização, tela inteira, região, mudança de atalho, erro, saída, instância única
- [`architecture/recursos.md`](architecture/recursos.md) — ciclo de vida de device D3D, frame pool, sessão, frames, bitmaps, buffers, handles Win32, arquivos
- [`architecture/erros.md`](architecture/erros.md) — `CaptureErrorKind`/`SaveErrorKind` → mensagem em pt-BR, ação, retry; outros erros do App; logs

### Motor de captura

- [`capture-engine/README.md`](capture-engine/README.md) — por que WGC, peças do motor, fluxo, exclusão das janelas, erros
- [`capture-engine/interop.md`](capture-engine/interop.md) — `DllImport` (user32/shcore/dwmapi/d3d11), `IGraphicsCaptureItemInterop`, criação do device, cópia do frame, checklist
- [`capture-engine/regiao.md`](capture-engine/regiao.md) — frame congelado, `RegionMath`, `PixelBuffer.Crop`, interface do overlay, um monitor por seleção
- [`capture-engine/dpi-e-monitores.md`](capture-engine/dpi-e-monitores.md) — PerMonitorV2, `MonitorInfo`, desktop virtual e coordenadas negativas, DIP↔pixel, escalas misturadas
- [`capture-engine/hdr.md`](capture-engine/hdr.md) — B8G8R8A8 → DWM mapeia HDR para SDR; PNG 8-bit; por que não FP16 agora
- [`capture-engine/png.md`](capture-engine/png.md) — WIC, garantias de "sem perdas", o que não é usado, teste de round-trip
- [`capture-engine/limitacoes.md`](capture-engine/limitacoes.md) — por decisão × por plataforma; conhecido e não tratado

### Desenvolvimento

- [`development/README.md`](development/README.md) — pré-requisitos, SDK via winget, Visual Studio, primeiro build, testes, executar, publicar, branding, problemas comuns
- [`development/comandos.md`](development/comandos.md) — todos os comandos com flags e saída esperada
- [`development/debug.md`](development/debug.md) — VS, logs, cenários difíceis (hotkey, overlay, WGC, bandeja, notificações, settings), regressão manual

### Distribuição

- [`distribution/README.md`](distribution/README.md) — unpackaged self-contained (principal) × MSIX (secundário), conteúdo da pasta, dependências de runtime (VC++ incluído no pacote), geração, checklist
- [`distribution/instalacao.md`](distribution/instalacao.md) — instalar, o que o app cria, iniciar com o Windows, atualizar, desinstalar sem resíduo, portátil, multiusuário
- [`distribution/compatibilidade.md`](distribution/compatibilidade.md) — Windows 10 2004+/11, x64 (arm64 futuro), por que 19041, matriz de cenários

### Design system

- [`design-system/README.md`](design-system/README.md) — ordem de leitura, princípios, mockups de referência, fonte executável, lacunas
- [`design-system/cores.md`](design-system/cores.md) — paleta, luminância e contraste calculados, semântica, tokens, nunca
- [`design-system/temas.md`](design-system/temas.md) — Sistema/Claro/Escuro, tokens por tema, logo por tema, o que não segue o tema
- [`design-system/tipografia.md`](design-system/tipografia.md) — Segoe UI Variable, escala do Fluent, numerais tabulares, idioma
- [`design-system/espacamento.md`](design-system/espacamento.md) — grade de 4, raio, elevação, dimensões, responsividade, medidas do overlay
- [`design-system/componentes.md`](design-system/componentes.md) — barra de título, shell, Home, Configurações, Sobre, overlay, bandeja, toast, componentes reutilizáveis
- [`design-system/estados.md`](design-system/estados.md) — controles, capturando/sucesso/erro/aviso/vazio/indisponível, overlay, foco, movimento
- [`design-system/icones.md`](design-system/icones.md) — Segoe Fluent Icons por ação, ícones gerados, logo na UI, cursores
- [`design-system/branding.md`](design-system/branding.md) · [`design-system/assets.md`](design-system/assets.md) — marca e inventário de assets *(mantidos pelo agente de branding)*
- [`design-system/palette.md`](design-system/palette.md) — ponteiro para `cores.md` (nome citado por `generate_branding.py`)

## Fora de `docs/`

| Onde | O quê |
|------|-------|
| `public/assets/` | **Mockups** de branding e telas (não são capturas do app) |
| `assets/branding/`, `assets/icons/` | Logo e ícones gerados pelo script |
| `tools/branding/generate_branding.py` | Fonte única do branding |
| `.github/workflows/build.yml` | CI (escrito pelo lead) |
| `src/`, `tests/` | Código — comentários XML no `Core` são o contrato; ver [`API.md`](API.md) |

## Lacunas da documentação (honestas)

- Nenhum número de desempenho, tamanho de pasta publicada ou resultado de teste: **não existem medições ainda**.
- Em caso de divergência entre um doc e o código do `App`, o código prevalece e o doc deve ser corrigido.
- `design-system/branding.md` e `assets.md` pertencem ao agente de branding — conferir existência.
- Capturas reais da UI (tema escuro) foram usadas na validação de 04/nov/2026; os mockups em `public/assets/` seguem como referência de design.
