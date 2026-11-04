# Ícones — Design system

> Ícones funcionais (Segoe Fluent Icons), ícones do app/bandeja/notificação (gerados) e a regra que não se negocia: **o logo nunca é substituído por um ícone genérico**.
> Fontes: `assets/icons/`, `src/SharpShot.App/Assets/`, `tools/branding/generate_branding.py` (verificados) · WinUI 3 (`FontIcon`) · App (em implementação).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Ícones funcionais (fonte do sistema)

`FontIcon` com `Segoe Fluent Icons` (Windows 11; no Windows 10 o WinUI 3 cai para `Segoe MDL2 Assets`, que tem os mesmos code points para estes glifos). Tamanho padrão 16 DIP; 20 em itens de navegação; 32 nos cards da Home.

| Ação / lugar | Glifo | Code point | Nome no catálogo |
|--------------|-------|------------|------------------|
| Capturar tela inteira (card, menu) | câmera | `E722` | Camera |
| Selecionar região (card, menu) | retângulo com cantos | `E7A8` | Crop (alternativa `EF20` *ScreenCast* — **escolher; pendente**) |
| Abrir pasta | pasta | `E8B7` | Folder |
| Ver imagem | imagem | `EB9F` | Picture |
| Início (nav) | casa | `E80F` | Home |
| Configurações (nav) | engrenagem | `E713` | Setting |
| Sobre (nav) | círculo i | `E946` | Info |
| Sair (menu) | porta/seta | `E7E8` | PowerButton (ou `E8BB` *ChromeClose* — **pendente**) |
| Restaurar padrões | seta circular | `E777` | Undo (`E72C` *Refresh* é alternativa) |
| Procurar… | pasta aberta | `ED25` | OpenFolderHorizontal |
| Sucesso (status) | ✓ em círculo | `E73E` | CheckMark (o `InfoBar` Success já traz o próprio) |
| Erro (status) | ⊗ | `EA39` | ErrorBadge (idem) |
| Aviso (conflito) | ⚠ | `E7BA` | Warning (idem) |
| Atalho de teclado (editor) | teclado | `E765` | KeyboardClassic |
| Tema | sol/lua | `E706` / `E708` | Brightness / QuietHours |

Regras:

- Ícone com significado recebe `AutomationProperties.Name` em pt-BR; decorativo ao lado de texto recebe `AutomationProperties.AccessibilityView="Raw"`.
- Cor: `SharpShotTextBrush` (ou `TextSecondary` quando inativo). Laranja só no hover do card e em estado selecionado — nunca como cor padrão de ícone (no claro, reprova contraste — [`cores.md`](cores.md)).
- Sem PNG para ícone de UI. Sem emoji como ícone.

## Ícones do app (gerados — ADR-13)

Todos saem de `tools/branding/generate_branding.py`; não editar à mão. Inventário completo e tamanhos em [`assets.md`](assets.md) (dono: agente de branding).

| Arquivo | Conteúdo | Uso |
|---------|----------|-----|
| `assets/icons/sharpshot.ico` → `src/SharpShot.App/Assets/SharpShot.ico` | Símbolo em tile escuro, 16/24/32/48/64/128/256 | `ApplicationIcon` do `.csproj` (ícone do `.exe`, barra de tarefas, Alt+Tab) |
| `assets/icons/sharpshot-<n>.png` | Mesmos tamanhos em PNG | Web/README, Store futura |
| `assets/icons/tray.ico` → `src/SharpShot.App/Assets/Tray.ico` | Variante para bandeja (contraste em 16 px) | `Shell_NotifyIcon` |
| `assets/icons/tray-<n>.png` | Idem em PNG | — |
| `src/SharpShot.App/Assets/NotificationLogo.png` | Símbolo com fundo transparente | `AppNotification` (app logo override) |
| `src/SharpShot.App/Assets/Square44x44Logo*.png`, `Square150x150Logo`, `Wide310x150Logo`, `StoreLogo`, `SplashScreen`, `LockScreenLogo` | Tiles MSIX | Só no caminho MSIX secundário |
| `src/SharpShot.App/Components/LogoGeometry.cs` | 4 paths (`TopBracket`, `LowerSilver`, `UpperSilver`, `BottomBracket`) em caixa 256 × 256 | `LogoView` (barra de título, Sobre) |

Tamanhos de bandeja por escala: 16 px (100 %), 20 px (125 %), 24 px (150 %), 32 px (200 %) — o Windows escolhe do `.ico` o mais próximo; `tray.ico` inclui 16/24/32/48/64/128/256. Se a nitidez em 20 px (125 %) ficar ruim, acrescentar 20 px ao script (**pendente de validação visual**).

## Logo na UI

- **Barra de título:** `LogoView` 20 DIP + wordmark.
- **Sobre:** `LogoView` 96 DIP.
- **Toast:** `NotificationLogo.png`.
- **Nunca:** `FontIcon` de câmera no lugar do logo; logo esticado; logo recolorido fora dos brushes por tema ([`temas.md`](temas.md)).

## Cursores (overlay)

| Situação | `InputSystemCursorShape` |
|----------|--------------------------|
| Fora da seleção | `Cross` |
| Dentro (mover) | `SizeAll` |
| Handles de canto | `SizeNorthwestSoutheast` / `SizeNortheastSouthwest` |
| Handles de aresta | `SizeNorthSouth` / `SizeWestEast` |
| Sobre a barra Capturar/Cancelar | `Arrow` |

---

Componentes: [`componentes.md`](componentes.md) · Assets: [`assets.md`](assets.md) · Marca: [`branding.md`](branding.md)
