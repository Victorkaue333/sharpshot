# Contexto do projeto — SharpShot

> Visão do produto, problema, público-alvo, objetivos, escopo do MVP e o que fica de fora. Os requisitos numerados estão em [`requisitos.md`](requisitos.md) e os critérios de aceitação em [`criterios-de-aceitacao.md`](criterios-de-aceitacao.md). O resumo executivo está em [`../../PROJECT_CONTEXT.md`](../../PROJECT_CONTEXT.md) e o PRD em [`../PRD.md`](../PRD.md).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Ordem de leitura

1. Este arquivo — visão, problema, público, objetivos, escopo.
2. [`requisitos.md`](requisitos.md) — RF-01..16 e RNF-01..11, com prioridade e status.
3. [`criterios-de-aceitacao.md`](criterios-de-aceitacao.md) — CA-01..20, o que precisa passar para o MVP existir.

## Visão do produto

**SharpShot** é uma ferramenta nativa de captura de tela para Windows 10/11 que entrega a foto exata do que está na tela — em PNG sem perdas, em pixels físicos — com um atalho de teclado, e nada mais. É leve, vive na bandeja, não tem conta, não tem nuvem, não tem telemetria, e se instala copiando uma pasta.

Frase-guia: **"Capture a tela exatamente como ela é."**

## O problema

| Sintoma | Causa | Custo para quem usa |
|---------|-------|---------------------|
| Print sai "borrado" ou com tamanho diferente do monitor | Ferramenta captura em DIP ou reamostra para o DPI primário | Documentação com pixels errados; bug report que não reproduz |
| Janela de jogo/app DirectX sai preta | Captura via GDI (`BitBlt`) não vê composição acelerada | Impossível documentar esses apps |
| Arquivo salvo como JPEG sem avisar | Padrão "menor arquivo" da ferramenta | Artefatos em texto e bordas; perda irreversível |
| Dez cliques para salvar um print | Fluxo clipboard → colar → salvar como | Dezenas de vezes por dia, vira minutos |
| Nome `Captura de tela (37).png` | Numeração sequencial | Impossível ordenar por data, colisões ao mover |
| Ferramenta "vê" a tela e tem telemetria/nuvem | Modelo de negócio da ferramenta | Desconfiança; proibido em ambientes sensíveis |
| Precisa de instalador/admin | Pacote tradicional | Não roda em máquina corporativa travada |

## Público-alvo

| Persona | Peso | Necessidade | Cenário típico |
|---------|------|-------------|----------------|
| **Desenvolvedor / QA** | alto | Print fiel de UI, DPI exato, nome ordenável | 2–3 monitores com escalas diferentes; apps DirectX; dezenas de prints por dia para PR e bug |
| **Redator técnico / suporte** | alto | Recorte rápido de região, sem cursor, sem editor no caminho | Atalho → arrasta → `Enter` → toast → "Abrir pasta" |
| **Usuário comum (pt-BR)** | médio | Substituir o `PrtScn` por "salvar direto" | Instala, deixa na bandeja, esquece que existe |
| **Usuário sensível a privacidade** | médio | Garantia verificável de zero rede | Lê o código, confere que não há pacote de rede |

Não é público-alvo no MVP: quem precisa de editor/anotações, gravação de vídeo, OCR ou compartilhamento em nuvem.

## Objetivos

| # | Objetivo | Como se mede |
|---|----------|--------------|
| O1 | Fidelidade total | Round-trip PNG byte-idêntico (CA-02); dimensões físicas exatas em setup multi-DPI (CA-01, CA-03) |
| O2 | Nunca perder uma captura | Sem sobrescrita (CA-05); erros de gravação classificados e explicados (CA-08) |
| O3 | Invisível até ser chamado | Bandeja, atalhos, iniciar com o Windows, instância única (CA-09..CA-12) |
| O4 | Sem instalação, sem admin | Pasta publicada roda em VM limpa (CA-20); só `HKCU` e `%LOCALAPPDATA%` tocados |
| O5 | Base de engenharia sólida | `Core` sem Windows e coberto por testes; CI verde (CA-19) |
| O6 | Privacidade verificável | Nenhum pacote de rede; nenhuma conexão (RNF-04) |

## Escopo do MVP (0.1.0)

### Entra

- Captura de **tela inteira** (monitor sob o cursor) por atalho, card da Home e menu da bandeja.
- Captura de **região** com overlay por monitor sobre frame congelado; um monitor por seleção.
- **PNG sem perdas**, pasta configurável, nome `SharpShot_<timestamp>.png`, nunca sobrescreve.
- **Notificação** com nome do arquivo e ações Abrir pasta / Ver imagem (desligável).
- **Bandeja** com menu de 5 itens; fechar minimiza para a bandeja (configurável); Sair encerra limpo.
- **Atalhos configuráveis** com Restaurar padrões e detecção de conflito.
- **Iniciar com o Windows** (`--minimized`).
- **Tema** Sistema / Claro / Escuro.
- **Instância única**, **logs locais**, página **Sobre**.
- **x64** (arm64 em versão futura), Windows 10 2004+ e 11, self-contained.
- UI em **pt-BR**.

### Roadmap (não implementado)

Histórico de capturas · captura de janela · captura com cursor · JPEG/WebP · área de transferência · anotações · i18n · MSIX/Store · HDR 16-bit. Detalhe em [`../PRD.md`](../PRD.md#roadmap-não-implementado).

### Fora de escopo

Backend · API REST · banco de dados · autenticação/conta · telemetria · sincronização/nuvem · gravação de vídeo/GIF · OCR · macOS/Linux. Entrar com qualquer um deles exige ADR em [`../DECISIONS.md`](../DECISIONS.md).

## Restrições

| Restrição | Origem | Efeito |
|-----------|--------|--------|
| Solo dev | realidade do projeto | Simplicidade vence; sem framework além do necessário |
| Qualidade de imagem inegociável | [`../../CLAUDE.md`](../../CLAUDE.md) | Qualquer reamostragem/conversão com perdas por padrão é rejeitada |
| Sem dependência nativa além do Windows App SDK | ADR-03 | Interop à mão; zero Vortice/Win2D |
| Sem rede | ADR-15 | Sem atualização automática; release manual no GitHub |
| Windows 10 2004+ | `TargetPlatformMinVersion` 19041 | APIs de WGC (`IsBorderRequired`, `IsCursorCaptureEnabled`) e `WDA_EXCLUDEFROMCAPTURE` disponíveis |
| Branding de fonte única | ADR-13 | Logo/ícones só pelo script Python |
| Honestidade na doc | [`../../CLAUDE.md`](../../CLAUDE.md) | Nada "validado" sem execução |

## Estado em 04/nov/2026

| Área | Status |
|------|--------|
| `SharpShot.Core` (modelos, interfaces, geometria, nomes, validação) | implementado |
| `SharpShot.Capture` (WGC, D3D, monitores, DwmFlush, PNG) | implementado |
| `SharpShot.Storage` (settings.json, gravação) | implementado |
| `SharpShot.App` (UI, hotkeys, bandeja, toasts, overlay, DI) | **em implementação** (esqueleto: `Program.Main`, `App.xaml`, `MainWindow` placeholder, manifesto, `.csproj` unpackaged) |
| Testes (`tests/`) | **planejados** — só `.csproj` |
| CI (`.github/workflows/build.yml`) | a ser escrito pelo lead |
| Branding (`tools/branding/`, `assets/`) | implementado (artefatos gerados) |
| Documentação | esta versão |
| Release | nenhuma |

---

Requisitos: [`requisitos.md`](requisitos.md) · Critérios: [`criterios-de-aceitacao.md`](criterios-de-aceitacao.md) · PRD: [`../PRD.md`](../PRD.md) · Índice: [`../INDEX.md`](../INDEX.md)
