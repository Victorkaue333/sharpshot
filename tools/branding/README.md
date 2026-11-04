# tools/branding

Ferramentas que geram **todos** os assets de marca do SharpShot a partir de uma única
fonte de verdade: a geometria descrita em `generate_branding.py`. Nenhum arquivo em
`assets/branding`, `assets/icons`, `src/SharpShot.App/Assets` ou
`src/SharpShot.App/Components/LogoGeometry.cs` deve ser editado à mão.

## Arquivos

| Arquivo | Função |
| --- | --- |
| `generate_branding.py` | Fonte de verdade. Define a anatomia da marca (`MarkParams`), constrói as formas (`Geometry`), escreve SVGs, PNGs, ICOs, tiles MSIX e o `LogoGeometry.cs`. |
| `compare.py` | Validação. Renderiza a marca na mesma caixa dos recortes das referências (`public/assets`), calcula métricas de fidelidade (IoU e desvio de bordas por linha) e monta `assets/branding/comparison-sheet.png`. |

## Requisitos

- Python 3.10+
- Pillow (`pip install pillow`)
- fontTools (`pip install fonttools`) **e** a fonte *Segoe UI Variable*
  (`C:\Windows\Fonts\SegUIVar.ttf`, presente no Windows 11) para o logotipo horizontal
  com o wordmark em contornos vetoriais. Sem eles o script ainda roda: o SVG horizontal
  usa `<text>` e o PNG usa a fonte disponível (com aviso no console).

## Uso

Na raiz do repositório:

```powershell
python tools/branding/generate_branding.py   # regenera todos os assets (~10 s)
python tools/branding/compare.py             # folha de comparação + métricas
python tools/branding/compare.py --diff-dir out/   # também grava diffs 4x por referência
```

## O que é gerado

- `assets/branding/` – `sharpshot-symbol*.svg/png`, `sharpshot-monochrome.svg`
  (`currentColor`), `sharpshot-app-icon.svg/-512.png`, `sharpshot-horizontal-{dark,light}.svg/png`,
  `comparison-sheet.png` (por `compare.py`).
- `assets/icons/` – `sharpshot-{16,24,32,48,64,128,256}.png`, `tray-{...}.png`,
  `sharpshot.ico`, `tray.ico` (cada ICO contém os 7 tamanhos renderizados individualmente;
  BMP até 48 px, PNG de 64 px em diante).
- `src/SharpShot.App/Assets/` – `SharpShot.ico`, `Tray.ico`, `NotificationLogo.png` e os
  tiles MSIX (`Square44x44Logo*`, `Square150x150Logo`, `Wide310x150Logo`, `StoreLogo`,
  `SplashScreen`, `LockScreenLogo`).
- `src/SharpShot.App/Components/LogoGeometry.cs` – path data XAML das formas na caixa
  256×256 (`TopBracket`, `LowerSilver`, `UpperSilver`, `BottomBracket`, opcionais
  `LowerSilverEdge`/`UpperSilverEdge`, e `BoxSize`).

## Como alterar a marca

1. Ajuste os parâmetros em `MarkParams` (valores relativos à altura do S e à espessura
   do braço; cada campo está comentado) ou, para os tamanhos pequenos, `SMALL_PARAMS`.
2. Rode `generate_branding.py` e depois `compare.py`.
3. Leia `assets/branding/comparison-sheet.png` e as métricas impressas; só aceite a
   mudança se a fidelidade às referências e a legibilidade em 16/24 px continuarem boas.

A documentação da identidade (anatomia, paleta, variantes, regras de uso) está em
`docs/design-system/branding.md`; o inventário dos arquivos em
`docs/design-system/assets.md`.
