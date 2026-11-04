# Design system — SharpShot

> Fonte canônica das decisões visuais do SharpShot: cores, tipografia, espaçamento, componentes, estados, ícones e temas. O guia rápido para quem escreve tela é [`../DESIGN.md`](../DESIGN.md); este diretório é o detalhe. A marca (logo, uso, versões) e o inventário de assets gerados são mantidos pelo agente de branding em [`branding.md`](branding.md) e [`assets.md`](assets.md).
> Precedência: identidade aprovada (Conceito A — mockup [`public/assets/02-identidade-aprovada-e-ui.png`](../../public/assets/02-identidade-aprovada-e-ui.png)) → paleta em `tools/branding/generate_branding.py` → estes arquivos → `ResourceDictionary` do App (`src/SharpShot.App/Resources/Colors.xaml` e `Styles.xaml` — fonte executável; estes docs devem espelhá-los).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Ordem de leitura

| # | Arquivo | O que responde |
|---|---------|----------------|
| 1 | [`cores.md`](cores.md) | Paleta, papel de cada cor, tokens, contraste calculado, o que não fazer |
| 2 | [`temas.md`](temas.md) | Valores dos tokens em Escuro, Claro e Seguir o sistema; como o app aplica |
| 3 | [`tipografia.md`](tipografia.md) | Segoe UI Variable, escala do Fluent por uso, numerais tabulares, fallback no Win10 |
| 4 | [`espacamento.md`](espacamento.md) | Grade de 4 px, padding/gaps por superfície, raio, tamanhos de janela |
| 5 | [`componentes.md`](componentes.md) | Cada tela e peça (barra de título, shell, Home, Configurações, Sobre, overlay, bandeja, toast) com composição e controles WinUI |
| 6 | [`estados.md`](estados.md) | Hover, pressionado, foco, desabilitado, carregando, sucesso, erro, aviso, vazio |
| 7 | [`icones.md`](icones.md) | Segoe Fluent Icons por ação, ícones de app/bandeja/notificação, regra "logo nunca é ícone genérico" |
| 8 | [`branding.md`](branding.md) · [`assets.md`](assets.md) | Marca e assets (dono: agente de branding) |
| — | [`palette.md`](palette.md) | Ponteiro para `cores.md` (nome citado pelo script de branding) |

## Princípios (resumo)

1. **Nativo primeiro** — WinUI 3 / Fluent, controles de estoque, sem biblioteca de UI.
2. **Marca é acento** — laranja em CTA, foco, seleção e logo; superfícies neutras.
3. **Sem controle falso** — tudo visível faz algo.
4. **Token, nunca hex no XAML** — brushes `SharpShot*` por tema.
5. **Três temas de primeira classe** — Sistema (default), Claro, Escuro.
6. **Teclado completo, contraste AA** nos três temas.
7. **pt-BR direto** — "Capturar tela inteira", não "Screenshot".
8. **Mockup é referência, não spec** — `public/assets/*.png` são mockups.

## Referências visuais (mockups)

| Arquivo | Conteúdo | Uso |
|---------|----------|-----|
| `public/assets/01-branding-conceitos.png` | Conceitos de marca explorados | histórico |
| `public/assets/02-identidade-aprovada-e-ui.png` | **Conceito A aprovado** + UI de referência | fonte da identidade |
| `public/assets/03-logo-horizontal-referencia.png` | Logo horizontal | referência do `svg_horizontal` do script |
| `public/assets/04-icone-aplicativo-referencia.png` | Ícone do app (tile) | referência do `.ico` |
| `public/assets/05-tela-inicial-referencia.png` | Home | hierarquia e tom |
| `public/assets/06-configuracoes-referencia.png` | Configurações | seções e controles |
| `public/assets/07-selecao-regiao-referencia.png` | Overlay de região | máscara, borda, handles, badge |
| `public/assets/a_*.png` | Boards de apresentação gerados | só apresentação |

Nenhuma dessas imagens é captura do app rodando. Quando houver capturas reais, elas entram em uma pasta própria e os mockups continuam rotulados como mockups.

## Fonte executável

As fontes executáveis de cor são `tools/branding/generate_branding.py` (constantes `BACKGROUND`, `SURFACE`, `PRIMARY`, `PRIMARY_HOVER`, `TEXT_PRIMARY`, `TEXT_SECONDARY`, `SILVER_*`, para os assets) e `src/SharpShot.App/Resources/Colors.xaml` (tema do App, dicionários Dark/Light/HighContrast, com os overrides de `SystemAccentColor*` em `App.xaml`); a convenção de nomes está em [`cores.md`](cores.md) e [`temas.md`](temas.md). Qualquer divergência entre eles e estes docs é bug de doc.

## Lacunas conhecidas

- [ ] Tom de acento escuro para o **tema claro** (o laranja `#FFAA35` falha contraste sobre branco como texto/ícone) — decisão de branding pendente ([`cores.md`](cores.md)).
- [ ] Tamanho inicial/mínimo da janela principal — pendente ([`espacamento.md`](espacamento.md)).
- [ ] Alternativa por teclado para desenhar a seleção no overlay — pendente ([`componentes.md`](componentes.md)).
- [ ] `branding.md` e `assets.md` — conferir existência (dono: agente de branding).

---

Guia rápido: [`../DESIGN.md`](../DESIGN.md) · Índice geral: [`../INDEX.md`](../INDEX.md)
