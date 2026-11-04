# Compatibilidade — SharpShot

> Em que Windows, arquiteturas e cenários o SharpShot roda, o que degrada e o que não funciona — com a origem de cada requisito no código.
> Fontes: `app.manifest`, `SharpShot.App.csproj`, `SharpShot.Capture.csproj` (verificados) · [`../capture-engine/limitacoes.md`](../capture-engine/limitacoes.md).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves · Matriz de testes: **nenhuma linha validada ainda**.

## Requisitos

| Requisito | Valor | Origem |
|-----------|-------|--------|
| Sistema | **Windows 10 versão 2004 (build 19041)** ou superior · **Windows 11** (qualquer versão) | `TargetPlatformMinVersion=10.0.19041.0` nos `.csproj`; `supportedOS` Win10/11 no `app.manifest` |
| Arquitetura | **x64** · **arm64** (nativo) | `RuntimeIdentifiers=win-x64;win-arm64`; plataformas x64/arm64 no `.slnx` |
| x86 (32 bits) | **não suportado** | Não há RID `win-x86`; Windows 10/11 x86 é residual |
| arm64 rodando o zip x64 | funciona por emulação, **não recomendado** | Use o zip `win-arm64` |
| Sessão | Área de trabalho interativa com DWM | WGC exige composição; `IsSupported` reporta |
| Runtime .NET / Windows App SDK | **nenhum** a instalar | self-contained |
| VC++ Redistributable | **verificar na pasta publicada** ([`README.md`](README.md#dependências-de-runtime)) | — |
| Privilégios | usuário comum | Nada em `HKLM`/`Program Files` |
| Disco | pasta do app (dezenas de MB — **pendente de medição**) + capturas | — |

## Por que 2004 (19041) e não 1903

| API usada | Disponível desde |
|-----------|------------------|
| `Windows.Graphics.Capture` (`GraphicsCaptureSession`, frame pool) | 1803/1903 |
| `GraphicsCaptureSession.IsCursorCaptureEnabled` | 2004 (19041) — o código checa com `ApiInformation.IsPropertyPresent` |
| `GraphicsCaptureSession.IsBorderRequired` + `GraphicsCaptureAccess` | 20H1+/Win11 — checado em runtime |
| `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)` | 2004 (19041) |
| Windows App SDK 2.6.x | exige 10.0.17763+ como mínimo próprio; o projeto fixa 19041 |

Em builds anteriores a 19041 o Windows recusa iniciar o executável ou o Windows App SDK falha no bootstrap. **Divergência conhecida:** a mensagem de `CaptureErrorKind.NotSupported` no código ainda diz "Windows 10 1903+"; o doc e o `.csproj` dizem 2004 — correção pendente no código.

## Matriz de cenários

| Cenário | Esperado | Status |
|---------|----------|--------|
| Windows 11 23H2/24H2, x64, 1 monitor 100 % | Tudo funciona; borda de captura desligada (`GraphicsCaptureAccess` → `Allowed`) | pendente de validação |
| Windows 11, 2–3 monitores com 100/125/150/200 % misturados | Dimensões físicas exatas por monitor; seleção por monitor | pendente de validação (CA-01, CA-03) |
| Windows 10 22H2 (19045), x64 | Funciona; `IsBorderRequired=false` **pode ser negado** em app unpackaged → borda amarela breve durante a captura (não aparece no PNG) | pendente de validação |
| Windows 10 2004–21H2 | Deve funcionar (mesmas APIs); sem teste previsto — versões fora de suporte da Microsoft | não testado |
| Windows 10 < 2004 (1909, 1903…) | **Não suportado** — não inicia | por desenho |
| Windows 11 arm64 (Snapdragon) | Funciona nativo com o zip `win-arm64`; captura via WARP se o driver não expuser D3D11 (improvável) | pendente de validação |
| VM (Hyper-V, VMware, VirtualBox) com sessão de console | Funciona; D3D11 pode cair para **WARP** (mais lento, funcional) | pendente de validação |
| RDP com desktop composto (Windows 10/11 modernos) | Funciona; captura é o que o DWM compõe na sessão remota | pendente de validação |
| RDP sem DWM / sessão de serviço / Session 0 | `IsSupported` falso → mensagem "Captura não disponível" | por desenho |
| Monitor HDR ligado | Captura mapeada para SDR; PNG 8-bit ([`../capture-engine/hdr.md`](../capture-engine/hdr.md)) | por desenho |
| Monitor em modo retrato | `rcMonitor` já rotacionado; funciona | pendente de validação |
| Monitor desconectado durante a seleção | Overlay daquele monitor fica órfão → fechar todos e cancelar (**tratamento no App pendente**) | em implementação |
| Conteúdo protegido (DRM, apps bancários, `WDA_EXCLUDEFROMCAPTURE`) | Área preta no PNG — regra do Windows | por desenho |
| Tela de bloqueio / UAC (desktop seguro) | Hotkey não chega; nada acontece | por desenho |
| Múltiplas GPUs (laptop híbrido) | `D3D11CreateDevice(adapter = null)` usa o adaptador padrão; WGC funciona independentemente de qual GPU renderiza o app | pendente de validação |
| Troca de GPU/driver durante uso (TDR) | `DXGI_ERROR_DEVICE_REMOVED` → device recriado, uma nova tentativa | implementado · pendente de validação |
| Conta sem permissão de escrita em Imagens (perfil corporativo) | Erro `PermissionDenied` com instrução para trocar a pasta | implementado (classificação) |
| Pasta Imagens redirecionada (OneDrive, servidor) | `GetFolderPath` honra; capturas vão para a pasta redirecionada | implementado |
| Windows com idioma ≠ pt-BR | UI continua em pt-BR (ADR-14); nomes de pasta do sistema vêm do Windows | por desenho |
| Alto contraste do Windows | WinUI aplica o tema de alto contraste; brushes da marca devem respeitar (**verificar**) | pendente de validação |
| Fontes: Windows 10 sem Segoe UI Variable / Segoe Fluent Icons | Fallback para Segoe UI / Segoe MDL2 Assets (o WinUI 3 já embute os ícones Fluent via `Microsoft.UI.Xaml` — **verificar** no Win10) | pendente de validação |

## O que não é suportado (e não está planejado)

- Windows 8.1/7, Windows 10 LTSB 2015/2016 (builds < 19041), Windows em x86.
- Windows Server sem Desktop Experience / Server Core.
- Linux/macOS (WinUI 3 e WGC são exclusivos do Windows).

## Como reportar incompatibilidade

Issue com: `winver` (versão e build), arquitetura, lista de monitores com escala, HDR sim/não, VM/RDP sim/não, GPU/driver, e o log de `%LOCALAPPDATA%\SharpShot\logs`. Ver [`../../CONTRIBUTING.md`](../../CONTRIBUTING.md).

---

Distribuição: [`README.md`](README.md) · Instalação: [`instalacao.md`](instalacao.md) · Limitações do motor: [`../capture-engine/limitacoes.md`](../capture-engine/limitacoes.md)
