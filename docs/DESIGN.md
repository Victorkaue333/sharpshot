# DESIGN.md — SharpShot

Guia rápido de design para quem escreve tela (gente e agente de IA). Diz **o que usar** e **o que não
fazer**. O detalhe de cada regra fica na fonte canônica: [`design-system/`](design-system/README.md)
(cores, tipografia, espaçamento, componentes, estados, ícones, temas) e, para a marca,
[`design-system/branding.md`](design-system/branding.md) e [`design-system/assets.md`](design-system/assets.md).

> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves
> Ordem de precedência: identidade aprovada (Conceito A, [`public/assets/02-identidade-aprovada-e-ui.png`](../public/assets/02-identidade-aprovada-e-ui.png) — *mockup*) → paleta em `tools/branding/generate_branding.py` (o que gera os assets) → `docs/design-system/` → este arquivo. **O App ainda está sendo escrito**: toda referência a XAML abaixo é a convenção a seguir, não código verificado.

---

## 1. Princípios

1. **Nativo primeiro.** WinUI 3 / Fluent Design com controles de estoque (`NavigationView`, `SettingsCard`-like, `InfoBar`, `ToggleSwitch`, `ComboBox`, `Button`). Nada de biblioteca de UI de terceiros ([`../CLAUDE.md`](../CLAUDE.md)).
2. **A marca é acento, não superfície.** Laranja (`Primary #FFAA35`) em CTA, foco, borda da seleção e no logo. Fundos são `Background #0D1017` e `Surface #1B2230` no escuro; no claro, os neutros do Fluent.
3. **Sem controle falso.** Todo controle visível está ligado a uma função real. O formato é uma **linha fixa** "PNG (sem perda de qualidade)", não um `ComboBox` com uma opção.
4. **Token, nunca hex no XAML.** Cores entram por `ResourceDictionary` (`SharpShotBackgroundBrush`, `SharpShotPrimaryBrush`…), definidas uma vez por tema.
5. **Os três temas são de primeira classe.** Seguir o sistema (default), Claro e Escuro — toda tela precisa funcionar nos três.
6. **Teclado completo.** `Enter`/`Esc` no overlay, `Tab` em tudo, foco visível (o do Fluent).
7. **pt-BR, direto, sem jargão.** "Capturar tela inteira", "Selecionar região", "Abrir pasta". Mensagens de erro dizem o que fazer ([`architecture/erros.md`](architecture/erros.md)).
8. **Mockup é referência visual, não spec de pixel.** As imagens em `public/assets/` são mockups: use-as para hierarquia e tom, não para copiar medidas.

## 2. Fontes da verdade

| O quê | Onde |
|-------|------|
| Identidade aprovada (Conceito A) | [`public/assets/02-identidade-aprovada-e-ui.png`](../public/assets/02-identidade-aprovada-e-ui.png) *(mockup)* |
| Paleta e geometria do logo (executável) | [`tools/branding/generate_branding.py`](../tools/branding/generate_branding.py) |
| Assets gerados | `assets/branding/` (SVG/PNG do logo), `assets/icons/` (`.ico`, PNGs), `src/SharpShot.App/Assets/` (runtime) |
| Path XAML do logo | `src/SharpShot.App/Components/LogoGeometry.cs` (gerado — não editar) |
| Tokens de cor, por tema | [`design-system/cores.md`](design-system/cores.md) · [`design-system/temas.md`](design-system/temas.md) |
| Tipografia | [`design-system/tipografia.md`](design-system/tipografia.md) |
| Espaçamento e raio | [`design-system/espacamento.md`](design-system/espacamento.md) |
| Componentes e telas | [`design-system/componentes.md`](design-system/componentes.md) |
| Estados (hover, foco, desabilitado, erro, carregando) | [`design-system/estados.md`](design-system/estados.md) |
| Ícones | [`design-system/icones.md`](design-system/icones.md) |
| Marca e uso do logo | [`design-system/branding.md`](design-system/branding.md) *(mantido pelo agente de branding)* |

ADRs de UI: ADR-01 (WinUI 3), ADR-11 (sem MVVM framework), ADR-13 (branding gerado), ADR-14 (pt-BR). Tudo em [`DECISIONS.md`](DECISIONS.md).

## 3. Cor

### Paleta

| Hex | Papel | Token (XAML) |
|-----|-------|--------------|
| `#0D1017` | Background (escuro) | `SharpShotBackgroundBrush` |
| `#1B2230` | Surface — cards, barra de título, painéis (escuro) | `SharpShotSurfaceBrush` |
| `#FFAA35` | Primary — CTA, borda da seleção, acento do logo | `SharpShotPrimaryBrush` |
| `#FF7A00` | Primary hover / pressed | `SharpShotPrimaryHoverBrush` |
| `#F5F7FA` | Texto primário (escuro) · prata clara do logo | `SharpShotTextBrush` |
| `#94A3B8` | Texto secundário, ícones inativos, prata sobre claro | `SharpShotTextSecondaryBrush` |

- Tema claro: fundo e superfície usam os recursos do Fluent (`SolidBackgroundFillColorBase`/`LayerFillColorDefault`); texto escuro do sistema; **Primary e Hover não mudam**. Tabela por tema em [`design-system/temas.md`](design-system/temas.md).
- Contraste (WCAG, calculado em [`design-system/cores.md`](design-system/cores.md)): `#F5F7FA` sobre `#0D1017` ≈ 17,7:1 e sobre `#1B2230` ≈ 14,8:1; `#94A3B8` sobre `#1B2230` ≈ 6,2:1 — todos passam AA para qualquer texto no escuro. Laranja `#FFAA35` sobre `#0D1017` ≈ 10:1 (texto laranja no escuro é legível). **Texto branco sobre laranja falha (≈ 1,8:1)**: botão primário tem texto **escuro** (`#0D1017`). No **tema claro**, `#FFAA35` sobre branco ≈ 1,9:1 e `#94A3B8` sobre branco ≈ 2,6:1 **falham** — ali o laranja só entra como preenchimento com texto escuro ou como anel de foco, e o texto secundário usa a cor do sistema.
- Sem gradiente na UI; o único gradiente é a prata do logo (gerada pelo script).

### Semântica

| Estado | Use | Nunca |
|--------|-----|-------|
| Sucesso (captura salva) | `InfoBar` `Success` do Fluent / toast | Verde custom |
| Erro (falha de captura/gravação) | `InfoBar` `Error` + mensagem da tabela de erros | Vermelho custom, `MessageDialog` bloqueante para erro recuperável |
| Aviso (conflito de atalho) | `InfoBar` `Warning` | Laranja da marca como aviso |
| Informativo (status da última captura) | `TextBlock` secundário | — |

## 4. Tipografia

- **Segoe UI Variable** — a família do Windows 11 (cai para Segoe UI no 10). Nenhuma fonte embutida.
- Escala do Fluent: `TitleLargeTextBlockStyle` (título da Home), `SubtitleTextBlockStyle` (seções de Configurações), `BodyTextBlockStyle` (corpo), `CaptionTextBlockStyle` (status, nome de arquivo, atalho no card). Não inventar tamanhos.
- Atalhos são exibidos como `Ctrl + Shift + 1` (`HotkeyBinding.ToDisplayString()`), em `CaptionTextBlockStyle` com fonte monoespaçada opcional (Cascadia Mono se presente; fallback Consolas).
- Badge de tamanho do overlay: numerais tabulares (`FontFeatures tnum`) para "1280 × 720" não "dançar" ao arrastar. Ver [`design-system/tipografia.md`](design-system/tipografia.md).

## 5. Forma, espaçamento e motion

- **Grade de 4 px**; padding de card 16 px; gap entre cards 12 px; margem de página 24 px. Tabela em [`design-system/espacamento.md`](design-system/espacamento.md).
- **Raio:** o do Fluent (`ControlCornerRadius` 4 px; `OverlayCornerRadius` 8 px para cards e flyouts). Sem raio custom.
- **Sombra:** só a que o controle traz (`ThemeShadow` em flyouts/cards elevados). Nada manual.
- **Motion:** só as animações implícitas do WinUI e a transição do `NavigationView`. Nenhuma animação decorativa. Respeitar a configuração de animações do Windows.
- **Overlay de região:** máscara escura `#0D1017` com 60 % de opacidade fora da seleção; borda tracejada 2 px em `Primary`; handles de 8 px quadrados em `Primary` com contorno `#0D1017` de 1 px. Detalhe em [`capture-engine/regiao.md`](capture-engine/regiao.md).

## 6. Componentes e telas

Referências visuais (mockups, em `public/assets/`): `05-tela-inicial-referencia.png` (Home), `06-configuracoes-referencia.png` (Configurações), `07-selecao-regiao-referencia.png` (overlay). Catálogo em [`design-system/componentes.md`](design-system/componentes.md).

| Tela / peça | Composição |
|-------------|------------|
| **Barra de título** | Custom (`ExtendsContentIntoTitleBar`), logo (path de `LogoGeometry`) + "SharpShot" à esquerda; botões de sistema nativos |
| **Shell** | `NavigationView` modo esquerdo compacto: Início · Configurações · Sobre (sem Histórico no MVP) |
| **Início** | Dois cards grandes lado a lado: "Capturar tela inteira" (`Ctrl + Shift + 1`) e "Selecionar região" (`Ctrl + Shift + 2`); abaixo, linha "Salvar em: <pasta>" com **Abrir pasta**, linha fixa "Formato: PNG (sem perda de qualidade)", e **linha de status** (última captura ou erro) |
| **Configurações** | Seções com cabeçalho: **Geral** (Iniciar com o Windows · Minimizar para a bandeja · Mostrar notificação após capturar — `ToggleSwitch`), **Atalhos** (editor por teclas para cada ação + **Restaurar padrões** + `InfoBar` de conflito), **Salvamento** (caixa de pasta + **Procurar…** + **Abrir pasta** + texto da convenção de nomes), **Aparência** (`RadioButtons`: Seguir o sistema · Claro · Escuro) |
| **Sobre** | Logo, nome, versão (`0.1.0`), "MIT", link do repositório, créditos de terceiros |
| **Overlay de região** | Um por monitor; fundo = frame congelado; barra flutuante **Capturar** (primário) · **Cancelar** |
| **Menu da bandeja** | Win32 (segue o tema do sistema): Capturar tela inteira · Selecionar região · Abrir SharpShot · Abrir pasta de capturas · Sair |
| **Toast** | Título "Captura salva com sucesso!", corpo = nome do arquivo, logo de notificação, botões **Abrir pasta** · **Ver imagem** |

Botões: `AccentButtonStyle` para a ação primária (com o accent **do app** apontado para `Primary`), `DefaultButtonStyle` para secundárias. Um botão primário por superfície.

## 7. Layout e DPI

- Janela principal: tamanho inicial 960×640 DIP (sugestão — **pendente** de decisão no App), mínimo 720×480; conteúdo responsivo até 640 DIP de largura (cards empilham).
- Tudo em DIP dentro das janelas; conversão para pixel físico **só** no overlay via `RegionMath` ([`capture-engine/dpi-e-monitores.md`](capture-engine/dpi-e-monitores.md)).
- Ícones vetoriais (`PathIcon`/`FontIcon`) — nunca PNG escalado na UI, exceto o logo de notificação e os `.ico` do sistema.

## 8. Ícones

Segoe Fluent Icons (`FontIcon`) para ações: captura (`&#xE722;` Camera), região (`&#xE7A8;`), pasta (`&#xE8B7;`), configurações (`&#xE713;`), sobre (`&#xE946;`), sair (`&#xE7E8;`). O **logo** é sempre o path gerado, nunca um ícone genérico de câmera no lugar da marca. Lista em [`design-system/icones.md`](design-system/icones.md).

## 9. Tema claro/escuro

- `theme = "System"` → `RequestedTheme` não definido (segue o Windows); `"Light"`/`"Dark"` → `ElementTheme` na raiz da janela, aplicado na hora ao salvar.
- Os brushes da marca têm valor por tema no `ResourceDictionary` (`ThemeDictionaries`). Nunca `if (tema == Dark)` no code-behind para cor.
- O menu Win32 da bandeja e o toast seguem o tema do **sistema**, não o do app — aceito ([ADR-06](DECISIONS.md), [ADR-07](DECISIONS.md)).

## 10. Acessibilidade

- Todo controle com `AutomationProperties.Name` em pt-BR quando o conteúdo visual não é texto (botões só com ícone, handles do overlay não são focáveis — a seleção tem alternativa por teclado: **pendente de decisão**).
- Contraste AA nos três temas (ver §3).
- `InfoBar` com `IsOpen` + texto, não só cor.
- Respeitar "Mostrar animações" e "Transparência" do Windows (WinUI já faz).

## 11. Nunca faça

- Hex direto no XAML; cor da marca como fundo de página.
- Controle sem função (seletor de formato com uma opção, toggle que não persiste).
- Fonte embutida ou tamanho fora da escala do Fluent.
- Ícone genérico no lugar do logo; editar PNG/ICO/`LogoGeometry.cs` à mão (ADR-13).
- `MessageDialog`/`ContentDialog` bloqueante para erro recuperável de captura — use `InfoBar` + status.
- Texto em inglês na UI; string de UI fora da tabela de mensagens quando for erro.
- Animação decorativa; sombra manual; raio custom.
- Capturar sem esconder as janelas do SharpShot.

## 12. Divergências conhecidas

Achadas ao montar este guia (04/nov/2026). O código manda; corrigir o que estiver errado.

- [ ] TODO: o App ainda não tem `ResourceDictionary` de tema — os nomes de brush acima são a convenção proposta; confirmar quando o XAML existir.
- [ ] TODO: tamanho inicial/mínimo da janela principal não decidido.
- [ ] TODO: alternativa por teclado para desenhar/ajustar a seleção no overlay (setas + Shift?) não decidida.
- [ ] TODO: `generate_branding.py` cita `docs/design-system/palette.md`; o arquivo existe como ponteiro para [`design-system/cores.md`](design-system/cores.md). Avaliar renomear a referência no script (dono: agente de branding).
- [ ] TODO: `design-system/branding.md` e `design-system/assets.md` são escritos pelo agente de branding — conferir se existem antes de publicar o índice.
