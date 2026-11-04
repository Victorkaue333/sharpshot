# PROJECT_CONTEXT — SharpShot

> Contexto de **negócio e problema**: por que o SharpShot existe, para quem, o que é sucesso e o que fica de fora. Camada fina; o produto está em [`docs/PRD.md`](docs/PRD.md), os requisitos numerados em [`docs/project-context/`](docs/project-context/README.md) e as decisões técnicas em [`docs/DECISIONS.md`](docs/DECISIONS.md).
> Mantenedor: Victor Alves · Licença: MIT · Repositório: [github.com/Victorkaue333/sharpshot](https://github.com/Victorkaue333/sharpshot)
> Última atualização: 04/nov/2026

## O problema

Tirar um print no Windows deveria ser trivial, e na prática envolve escolher entre:

- **`PrtScn` / `Win+Shift+S`** — vai para a área de transferência (ou para um fluxo de salvar com vários cliques); nome de arquivo genérico; o comportamento em múltiplos monitores com escalas diferentes depende da ferramenta e do build do Windows.
- **Ferramentas completas de captura** — resolvem, mas trazem editor, gravador, conta, nuvem, atualização automática, telemetria e, às vezes, conversão silenciosa para JPEG.
- **Capturas de utilitários baseados em GDI** — retornam preto em janelas DirectX/fullscreen e perdem precisão em DPI alto.

Quem tira dezenas de prints por dia para documentação, bug report e suporte quer o oposto: **um atalho, um arquivo PNG fiel na pasta certa, nome ordenável, zero surpresa**. E quem se preocupa com o que uma ferramenta de captura "vê" quer garantia de que nada sai da máquina.

## A proposta

**SharpShot** é um utilitário nativo (C#, .NET 10, WinUI 3) para Windows 10/11 que captura a tela inteira ou uma região e salva em **PNG sem perdas**, em pixels físicos, com um atalho global — e para por aí.

| Promessa | O que significa na prática |
|----------|----------------------------|
| **Fidelidade** | O PNG é byte a byte o frame que o Windows compôs: sem reamostragem, sem filtro, sem compressão com perdas. Monitores com 100 %/125 %/150 % lado a lado saem nas dimensões físicas exatas |
| **Funciona com tudo** | Windows Graphics Capture lê a composição do DWM: apps DirectX, jogos em janela, vídeo acelerado |
| **O que você vê é o que sai** | Na seleção de região, a tela é congelada antes de você desenhar o retângulo |
| **Previsível** | `SharpShot_2026-11-04_14-32-07-583.png` na pasta escolhida; nunca sobrescreve |
| **Invisível até ser chamado** | Vive na bandeja; `Ctrl+Shift+1` / `Ctrl+Shift+2`; toast com "Abrir pasta" |
| **Privado** | Nenhuma conexão de rede, nenhuma telemetria, nenhuma conta. Código aberto sob MIT |
| **Sem instalador** | Copie a pasta e execute; desinstale apagando a pasta |

## Público-alvo

| Quem | Dor | O que o SharpShot resolve |
|------|-----|---------------------------|
| Desenvolvedores, QA, redatores técnicos | Prints fiéis de UI para PR, bug e docs; setups com vários monitores e DPIs | Fidelidade + DPI exato + nome ordenável |
| Suporte e atendimento | Volume alto de recortes rápidos sem editor no caminho | Atalho + região congelada + toast com ações |
| Usuário comum em pt-BR | Quer que o `PrtScn` "salve direto" sem configurar nada | Padrões sensatos, UI em português, zero configuração obrigatória |
| Quem não confia em ferramenta de captura | Medo de telemetria e upload | Zero rede por desenho (ADR-15), código aberto |

## Objetivos do MVP (0.1.0)

1. Capturar tela inteira e região com fidelidade total, em qualquer combinação de monitores/DPI suportada pelo Windows 10 2004+/11.
2. Nunca perder uma captura: sem sobrescrita, erros de gravação explicados em pt-BR com ação sugerida.
3. Ser invisível até ser chamado: bandeja, atalhos globais, iniciar com o Windows, instância única.
4. Rodar sem instalação e sem privilégios de administrador.
5. Ter a lógica testável sem sessão de desktop (`Core` sem Windows) e um CI que compila, testa e publica.

## O que é sucesso

Sucesso do MVP é **qualitativo e verificável**, não métrica de uso (não há telemetria para medir):

- Os critérios de aceitação de [`docs/project-context/criterios-de-aceitacao.md`](docs/project-context/criterios-de-aceitacao.md) passam em uma máquina com dois monitores de escalas diferentes e em uma VM limpa.
- Round-trip PNG byte-idêntico comprovado por teste automatizado.
- Um usuário que nunca viu o app consegue, sem ler nada: tirar um print com `Ctrl+Shift+1`, achar o arquivo pelo toast e mudar a pasta nas Configurações.
- Nenhum item da lista "fora de escopo" entrou.

Status em 04/nov/2026: todos os projetos implementados e commitados; 320 testes automatizados verdes; critérios de captura (tela inteira e região), notificação, bandeja e configurações **validados na máquina do autor** — DPI ≠ 100 %, HDR, Windows 10 e menu da bandeja seguem **pendentes de validação** (ver `docs/project-context/criterios-de-aceitacao.md`).

## Escopo do MVP

**Entra:** tela inteira · região (um monitor por seleção) · PNG · pasta configurável · atalhos configuráveis com detecção de conflito · bandeja com menu · toast com ações · iniciar com o Windows · minimizar para a bandeja · tema Sistema/Claro/Escuro · página Sobre · logs locais · x64 · pt-BR.

**Roadmap (não implementado):** histórico de capturas · captura de janela · captura com cursor · JPEG/WebP · área de transferência · anotações · i18n · MSIX/Store · HDR 16-bit.

**Fora de escopo (por decisão, não por falta de tempo):** backend, API REST, banco de dados, autenticação/conta, telemetria, sincronização/nuvem, gravação de vídeo/GIF, OCR, outros sistemas operacionais.

## Restrições e princípios

- **Solo dev, projeto aberto.** Simplicidade vence: sem framework MVVM, sem binding D3D de terceiros, sem biblioteca de UI ([`docs/DECISIONS.md`](docs/DECISIONS.md) ADR-03, ADR-11).
- **Qualidade de imagem é inegociável.** Qualquer feature que reamostre, converta cor ou comprima com perdas por padrão é rejeitada.
- **Honestidade na documentação.** Nada é declarado validado sem execução; HDR não é preservado e isso está escrito, não escondido.
- **Identidade própria.** Conceito A (S geométrico laranja + prata), gerado de uma fonte única; nunca um ícone genérico de câmera.
- **Licença MIT.** Qualquer pessoa pode usar, modificar e redistribuir, inclusive comercialmente, mantendo o aviso de copyright.

## Riscos conhecidos

| Risco | Mitigação |
|-------|-----------|
| WGC não suportado na sessão (RDP sem desktop composto, sessão de serviço) | `IsSupported` checado antes; erro claro; não há fallback GDI por decisão (fidelidade) |
| Borda amarela do WGC em app unpackaged no Windows 10 | `IsBorderRequired = false` quando permitido; **pendente de validação** em Windows 10 |
| Atalho padrão já usado por outro app | Conflito detectado e mostrado; usuário escolhe outro |
| Pasta Imagens redirecionada (OneDrive) | `GetFolderPath` honra o redirecionamento; fallback para `%USERPROFILE%\Pictures` |
| Tamanho da pasta publicada (self-contained) | Aceito; medir no primeiro publish e registrar em [`docs/distribution/README.md`](docs/distribution/README.md) |
| Sem telemetria = sem visibilidade de bugs em campo | Logs locais claros + issue template pedindo versão do Windows, DPI e monitores |

---

Produto: [`docs/PRD.md`](docs/PRD.md) · Requisitos: [`docs/project-context/requisitos.md`](docs/project-context/requisitos.md) · Arquitetura: [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) · Regras para IA: [`CLAUDE.md`](CLAUDE.md)
