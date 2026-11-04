# Espaçamento, forma e dimensões — Design system

> Grade, padding, gaps, raio, elevação e tamanhos de janela. Tudo em DIP (unidades do XAML); o overlay é a única superfície que pensa em pixels físicos.
> Fontes: recursos do WinUI 3 (`ControlCornerRadius`, `OverlayCornerRadius`) · mockups em `public/assets/` · App (em implementação).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Grade

Base **4 px**. Valores permitidos: 4, 8, 12, 16, 20, 24, 32, 40, 48. Qualquer outro valor é erro de layout, não exceção.

| Token (convenção) | Valor | Uso |
|-------------------|-------|-----|
| `SpaceXs` | 4 | Entre ícone e texto no mesmo rótulo; entre badge e borda da seleção |
| `SpaceS` | 8 | Entre rótulo e descrição auxiliar; entre botões de uma mesma barra |
| `SpaceM` | 12 | Gap entre cards; entre itens de uma seção de Configurações |
| `SpaceL` | 16 | Padding interno de card; gap entre seção e seu primeiro item |
| `SpaceXl` | 24 | Margem de página (conteúdo do `NavigationView`); entre seções de Configurações |
| `SpaceXxl` | 32 | Acima do primeiro bloco da Home; em volta do logo em Sobre |

Em XAML: `Margin`/`Padding` com esses valores; `Spacing` em `StackPanel`; `ColumnSpacing`/`RowSpacing` em `Grid`.

## Raio e elevação

| Elemento | Recurso WinUI | Valor |
|----------|---------------|-------|
| Botões, `ToggleSwitch`, `TextBox`, `ComboBox` | `ControlCornerRadius` | 4 px |
| Cards da Home, painéis de seção, flyouts, `InfoBar` | `OverlayCornerRadius` | 8 px |
| Janela | o Windows 11 arredonda (`DWMWCP_ROUND`); Windows 10 não | — |
| Overlay de região | 0 (tela cheia) | — |
| Badge do overlay | 4 px | fundo `Surface` 90 %, texto `Text` |
| Handles da seleção | 0 (quadrados) | 8 × 8 px físicos, contorno 1 px |

Sombra: só `ThemeShadow`/elevação que o próprio controle traz (flyouts, `ContentDialog`). Cards da Home **não** têm sombra — contraste `Surface` sobre `Background` já separa. Nenhuma `DropShadow` manual.

## Dimensões

| Item | Valor | Status |
|------|-------|--------|
| Janela principal — inicial | 960 × 640 DIP (proposta) | **pendente de decisão** |
| Janela principal — mínima | 720 × 480 DIP (proposta); abaixo de 640 de largura os cards da Home empilham | **pendente** |
| `NavigationView` | `PaneDisplayMode="LeftCompact"`, `CompactPaneLength` 48; expandido 200 — ou `Top` se a janela for baixa (**decisão pendente**) | — |
| Barra de título custom | 32 DIP (altura padrão do caption no Win11; 48 com logo + wordmark é aceitável) | — |
| Cards da Home | 2 colunas iguais, altura mínima 140 DIP, ícone 32 DIP, título `BodyStrong`, atalho `Caption` | — |
| Linha de configuração | altura mínima 64 DIP (rótulo + descrição à esquerda, controle à direita), padding 16 | — |
| `InfoBar` | largura do conteúdo, acima da seção a que se refere | — |
| Barra flutuante do overlay (Capturar/Cancelar) | 40 DIP de altura, padding 8, 12 abaixo da seleção (ou acima, se não couber) | — |
| Badge do overlay | padding 4 × 8, posicionado 4 px físicos abaixo/direita da seleção, reposicionado para dentro quando encosta no monitor | — |
| Ícone da bandeja | 16 × 16 (100 %) … 32 × 32 (200 %) — o `.ico` traz 16/24/32/48/64/128/256 | gerado |
| Logo na barra de título | 20 × 20 DIP + wordmark | — |
| Logo em Sobre | 96 × 96 DIP | — |

## Responsividade

- Largura ≥ 640: Home com 2 cards lado a lado; `< 640`: empilhados (`VisualStateManager` com `AdaptiveTrigger`).
- Configurações: lista única de largura máxima 720 DIP, centralizada quando a janela é mais larga.
- Nada de scroll horizontal; `ScrollViewer` vertical na página de Configurações.
- Escala de texto do Windows (até 225 %): layouts com `Auto`/`*`, nunca alturas fixas em linhas de texto.

## Overlay (pixels físicos)

| Medida | Valor físico | Motivo |
|--------|--------------|--------|
| Espessura da borda da seleção | 2 px | Visível em 100 % e 200 % sem virar "grossa" |
| Traço | 6 px traço / 4 px espaço | Padrão de "marching ants" estático (sem animação) |
| Handle | 8 × 8 px + contorno 1 px | Alvo mínimo de toque não se aplica (mouse/caneta); 8 px é o padrão do Windows |
| Tolerância de hit-test (`RegionMath.HitTest`) | 6 px | Pegar o handle sem exigir precisão de pixel |
| Área mínima de seleção válida | 1 × 1 px (técnico); a UI pode exigir ≥ 8 × 8 para habilitar **Capturar** (**decisão pendente**) | — |

Conversão DIP↔pixel: [`../capture-engine/dpi-e-monitores.md`](../capture-engine/dpi-e-monitores.md).

## Nunca

- Valor fora da grade de 4.
- `Margin` negativa para "alinhar".
- Altura fixa em `TextBlock`.
- Raio custom (`CornerRadius="6"`).
- Sombra manual.

---

Componentes: [`componentes.md`](componentes.md) · Tipografia: [`tipografia.md`](tipografia.md) · Guia rápido: [`../DESIGN.md`](../DESIGN.md)
