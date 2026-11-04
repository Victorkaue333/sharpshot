# DPI e múltiplos monitores — Capture Engine

> Como o SharpShot mantém coordenadas exatas em pixels físicos quando os monitores têm escalas diferentes, posições negativas e tamanhos distintos.
> Fontes: [`src/SharpShot.App/app.manifest`](../../src/SharpShot.App/app.manifest) · [`MonitorService.cs`](../../src/SharpShot.Capture/Services/MonitorService.cs) · [`MonitorInfo.cs`](../../src/SharpShot.Core/Models/MonitorInfo.cs) · [`PixelRectangle.cs`](../../src/SharpShot.Core/Models/PixelRectangle.cs) · [`RegionMath.cs`](../../src/SharpShot.Core/Services/RegionMath.cs).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Regra de ouro

**Tudo que atravessa o motor está em pixels físicos.** DIPs (device-independent pixels, a unidade do XAML) existem só dentro das janelas WinUI e são convertidos na borda, uma vez, por `RegionMath.DipToPixel`/`PixelToDip`.

## PerMonitorV2

O `app.manifest` declara:

```xml
<dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
<dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
```

Consequências:

- O Windows **não virtualiza** DPI para o processo: `GetMonitorInfoW` devolve `rcMonitor` em pixels físicos reais, e `GetCursorPos` idem.
- Cada janela do SharpShot recebe `WM_DPICHANGED` ao mudar de monitor; o WinUI 3 trata isso e expõe a escala em `XamlRoot.RasterizationScale`.
- Sem PerMonitorV2, um monitor em 150 % apareceria com `rcMonitor` "encolhido" e a captura (que é sempre física) não bateria com a seleção.

## O que `MonitorInfo` carrega

| Campo | Origem | Observação |
|-------|--------|------------|
| `Handle` (`nint`) | `EnumDisplayMonitors` | `HMONITOR`; é o que `IGraphicsCaptureItemInterop.CreateForMonitor` recebe |
| `DeviceName` | `MONITORINFOEXW.szDevice` | `\\.\DISPLAY1`, `\\.\DISPLAY2`… — usado em logs |
| `Bounds` (`PixelRectangle`) | `rcMonitor` | Pixels físicos no desktop virtual; pode ter `X`/`Y` negativos |
| `WorkArea` | `rcWork` | Sem barra de tarefas; informativo |
| `IsPrimary` | `dwFlags & MONITORINFOF_PRIMARY` | Primário sempre tem `Bounds.X == 0 && Bounds.Y == 0` |
| `Dpi` (`uint`) | `GetDpiForMonitor(MDT_EFFECTIVE_DPI)` | 96 = 100 %, 120 = 125 %, 144 = 150 %, 192 = 200 %; fallback 96 se a chamada falhar |
| `ScaleFactor` (calculado) | `Dpi / 96.0` | Usado para converter DIP↔pixel nas janelas de overlay |

**Ordem de `GetMonitors()`**: primário primeiro, depois por `Bounds.X` e `Bounds.Y` crescentes — estável entre chamadas para a UI poder indexar.

## Desktop virtual e coordenadas negativas

```text
                 (-1920, 0)                (0, 0)                  (1920, 0)
                 ┌─────────────────────────┬────────────────────────┐
                 │ \\.\DISPLAY2            │ \\.\DISPLAY1 (primário)│
                 │ 1920×1080 @ 100 %       │ 1920×1080 @ 100 %      │
                 │ Bounds = (-1920,0,1920,1080)                     │
                 └─────────────────────────┴────────────────────────┘
                                                                 (1920, 1080)
```

- A origem `(0, 0)` é o canto superior esquerdo do **monitor primário**; monitores à esquerda ou acima têm coordenadas negativas. `PixelRectangle` usa `int` com sinal por isso.
- `GetVirtualDesktopBounds()` devolve a caixa envolvente de todos os monitores (`FromLTRB(min X, min Y, max Right, max Bottom)`). No MVP é informativo: a captura é sempre **de um monitor**, nunca do desktop inteiro ([`limitacoes.md`](limitacoes.md)).
- `GetMonitorContaining(x, y)` usa `MonitorFromPoint(DEFAULTTONEAREST)` e confere o handle na lista; se o handle não bater (monitor acabou de ser adicionado), cai para `Bounds.Contains` e, por fim, para o primário.

## Qual monitor é capturado

| Modo | Monitor | Como |
|------|---------|------|
| Tela inteira | O que está **sob o cursor** | `IMonitorService.GetMonitorUnderCursor()` *(decisão do App, em implementação — ver questão aberta abaixo)* |
| Região | O que o usuário escolheu arrastando | Um `RegionOverlayWindow` por monitor; a seleção **não atravessa** monitores (ADR-10) |

A escolha "monitor sob o cursor" é a que `IMonitorService` já suporta diretamente e a que corresponde ao comportamento de `PrintScreen` por monitor em outras ferramentas. **Pendente de validação** no App: o lead pode decidir por "monitor da janela ativa" em vez do cursor.

## Conversão DIP ↔ pixel (`RegionMath`)

```csharp
int    DipToPixel(double dip, double scale)  // Math.Round(dip * scale, AwayFromZero)
double PixelToDip(int pixel, double scale)   // pixel / scale
```

- `scale` é `MonitorInfo.ScaleFactor` **ou** `XamlRoot.RasterizationScale` do overlay — os dois devem coincidir quando a janela está no monitor certo.
- Arredondamento `AwayFromZero` evita o viés de `Math.Round` padrão (bankers) em bordas de 0,5 px, que produziria seleções 1 px menores de forma assimétrica.
- A seleção é construída em pixels físicos **relativos ao monitor** (`PixelRectangle.RelativeTo(monitor.Bounds)`) antes de `PixelBuffer.Crop`, porque o buffer capturado tem origem `(0, 0)` no canto do monitor, não no desktop virtual.

## Monitores com escalas diferentes lado a lado

Cenário: DISPLAY1 em 150 % (2880×1620 físico), DISPLAY2 em 100 % (1920×1080).

1. O overlay de cada monitor é posicionado pelos `Bounds` físicos (`SetWindowPos` com `HWND` do WinUI — em implementação) e usa a `RasterizationScale` do seu próprio monitor para desenhar.
2. O cursor do usuário, em pixels físicos, é convertido para DIP **da janela em que está**; o retângulo desenhado em DIP é convertido de volta para físico com a mesma escala. Nenhum valor "vaza" de um monitor para outro.
3. O `PixelBuffer` capturado de DISPLAY1 tem 2880×1620 pixels — o PNG sai nesse tamanho, sem reamostragem. O badge "1280 × 720" do overlay mostra o tamanho **físico** da seleção, não em DIP.

## O que pode dar errado (e a resposta do código)

| Situação | Comportamento |
|----------|---------------|
| Monitor desconectado entre a enumeração e a captura | `CreateForMonitor` falha → `CaptureException(NoMonitor)`; a UI reenumera na próxima tentativa |
| `GetDpiForMonitor` falha (sessão sem shcore, RDP antigo) | DPI assume 96; captura continua correta (é física); só a conversão DIP pode ficar errada no overlay |
| Mudança de resolução durante a captura | `frame.ContentSize` < tamanho do pool → buffer recortado para o conteúdo real |
| Monitor em orientação retrato | `rcMonitor` já vem rotacionado; nada a fazer |
| Monitor duplicado (modo "Duplicar") | Windows expõe um único `HMONITOR`; captura normal |

## Testes relacionados (planejados — `tests/`)

- `RegionMath`: `DipToPixel` em 100/125/150/200 %, arredondamento em 0,5; `FromDrag` com pontos fora do monitor (clamp); `Move`/`Resize` nos limites.
- `PixelRectangle.FromPoints` com coordenadas negativas; `Intersect` vazio; `RelativeTo`.
- `MonitorService.GetMonitors()` retorna ≥ 1 monitor com `Dpi > 0` e exatamente um `IsPrimary` (teste de integração, pulado sem sessão de desktop).

---

Seleção de região: [`regiao.md`](regiao.md) · Visão geral: [`README.md`](README.md) · Limitações: [`limitacoes.md`](limitacoes.md)
