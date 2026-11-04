# Temas — Design system

> Como os três modos (Seguir o sistema · Claro · Escuro) se traduzem em tokens, como o App aplica a escolha e o que fica fora do controle do app (menu Win32, toast).
> Fontes: `ThemeMode` em `SharpShot.Core` (verificado) · [`cores.md`](cores.md) · App (`src/SharpShot.App/Resources/Colors.xaml`, `Styles.xaml`, `Views/`).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Modelo

`AppSettings.Theme : ThemeMode` = `System` (padrão) · `Light` · `Dark`, persistido como string em `settings.json`. O App mapeia para `ElementTheme` na raiz de cada janela:

| `ThemeMode` | `ElementTheme` | Comportamento |
|-------------|----------------|---------------|
| `System` | `Default` | Segue *Configurações → Personalização → Cores → Escolher o modo de aplicativo*; muda ao vivo quando o Windows muda |
| `Light` | `Light` | Fixo |
| `Dark` | `Dark` | Fixo |

Aplicado imediatamente ao salvar (evento `ISettingsService.Changed` → `ThemeService` → `window.Content.RequestedTheme`), em **todas** as janelas abertas (principal e overlays). Alto contraste do Windows sobrepõe os dois (WinUI usa o dicionário `HighContrast`).

## Tokens por tema

| Token | Escuro | Claro | Alto contraste |
|-------|--------|-------|----------------|
| `SharpShotBackgroundColor` | `#0D1017` | `SolidBackgroundFillColorBase` (Fluent, ≈ `#F3F3F3`) | `SystemColorWindowColor` |
| `SharpShotSurfaceColor` | `#1B2230` | `LayerFillColorDefault` (Fluent, branco translúcido) | `SystemColorWindowColor` |
| `SharpShotPrimaryColor` | `#FFAA35` | `#FFAA35` | `SystemColorHighlightColor` |
| `SharpShotPrimaryHoverColor` | `#FF7A00` | `#FF7A00` | `SystemColorHighlightColor` |
| `SharpShotOnPrimaryColor` | `#0D1017` | `#0D1017` | `SystemColorHighlightTextColor` |
| `SharpShotTextColor` | `#F5F7FA` | `TextFillColorPrimary` (Fluent, ≈ `#1A1A1A`) | `SystemColorWindowTextColor` |
| `SharpShotTextSecondaryColor` | `#94A3B8` | `TextFillColorSecondary` (Fluent) | `SystemColorWindowTextColor` |
| Acento para texto/ícone no claro | — | **pendente** (laranja reprova contraste sobre branco — [`cores.md`](cores.md)) | — |
| Máscara do overlay | `#0D1017` @ 60 % | `#0D1017` @ 60 % (a máscara é sempre escura: escurecer é o que destaca a seleção) | idem |
| Borda/handles da seleção | `#FFAA35` com contorno `#0D1017` 1 px | igual | `SystemColorHighlightColor` |

No tema claro o app **não** inventa neutros: usa os recursos do Fluent para fundo, superfície e texto, e mantém só o laranja como marca. Isso garante que `NavigationView`, `InfoBar`, `ToggleSwitch` e o restante fiquem coerentes com o sistema sem retrabalho.

## Logo por tema

| Contexto | Asset |
|----------|-------|
| Barra de título, Sobre (escuro) | `LogoGeometry` com prata clara (`#F5F7FA` → `#C9D1DC`) e laranja |
| Barra de título, Sobre (claro) | `LogoGeometry` com prata escura (`#94A3B8` → `#5B6B80`) e laranja — equivale a `sharpshot-symbol-on-light.svg` |
| Horizontal em docs/README | `assets/branding/sharpshot-horizontal-dark.png` / `-light.png` via `<picture>` |
| Ícone do app / bandeja | `.ico` com tile escuro — não muda com o tema (o Windows não troca `.ico` por tema) |
| Notificação | `NotificationLogo.png` — fundo transparente, prata clara + laranja (**verificar legibilidade em toast claro**: pendente) |

Detalhe de cada variante em [`branding.md`](branding.md) e [`assets.md`](assets.md).

## O que não segue o tema do app

| Peça | Segue | Motivo |
|------|-------|--------|
| Menu da bandeja (Win32 `TrackPopupMenuEx`) | tema **do sistema** | Menu nativo; não há API para forçar claro/escuro em menu Win32 sem custom-draw ([ADR-06](../DECISIONS.md)) |
| Toast (`AppNotificationManager`) | tema **do sistema** | Renderizado pelo Windows ([ADR-07](../DECISIONS.md)) |
| Botões da barra de título (min/max/fechar) | tema da janela | WinUI 3 com `ExtendsContentIntoTitleBar` pinta os caption buttons conforme `RequestedTheme` — precisa setar `TitleBar.ButtonForegroundColor` etc. no `AppWindow` (**em implementação**) |
| Diálogo de pasta (`FolderPicker`) | sistema | Diálogo do shell |

Aceito e documentado; não é bug.

## Teste de regressão de tema (manual)

1. Escuro → Claro → Sistema sem reiniciar: Home, Configurações, Sobre e um overlay aberto mudam na hora.
2. Com "Seguir o sistema", trocar o modo do Windows: o app acompanha em < 1 s.
3. Alto contraste (tema "Aquatic"/"Desert"): tudo legível, laranja substituído pelo realce do sistema.
4. Contraste AA conferido nos dois temas para: texto da Home, caption do atalho, `InfoBar` de erro, badge do overlay.

Status: **pendente de validação** (App em implementação).

---

Cores: [`cores.md`](cores.md) · Componentes: [`componentes.md`](componentes.md) · Estados: [`estados.md`](estados.md)
