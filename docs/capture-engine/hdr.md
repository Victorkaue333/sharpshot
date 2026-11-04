# SDR e HDR — Capture Engine

> O que acontece com a cor quando o monitor está em HDR, e por que o PNG do SharpShot é sempre 8-bit SDR. Este arquivo é referenciado pelo código (`WindowsGraphicsCaptureService.cs`).
> Fontes: [`src/SharpShot.Capture/Services/WindowsGraphicsCaptureService.cs`](../../src/SharpShot.Capture/Services/WindowsGraphicsCaptureService.cs) · [`png.md`](png.md) · [ADR-02](../DECISIONS.md#adr-02--windows-graphics-capture-em-vez-de-gdi-ou-desktop-duplication) · [ADR-04](../DECISIONS.md#adr-04--png-sem-perdas-como-único-formato-do-mvp).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## O que o código pede

O frame pool é criado com o formato **`DirectXPixelFormat.B8G8R8A8UIntNormalized`** (8 bits por canal, BGRA):

```csharp
Direct3D11CaptureFramePool.CreateFreeThreaded(device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, size);
```

Esse formato é a escolha deliberada do MVP: é o que a `SoftwareBitmap` consome sem conversão, é o que o `PixelBuffer` representa (BGRA8 compactado) e é o que o codificador PNG grava sem reamostrar ([`png.md`](png.md)).

## O que o Windows entrega

| Monitor | O que o DWM compõe | O que chega ao frame pool |
|---------|--------------------|---------------------------|
| **SDR** (o caso comum) | Composição 8-bit sRGB | Exatamente os pixels compostos — captura fiel ao que está na tela |
| **HDR ligado** (Windows "Usar HDR") | Composição em FP16 / scRGB (valores acima de 1.0 para realces) | Como o pool pede B8G8R8A8, o **DWM mapeia a composição para SDR** antes de entregar o frame (tone mapping do próprio sistema) |

Em outras palavras: em um monitor HDR, o SharpShot recebe a versão SDR da composição. Isso é comportamento do Windows Graphics Capture quando o formato pedido é de 8 bits, não um processamento do SharpShot.

## Consequências (limitação documentada)

- **HDR não é preservado.** Conteúdo com brilho acima do branco SDR (vídeo HDR, jogos em HDR) aparece comprimido para a faixa SDR no PNG. É o mesmo que acontece com a maioria das ferramentas de captura 8-bit.
- **O PNG é sempre 8 bits por canal, sem perfil de cor embutido.** O WIC grava RGB 8-bit (alpha ignorado); nenhum chunk `iCCP`/`cHRM` é escrito — visualizadores assumem sRGB.
- **Em SDR, a captura é fiel ao pixel.** O que o DWM compõe é o que é gravado — sem conversão de espaço de cor, sem escala, sem filtro.
- Capturas em monitores **com DPI diferente** ou **HDR em um monitor e SDR no outro** funcionam monitor a monitor (cada captura é de um `GraphicsCaptureItem` de monitor); a regra acima vale para cada um individualmente.

## Por que não FP16 no MVP

Pedir `R16G16B16A16Float` ao frame pool traria o frame HDR linear, mas exigiria tone mapping próprio (ou um PNG 16-bit com metadados que poucos visualizadores respeitam), um codificador diferente do caminho simples do WIC e uma decisão de produto sobre "o que é a imagem certa" de um frame HDR. Nada disso cabe no MVP "PNG sem perdas do que está na tela". Está no roadmap como **HDR 16-bit** ([`../PRD.md`](../PRD.md#roadmap-não-implementado)).

## O que o usuário vê

Nenhum aviso específico de HDR existe na interface do MVP. A limitação está documentada aqui, em [`limitacoes.md`](limitacoes.md) e no [`README.md`](../../README.md) (roadmap). Decisão de mostrar um aviso quando o monitor estiver em HDR: **pendente** (ver [`limitacoes.md`](limitacoes.md)).

---

Visão geral do motor: [`README.md`](README.md) · Codificação PNG: [`png.md`](png.md) · Limitações: [`limitacoes.md`](limitacoes.md)
