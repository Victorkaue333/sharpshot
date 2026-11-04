# Limitações conhecidas — Capture Engine

> O que o motor de captura **não** faz, por decisão ou por limite da plataforma, e o que está no roadmap. Lista honesta: se algo aqui mudar, atualize também [`../PRD.md`](../PRD.md) e o [`README.md`](../../README.md).
> Fontes: código de `SharpShot.Capture` · [`../DECISIONS.md`](../DECISIONS.md) · [`../PRD.md`](../PRD.md).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Por decisão (MVP)

| Limitação | Motivo | Onde está decidido | Roadmap |
|-----------|--------|--------------------|---------|
| **Só PNG** | Promessa do produto é "sem perdas"; JPEG/WebP exigem decisão sobre qualidade | ADR-04 | JPEG/WebP |
| **Sem cursor na captura** | `IsCursorCaptureEnabled = false`; o cursor raramente é desejado em prints de documentação e aparece "na posição errada" no modo região (frame congelado) | `WindowsGraphicsCaptureService.ConfigureSession` | Captura com cursor (opção) |
| **Captura de um monitor por vez** | Desktop virtual com DPIs diferentes não tem grade comum; compor frames exigiria preencher lacunas | ADR-10 · [`dpi-e-monitores.md`](dpi-e-monitores.md) | — |
| **Seleção de região não atravessa monitores** | Mesmo motivo acima | ADR-10 · [`regiao.md`](regiao.md) | — |
| **Sem captura de janela** | `CreateForWindow` existe na interface COM, mas escolher a janela (hit-test, janelas minimizadas, DWM cloaked) é uma feature inteira | ADR-02 (escopo) | Captura de janela |
| **Sem área de transferência** | MVP salva em arquivo; copiar para o clipboard é uma linha, mas a UX (toast "copiado", formato CF_DIB vs PNG) ainda não foi decidida | [`../PRD.md`](../PRD.md) | Copiar para área de transferência |
| **Sem anotações / editor** | Fora do escopo "capturar e salvar" | [`../PRD.md`](../PRD.md) | Anotações |
| **Sem histórico de capturas** | Sem banco de dados no MVP; a pasta de capturas é o histórico | [`../DATABASE.md`](../DATABASE.md) | Histórico |
| **Sem captura com atraso (timer)** | Não pedido | — | avaliar |

## Por limite da plataforma

| Limitação | Detalhe | Mitigação |
|-----------|---------|-----------|
| **HDR vira SDR** | Pool em B8G8R8A8 → DWM entrega composição mapeada para SDR; PNG 8-bit | Documentado em [`hdr.md`](hdr.md); roadmap "HDR 16-bit" |
| **Exige sessão de desktop** | WGC não funciona em serviço, sessão 0, nem em algumas sessões RDP sem desktop composto | `IsSupported` é checado antes; erro `NotSupported` com mensagem clara |
| **Borda amarela de captura** | Windows 10 desenha uma borda em torno do item capturado. `IsBorderRequired = false` só funciona se `GraphicsCaptureAccess.RequestAccessAsync(Borderless)` devolver `Allowed` — em app **unpackaged** isso depende do build do Windows (disponível no Windows 11; no Windows 10 20H1+ a API existe, mas a permissão pode ser negada) | Captura única: a borda, se aparecer, some em um frame; o frame capturado não a contém (a borda é desenhada sobre a composição, fora do item). **Pendente de validação** em Windows 10 |
| **Conteúdo protegido (DRM)** | Janelas com `WDA_MONITOR`/`WDA_EXCLUDEFROMCAPTURE` (players com DRM, apps bancários) aparecem **pretas** — por design do Windows | Nenhuma; é a mesma regra que protege as janelas do próprio SharpShot |
| **Desktop seguro (UAC, tela de bloqueio)** | Não há composição acessível; a captura falha ou não dispara (hotkeys não chegam ao desktop seguro) | Nenhuma |
| **Primeiro frame pode demorar** | Pool recém-criado: o DWM precisa compor um frame para o item; em máquinas lentas ou WARP pode levar centenas de ms | Timeout de 5 s; sem medição formal (**pendente**) |
| **`ContentSize` ≠ tamanho do pool** | Mudança de resolução/rotação durante a captura | Buffer é recortado para `ContentSize` |
| **Monitores "fantasma"** | Adaptadores virtuais (ex.: alguns drivers de streaming) aparecem em `EnumDisplayMonitors` | Listados como qualquer outro; capturáveis |
| **Windows 10 < 2004 (build 19041)** | `TargetPlatformMinVersion` = 19041; builds anteriores nem iniciam o app | Requisito declarado em [`../distribution/compatibilidade.md`](../distribution/compatibilidade.md) |

> **Divergência a corrigir no código:** a mensagem de `CaptureErrorKind.NotSupported` diz "Windows 10 1903+", mas o mínimo real do app é 2004 (19041). Reportado ao lead.

## Conhecido e não tratado

- **Memória no modo região com muitos monitores**: N buffers BGRA8 vivem até o overlay fechar (3 monitores 4K ≈ 100 MB). Aceitável; não há streaming.
- **Nenhum aviso de HDR na UI**: o usuário não é informado de que a captura foi mapeada para SDR. Decisão de produto pendente.
- **Borderless em unpackaged**: ver tabela acima; precisa de teste em Windows 10 22H2 e Windows 11 23H2+ (**pendente de validação**).
- **Medições de desempenho** (tempo do hotkey ao arquivo, pico de memória): **não existem ainda**. Nenhum número deve ser publicado até haver medição reproduzível.

---

Visão geral: [`README.md`](README.md) · Requisitos: [`../project-context/requisitos.md`](../project-context/requisitos.md) · Roadmap: [`../PRD.md`](../PRD.md#roadmap-não-implementado)
