# Estados — Design system

> Como cada peça se comporta em repouso, hover, pressionado, foco, desabilitado, carregando, sucesso, erro, aviso e vazio. Regra: estado visual **sempre** acompanhado de estado acessível (`IsEnabled`, `AutomationProperties`, `InfoBar` com texto) — cor nunca é a única informação.
> Fontes: estados padrão dos controles WinUI 3 · [`cores.md`](cores.md) · [`../architecture/erros.md`](../architecture/erros.md) · App (`src/SharpShot.App/Resources/Colors.xaml`, `Styles.xaml`, `Views/`).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Estados dos controles padrão

Botões, toggles, `RadioButtons`, `TextBox` e `NavigationView` usam os `VisualStates` do WinUI sem override — `PointerOver`, `Pressed`, `Disabled`, `Focused` já respeitam tema e alto contraste. O que o SharpShot define:

| Controle | Repouso | Hover | Pressionado | Foco (teclado) | Desabilitado |
|----------|---------|-------|-------------|----------------|--------------|
| **Botão primário** (`AccentButtonStyle` com accent = `Primary`) | fundo `#FFAA35`, texto `#0D1017` | fundo `#FF7A00`, texto `#0D1017` | fundo `#FF7A00` a 90 % | anel do Fluent (2 px, `SystemControlFocusVisualPrimaryBrush`) | fundo `Primary` 40 %, texto 50 % — ou o `Disabled` padrão do Fluent (**escolher um; pendente**) |
| **Botão secundário** (`DefaultButtonStyle`) | padrão Fluent sobre `Surface` | padrão | padrão | padrão | padrão |
| **ActionCard** (Home) | `Surface`, borda 1 px transparente | borda 1 px `Primary` | fundo `Surface` + 4 % branco | anel do Fluent em volta do card inteiro | opacidade 60 %, sem hover, `IsEnabled=false` |
| **SettingRow** | `Surface` | sem hover (não é clicável; o controle à direita tem o próprio hover) | — | foco vai ao controle | linha inteira em 60 % quando a opção não se aplica (ex.: notificações indisponíveis) |
| **HotkeyEditor** | mostra "Ctrl + Shift + 1" | borda `Primary` | — | borda `Primary` 2 px + texto "Pressione a combinação…" | 60 %, não entra em edição |
| **NavigationViewItem** | padrão | padrão | padrão | padrão | — |

## Estados da aplicação

### Capturando

- `ICaptureCoordinator.IsBusy = true` → cards da Home e item de menu da bandeja desabilitados; hotkeys **ignorados** (não enfileirados) enquanto `IsBusy`.
- Sem spinner visível para tela inteira (dura < 1 s esperado — **não medido**); a janela some e volta. Se passar de ~1 s (timeout vai até 5 s), a linha de status mostra `ProgressRing` pequeno + "Capturando…" (**decisão pendente** sobre o limiar).
- No modo região, "capturando" = overlays abertos; a Home não é visível.

### Sucesso

- `StatusLine` Success: ícone ✓ do Fluent + "Última captura: <nome> (<L × A>)" + link "Abrir pasta".
- Toast (se ligado). Nenhum som próprio (o toast usa o som padrão do sistema; `Audio silent` é opção — **pendente**).
- O card volta ao repouso; o foco do teclado volta a onde estava.

### Erro

- `StatusLine` Error: ícone do Fluent + título + mensagem da tabela de [`../architecture/erros.md`](../architecture/erros.md) + botão de ação ("Tentar novamente", "Abrir Configurações", "Abrir pasta de logs").
- Toast de erro (se notificações ligadas).
- Nunca `ContentDialog` para erro de captura/gravação; nunca fechar o app.
- Estado restaurado: janela visível se estava, overlays fechados, cards habilitados.

### Aviso

- `InfoBar` Warning na seção Atalhos em caso de conflito; permanece até o conflito ser resolvido; `IsClosable=false` enquanto o atalho não funcionar.
- `InfoBar` Informational na Home se as configurações foram redefinidas por arquivo corrompido (**decisão pendente**).

### Vazio / primeiro uso

- `StatusLine` oculta (sem "nenhuma captura ainda" — ruído).
- Pasta padrão exibida mesmo antes de existir no disco ("Imagens\SharpShot" — criada na primeira captura).
- Nenhum tutorial/onboarding.

### Indisponível

| Situação | Estado |
|----------|--------|
| WGC não suportado (`IsSupported=false`) | Cards desabilitados + `InfoBar` Error permanente na Home: "Captura não disponível…" ([`../architecture/erros.md`](../architecture/erros.md)); hotkeys não registrados |
| Notificações não registradas | Toggle "Mostrar notificação" desabilitado com descrição "Notificações indisponíveis nesta sessão" (**pendente**) |
| Chave `Run` não gravável | Toggle volta ao estado real + `InfoBar` Error na seção Geral |

## Overlay de região

| Estado | Visual |
|--------|--------|
| Sem seleção | Máscara em todo o monitor; cursor cruz; barra Capturar/Cancelar oculta; `Esc` cancela |
| Arrastando | Retângulo cresce a partir do ponto inicial; badge acompanha; handles ocultos até soltar |
| Selecionado | Handles visíveis; barra visível; `Enter` habilitado; cursor muda por `HitTest` |
| Redimensionando/movendo | Igual a "arrastando" com o handle ativo em `#FF7A00` |
| Seleção mínima (< 8 × 8 px, se adotado) | Barra visível, **Capturar** desabilitado, badge em `TextSecondary` (**pendente**) |
| Cancelado / confirmado | Todos os overlays fecham ao mesmo tempo |

Overlay em monitor que **não** tem a seleção: só máscara + cursor cruz; iniciar arrasto lá cancela a seleção anterior e começa outra (uma seleção por vez, em um monitor).

## Foco e teclado

- `Tab` percorre: barra de título (caption buttons) → `NavigationView` → conteúdo da página.
- Na Home: card 1 → card 2 → Abrir pasta → (status: link). `Enter`/`Space` ativam o card.
- Em Configurações: ordem visual; `HotkeyEditor` entra em modo de captura com `Enter`/`Space`, sai com `Esc`.
- No overlay: `Enter`, `Esc`; setas (**pendente**).
- Foco visível sempre o do Fluent; nunca `UseSystemFocusVisuals="False"`.

## Movimento

Só o que o WinUI já faz (transições de `NavigationView`, hover de botão). Nenhuma animação própria; nada de "marching ants" animado no overlay. Respeita *Mostrar animações no Windows* automaticamente.

---

Componentes: [`componentes.md`](componentes.md) · Cores: [`cores.md`](cores.md) · Erros: [`../architecture/erros.md`](../architecture/erros.md)
