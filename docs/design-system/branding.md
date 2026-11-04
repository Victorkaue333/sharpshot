# Identidade visual do SharpShot

> Fonte de verdade dos assets: `tools/branding/generate_branding.py`. Nunca edite os
> arquivos gerados à mão; altere os parâmetros e rode o script (ver "Regeneração").
> Inventário completo dos arquivos: [assets.md](assets.md).

## 1. Identidade oficial

A identidade aprovada é o **Conceito A** do board `public/assets/01-branding-conceitos.png`
("S geométrico com cantos de recorte. Moderno, legível e escalável"), consolidada no board
definitivo `public/assets/02-identidade-aprovada-e-ui.png` (logotipo final, variações,
ícones para Windows, paleta e tipografia). As referências `03-logo-horizontal-referencia.png`
(logotipo horizontal, recorte 1:1 do board 02) e `04-icone-aplicativo-referencia.png`
(ícone do aplicativo) complementam o conjunto. Os conceitos B e C foram descartados.

Toda a geometria desta página foi obtida por medição pixel a pixel dessas referências
(ver `tools/branding/compare.py` e `assets/branding/comparison-sheet.png`).

## 2. Logotipo aprovado (Conceito A) e sua anatomia

O símbolo é um **"S" geométrico composto por quatro peças**, com simetria pontual
(rotação de 180° em torno do centro):

| Peça | Cor | Descrição |
| --- | --- | --- |
| `TopBracket` | gradiente laranja | Colchete superior: barra horizontal no topo + braço vertical à esquerda. Canto externo superior-esquerdo arredondado, canto interno com filete discreto, ponta livre da barra cortada com leve inclinação, ponta inferior do braço cortada em chanfro. |
| `LowerSilver` | gradiente prata | Fita inferior: paralelogramo que "sai" do braço esquerdo, segue para a direita descendo suavemente; as duas extremidades são cortadas paralelas ao chanfro do braço. |
| `UpperSilver` | gradiente prata | Rotação de 180° da fita inferior, presa ao braço direito do colchete inferior. |
| `BottomBracket` | gradiente laranja | Rotação de 180° do colchete superior. |
| `LowerSilverEdge` / `UpperSilverEdge` (opcionais) | gradiente prata-escuro | Tira fina sobre a aresta inferior de cada fita que sugere a espessura da "fita dobrada" das referências. Omitidas no monocromático e abaixo de ~26 px. |

### Proporções (medidas relativas à altura do S = 1,0; "braço" = espessura do braço)

| Parâmetro | Valor | Observação |
| --- | --- | --- |
| Largura / altura do S | 0,89 | O S é mais alto que largo. |
| Espessura do braço (vertical) | 0,164 | ≈ 1/6 da altura. |
| Espessura da barra (horizontal) | 0,90 × braço | As barras são ligeiramente mais finas que os braços, como nas referências. |
| Comprimento da barra (da borda externa à ponta superior) | 0,63 | |
| Inclinação da ponta livre da barra | 12° em relação à vertical, sentido "/" | A ref. 01 mostra corte vertical; 02/04 mostram 10–20°. |
| Canto externo | raio 1,15 × braço, superelipse de expoente 1,35 | Mais "cheio" nas pontas e mais reto no meio que um círculo, como nas referências. |
| Filete interno | raio 0,35 × braço | |
| Chanfro do braço | começa a 0,47 da altura na borda interna; 41° em relação à horizontal | Termina na borda externa a ≈ 0,61 da altura. |
| Fita prata – inclinação | 15° em relação à horizontal (desce para a direita) | |
| Fita prata – espessura (perpendicular) | 0,95 × braço | Extensão vertical ≈ 0,16 da altura. |
| Fita prata – comprimento da aresta superior | 0,43 | A ponta distante fica a ≈ 0,14 da altura (≈ 0,85 braço) do braço oposto. |
| Fita prata – ângulo das extremidades | 41° (paralelas ao chanfro) | |
| Folga entre o chanfro e a fita | 0,20 × braço | Linha fina de respiro; a fita parece continuar o braço. |
| Folga vertical entre as duas fitas | ≈ 0,97 × braço | |
| Tira de espessura da fita | 0,09 × braço | |

### Caixa do símbolo

O símbolo é entregue numa **caixa de 256 × 256** com o S ocupando 216 px de altura
(margem de 20 px acima e abaixo) e 192 px de largura, centralizado: caixa do S =
x 32 … 224, y 20 … 236. É esse sistema de coordenadas que `LogoGeometry.cs` e os SVGs usam.

### Variante óptica para tamanhos pequenos (`SMALL_PARAMS`)

Usada automaticamente quando o S é renderizado com menos de ~26 px de altura (ícones de
16 e 24 px, tiles de 24 e 32 px): braço 0,19, barra 0,95 × braço, sem filete interno,
folga chanfro↔fita 0,32 × braço, fita 0,92 × braço e comprimento 0,41, sem tira de
espessura. As arestas horizontais e verticais são alinhadas ao pixel ("snap") para
barras nítidas de 2–3 px.

## 3. Paleta

| Token | Hex | Uso |
| --- | --- | --- |
| Background | `#0D1017` | Fundo da aplicação, fundo do logotipo horizontal dark, base do tile. |
| Surface | `#1B2230` | Superfícies elevadas; topo do gradiente do tile. |
| Primary | `#FFAA35` | Destaque; início do gradiente laranja; "Shot" no wordmark. |
| Primary hover | `#FF7A00` | Hover/ativo; fim do gradiente laranja. |
| Text primary | `#F5F7FA` | Texto principal; "Sharp" no wordmark dark; início do gradiente prata; fundo do logotipo light. |
| Text secondary | `#94A3B8` | Texto secundário; tagline no logotipo dark; início da prata na versão on-light. |

Cores derivadas usadas pelo gerador: prata `#F5F7FA → #C9D1DC`; prata on-light
`#94A3B8 → #5B6B80`; prata "bandeja" (marca sem tile em fundos desconhecidos)
`#D3DBE4 → #9AA6B6`; tira de espessura `#9AA5B4 → #6E7A8A`; borda do tile `#2A3447`
com realce superior `#4A5870`; tagline na versão light `#5B6B80`.

Gradientes são **diagonais por peça** (canto superior-esquerdo → inferior-direito da caixa
de cada forma), tanto no SVG (`objectBoundingBox`) quanto no PNG e no XAML.

## 4. Tipografia

**Segoe UI Variable** (Fluent / Windows 11). Pesos: Regular, Semibold e Bold.

- Wordmark: `Segoe UI Variable`, instância **Bold Display**, "Sharp" em Text primary e
  "Shot" em Primary, tracking −0,02 em (a referência é composta com entreletras justo).
- Tagline "Capturas de tela com máxima qualidade": instância **Regular**, Text secondary.
- Nos SVGs horizontais o texto é entregue **em contornos** (extraídos da fonte com
  fontTools), para renderizar igual em qualquer sistema. Na UI o texto é vivo.

## 5. Variantes

| Variante | Arquivo | Quando usar |
| --- | --- | --- |
| Símbolo (cor, fundo escuro) | `assets/branding/sharpshot-symbol.svg` / `-512.png` | Uso principal sobre `#0D1017`/`#1B2230`. |
| Símbolo on-light | `sharpshot-symbol-on-light.svg` / `-512.png` | Sobre fundos claros: a prata vira cinza-azulado para manter contraste. |
| Monocromático | `sharpshot-monochrome.svg` (`currentColor`), `sharpshot-symbol-mono-white.svg`, `sharpshot-symbol-mono-dark.svg` | Impressão 1 cor, gravação, badges, situações sem gradiente. |
| Ícone do aplicativo | `sharpshot-app-icon.svg` / `-512.png` | Símbolo sobre tile escuro arredondado. |
| Horizontal dark | `sharpshot-horizontal-dark.svg` / `.png` | Lock-up símbolo + wordmark + tagline sobre `#0D1017`. |
| Horizontal light | `sharpshot-horizontal-light.svg` / `.png` | Mesmo lock-up sobre `#F5F7FA`, texto em `#0D1017`, "Shot" laranja. |

### Lock-up horizontal (medido na referência 03; H = altura do S)

- Folga símbolo → texto: 0,216 H.
- Wordmark: altura de caixa alta 0,40 H; linha de base a 0,546 H do topo do S.
- Tagline: altura de caixa alta 0,113 H; linha de base a 0,845 H do topo do S; alinhada à
  esquerda com o wordmark.
- O bloco de texto fica centralizado verticalmente no S; margem direita 0,15 H.

## 6. Ícones

### Tile (ícone do aplicativo)

Quadrado arredondado com raio de **22 %** do lado, gradiente vertical `#1B2230 → #0D1017`,
borda de 1 px `#2A3447` com realce superior `#4A5870`. O S ocupa **65 %** da altura do
tile (70 % em tiles de 32 px ou menos), centralizado.

### Plano por tamanho

| Tamanho | `sharpshot.ico` (app) | `tray.ico` (bandeja) |
| --- | --- | --- |
| 16 | marca sem tile, geometria pequena, prata "bandeja" | marca sem tile, geometria pequena, prata "bandeja" |
| 24 | tile + marca pequena (70 %) | marca sem tile, geometria pequena |
| 32 | tile + marca pequena (70 %) | marca sem tile, geometria normal |
| 48 | tile + marca normal (65 %) | marca sem tile |
| 64 / 128 / 256 | tile + marca normal (65 %) | marca sem tile |

Razões: o board 02 ("Ícones para Windows") mostra tile de 24 px em diante e marca nua em
16 px; a 16 px um tile deixaria o S com ~11 px. A bandeja usa a marca nua em todos os
tamanhos para ficar consistente entre escalas de DPI (16/20/24/32 px) e recebe uma prata
ligeiramente mais escura para continuar legível na barra de tarefas clara (`#F3F3F3`) –
a prata padrão `#F5F7FA` desaparece nesse fundo. Cada entrada do ICO é renderizada no
próprio tamanho (nunca reduzida de outra), com transparência real fora do tile.

### Tiles MSIX (`src/SharpShot.App/Assets`)

`Square44x44Logo.scale-200` (tile 88 px), `Square44x44Logo.targetsize-24_altform-unplated`
(tile 24 px), `Square150x150Logo.scale-200` e `Wide310x150Logo.scale-200` (tile de 200 px
centralizado, fundo transparente), `StoreLogo` (tile 50 px), `SplashScreen.scale-200`
(lock-up horizontal centralizado sobre `#0D1017`), `LockScreenLogo.scale-200` (glifo
branco sobre transparente, como a convenção de badge do Windows), `NotificationLogo`
(tile 256 px).

## 7. Regras de uso

- **Área de respiro**: no mínimo a espessura de um braço (≈ 0,16 H) ao redor do símbolo;
  a caixa 256 já inclui ~20 px de margem vertical.
- **Fundos**: a versão colorida padrão foi desenhada para `#0D1017`/`#1B2230`. Em fundos
  claros use a variante on-light; em fundos fotográficos ou coloridos use o tile ou o
  monocromático.
- **Tamanhos mínimos**: símbolo 16 px (use os PNG/ICO gerados, que têm geometria
  própria para 16/24 px); lock-up horizontal 24 px de altura do S (≈ 110 px de largura).
- **Não fazer**: não redesenhar as peças, não alterar a inclinação das fitas nem o chanfro,
  não usar três barras horizontais planas (versão rejeitada), não distorcer a proporção
  0,89, não trocar o laranja por outra cor, não aplicar sombras/efeitos extras, não
  reduzir o ICO a partir de um único tamanho, não compor "Sharp"/"Shot" em cores
  diferentes das definidas, não usar a prata padrão sobre fundos claros.
- **Título da janela / cabeçalho**: símbolo sem tile (como nas referências 05–07),
  desenhado pelos `Path` do `LogoGeometry.cs` dentro de um `Viewbox`.

## 8. Regeneração

- Fonte de verdade: `tools/branding/generate_branding.py` (`MarkParams` = anatomia;
  `Geometry` = formas; demais funções = exportadores).
- Comando: `python tools/branding/generate_branding.py` na raiz do repositório.
- Validação: `python tools/branding/compare.py` gera `assets/branding/comparison-sheet.png`
  (referência × novo × sobreposição × diferença, lock-up, ícone 256, tamanhos pequenos
  em 6×/8×, bandeja em barra escura/clara, variantes) e imprime IoU e desvios de borda
  por referência.
- Dependências: Python 3.10+, Pillow; fontTools + Segoe UI Variable para os contornos do
  wordmark (fallback para `<text>`/fonte genérica se ausentes).
- Saídas: ver [assets.md](assets.md). O C# (`LogoGeometry.cs`) é gerado junto.

## 9. Diferenças conhecidas em relação às referências

1. **Simetria**: as referências 02/04 (geradas por IA) não são perfeitamente simétricas –
   o braço do colchete inferior é ~5 % da altura mais longo que o superior e as fitas
   ficam ~1,5 % acima do centro. A marca oficial é **exatamente** simétrica (rotação de
   180°), como o conceito original (ref. 01). Isso responde pela maior parte do resíduo
   das métricas (IoU laranja 0,79–0,93; prata 0,80–0,87).
2. **Sombreamento**: as referências têm iluminação 3D (reflexos no topo das fitas, sombra
   suave sob elas). A versão vetorial usa gradientes planos por peça e uma tira escura na
   aresta inferior das fitas; não há sombras projetadas.
3. **Ponta das barras**: 12° de inclinação (01 mostra vertical; 02/04 entre 10° e 20°).
4. **Canto externo**: superelipse (expoente 1,35) em vez de arco circular, para reproduzir
   o perfil "cheio" medido; o canto inferior-direito das referências é um pouco maior que
   o superior-esquerdo (assimetria da IA).
5. **Tagline**: na ref. 03 o texto aparece quase branco; a identidade usa Text secondary
   `#94A3B8` conforme a paleta. Para o visual da referência basta trocar `TEXT_SECONDARY`
   pelo valor desejado na função `svg_horizontal`/`render_horizontal`.
6. **Wordmark**: Segoe UI Variable Bold Display com tracking −0,02 em; a largura total
   fica ~4 % maior que na referência (que foi composta mais justa).
7. **Ícones pequenos**: a 16 px a prata é levemente escurecida (`#D3DBE4 → #9AA6B6`) para
   funcionar em fundos claros; o board mostra a prata padrão sobre fundo escuro.
