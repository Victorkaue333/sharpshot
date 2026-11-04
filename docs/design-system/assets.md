# Assets de marca – inventário

Todos os arquivos abaixo são **gerados** por `python tools/branding/generate_branding.py`
(ver [branding.md](branding.md) para a identidade e as regras). As referências aprovadas
ficam em `public/assets/` e são somente leitura.

## Estrutura de pastas

```
public/assets/                       referências aprovadas (não editar)
  01-branding-conceitos.png          board de conceitos (A = aprovado)
  02-identidade-aprovada-e-ui.png    board definitivo da identidade + UI
  03-logo-horizontal-referencia.png  logotipo horizontal (recorte do board 02)
  04-icone-aplicativo-referencia.png ícone do aplicativo
  05..07-*.png                       telas de referência da UI
assets/branding/                     logotipo (vetor + raster) e validação
assets/icons/                        ícones Windows (ICO + PNG por tamanho)
src/SharpShot.App/Assets/            assets consumidos pelo app (ICO, tiles MSIX, splash)
src/SharpShot.App/Components/LogoGeometry.cs  path data XAML da marca
tools/branding/                      gerador, comparador e README
docs/design-system/                  esta documentação
```

## assets/branding

| Arquivo | Conteúdo | Dimensões |
| --- | --- | --- |
| `sharpshot-symbol.svg` | Símbolo colorido (gradientes laranja/prata + tiras de espessura), fundo transparente | caixa 256 × 256 |
| `sharpshot-symbol-512.png` | Mesmo símbolo rasterizado | 512 × 512 |
| `sharpshot-symbol-on-light.svg` / `-512.png` | Símbolo para fundos claros (prata → cinza-azulado) | 256 / 512 |
| `sharpshot-monochrome.svg` | Símbolo em `currentColor` (4 peças, sem tiras) | 256 |
| `sharpshot-symbol-mono-white.svg` | Símbolo em `#F5F7FA` | 256 |
| `sharpshot-symbol-mono-dark.svg` | Símbolo em `#0D1017` | 256 |
| `sharpshot-app-icon.svg` / `-512.png` | Símbolo sobre o tile escuro arredondado | 256 / 512 |
| `sharpshot-horizontal-dark.svg` / `.png` | Lock-up símbolo + "SharpShot" + tagline sobre `#0D1017` (texto em contornos no SVG) | 913 × 256 |
| `sharpshot-horizontal-light.svg` / `.png` | Lock-up sobre `#F5F7FA` | 913 × 256 |
| `comparison-sheet.png` | Folha de validação visual gerada por `tools/branding/compare.py` | 1900 × ~3500 |

IDs dos `path` nos SVGs: `TopBracket`, `LowerSilver`, `LowerSilverEdge`, `UpperSilver`,
`UpperSilverEdge`, `BottomBracket` (ordem de pintura). Gradientes: `orange`, `silver`,
`silverEdge`, `tile`, `tileHighlight`.

## assets/icons

| Arquivo | Conteúdo |
| --- | --- |
| `sharpshot.ico` | Ícone do aplicativo: 16 (marca nua) e 24/32/48/64/128/256 (tile). Entradas BMP 32 bpp até 48 px, PNG de 64 px em diante; cada tamanho renderizado individualmente. |
| `sharpshot-{16,24,32,48,64,128,256}.png` | As mesmas imagens, uma por arquivo. |
| `tray.ico` | Ícone da bandeja: marca sem tile em todos os tamanhos (16/24 com geometria pequena e prata "bandeja"). |
| `tray-{16,24,32,48,64,128,256}.png` | As mesmas imagens, uma por arquivo. |

## src/SharpShot.App/Assets (consumidos pelo app)

| Arquivo | Conteúdo | Dimensões |
| --- | --- | --- |
| `SharpShot.ico` | Cópia de `assets/icons/sharpshot.ico` (`<ApplicationIcon>` no csproj) | 7 tamanhos |
| `Tray.ico` | Cópia de `assets/icons/tray.ico` | 7 tamanhos |
| `NotificationLogo.png` | Tile com a marca, para toasts | 256 × 256 |
| `Square44x44Logo.scale-200.png` | Tile com a marca | 88 × 88 |
| `Square44x44Logo.targetsize-24_altform-unplated.png` | Tile com a marca (geometria pequena) | 24 × 24 |
| `Square150x150Logo.scale-200.png` | Tile de 200 px centralizado, fundo transparente | 300 × 300 |
| `Wide310x150Logo.scale-200.png` | Tile de 200 px centralizado, fundo transparente | 620 × 300 |
| `StoreLogo.png` | Tile com a marca | 50 × 50 |
| `SplashScreen.scale-200.png` | Lock-up horizontal dark centralizado sobre `#0D1017` | 1240 × 600 |
| `LockScreenLogo.scale-200.png` | Marca monocromática branca sobre transparente (badge) | 48 × 48 |

## src/SharpShot.App/Components/LogoGeometry.cs

`namespace SharpShot.App.Components; internal static class LogoGeometry` com:

- `public const double BoxSize = 256;`
- `TopBracket`, `LowerSilver`, `UpperSilver`, `BottomBracket` – path data (`M x,y L x,y … Z`)
  na caixa 256 × 256, ordem de pintura; colchetes com gradiente laranja
  `#FFAA35 → #FF7A00`, fitas com gradiente prata `#F5F7FA → #C9D1DC` (diagonal por forma).
- `LowerSilverEdge`, `UpperSilverEdge` – opcionais; tiras `#9AA5B4 → #6E7A8A` pintadas
  depois da fita correspondente quando a marca tem 26 px ou mais.

## Como consumir

- **XAML (WinUI 3)**: quatro (ou seis) `Path` dentro de um `Viewbox`, com o `Canvas`/grid
  interno de `Width`/`Height` = `LogoGeometry.BoxSize`. As constantes são *path markup*
  (`M x,y L x,y … Z`): converta em código (por exemplo
  `XamlBindingHelper.ConvertValue(typeof(Geometry), LogoGeometry.TopBracket)`) ou cole o
  texto no atributo `Data` do XAML – WinUI não tem `x:Static` nem converte `string` em
  `Geometry` por binding.
- **Web/Docs**: usar os SVGs diretamente; `sharpshot-monochrome.svg` herda a cor do texto.
- **Instaladores / Store**: `sharpshot.ico`, `StoreLogo.png`, tiles MSIX.

## Regeneração

```powershell
python tools/branding/generate_branding.py   # regenera tudo
python tools/branding/compare.py             # valida (folha + métricas)
```

Requisitos e detalhes em `tools/branding/README.md`.
