# Depuração — SharpShot

> Como depurar o SharpShot no Visual Studio e na linha de comando, onde olhar quando algo falha e como lidar com os cenários que não cabem num breakpoint simples: hotkeys globais, overlay em tela cheia, captura WGC, bandeja.
> Fontes: código de `src/` · [`../architecture/erros.md`](../architecture/erros.md) · [`../architecture/recursos.md`](../architecture/recursos.md).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Visual Studio

| Ajuste | Onde | Por quê |
|--------|------|---------|
| Projeto de inicialização `SharpShot.App`, plataforma **x64** | barra de ferramentas | AnyCPU não compila WinUI |
| Desmarcar *Apenas Meu Código* | Ferramentas → Opções → Depuração → Geral | Ver pilhas dentro de WinRT/CsWinRT e interop |
| Ligar *Exceções Comuns do Linguagem CLR* → `System.Runtime.InteropServices.COMException` | Janela *Configurações de Exceção* (Ctrl+Alt+E) | Quebrar no HRESULT original antes de virar `CaptureException` |
| Símbolos da Microsoft (`Microsoft Symbol Servers`) | Opções → Depuração → Símbolos | Pilhas legíveis em `Microsoft.ui.xaml.dll`, `d3d11.dll` |
| *Depuração nativa* (Native Code Debugging) | Propriedades do projeto App → Depurar | Só quando precisar ver dentro do DWM/D3D; deixa o F5 mais lento |
| Hot Reload de XAML | automático em Debug | Alterações de layout sem reiniciar |

F5 roda o `SharpShot.exe` unpackaged direto da pasta de build — não há deploy de pacote nem registro.

## Linha de comando

```bash
# anexar depurador ao processo já rodando
# (VS: Depurar → Anexar ao Processo → SharpShot.exe)

# ver o log em tempo real (PowerShell)
Get-Content "$env:LOCALAPPDATA\SharpShot\logs\*.log" -Wait -Tail 50
```

## Onde olhar

| O quê | Onde |
|-------|------|
| Logs do app | `%LOCALAPPDATA%\SharpShot\logs\` (provider de arquivo — em implementação) e janela **Output → Debug** do VS (`DebugLoggerProvider`) |
| Configurações em efeito | `%LOCALAPPDATA%\SharpShot\settings.json` (+ `.corrupt-*` se houve backup) |
| Capturas | pasta configurada / `Imagens\SharpShot` |
| Chave de inicialização | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\SharpShot` (`regedit` ou `Get-ItemProperty`) |
| Registro de notificações | Configurações do Windows → Sistema → Notificações → SharpShot |
| Handles GDI/USER vazando | Gerenciador de Tarefas → Detalhes → colunas *Objetos GDI* / *Objetos USER* |
| Memória após N capturas | Gerenciador de Tarefas / *Diagnostic Tools* do VS (Memory Usage) |
| Eventos de D3D | *Diagnostic Tools* ou PIX (só se precisar) |

## Cenários difíceis

### Hotkey global

- `WM_HOTKEY` chega à janela message-only, não à janela XAML. Breakpoint no `WndProc` dessa janela (`HotkeyService`).
- Para confirmar que o atalho está registrado pelo processo certo: outro app (AutoHotkey) tentando registrar a mesma combinação deve falhar com 1409.
- O depurador do VS captura `Ctrl+Shift+1`? Não — mas `Ctrl+Shift+F`-like podem colidir com atalhos do próprio VS quando ele tem foco. Teste com o VS minimizado.
- `MOD_NOREPEAT` impede repetição ao segurar a tecla; se receber dois `WM_HOTKEY`, o flag não foi passado.

### Overlay de região

- Breakpoint dentro do overlay esconde a tela atrás do VS e distorce o teste. Use `Debug.WriteLine`/log de `PixelRectangle` a cada evento de ponteiro, ou um segundo monitor para o VS.
- Para inspecionar o frame congelado sem a UI: grave-o em PNG com `PngImageEncoder` antes de abrir o overlay (código temporário, nunca commitado).
- Coordenadas erradas por 1 px: conferir se o `RasterizationScale` do overlay bate com `MonitorInfo.ScaleFactor` e se a conversão usa `AwayFromZero` ([`../capture-engine/dpi-e-monitores.md`](../capture-engine/dpi-e-monitores.md)).
- Overlay aparece na captura: `WDA_EXCLUDEFROMCAPTURE` não aplicado no `HWND` certo (`WindowNative.GetWindowHandle(window)`), ou aplicado depois da primeira composição.

### Captura WGC

- `IsSupported` falso dentro do depurador remoto/RDP: esperado; teste em sessão local.
- `Timeout` (5 s) ao depurar com breakpoint entre `StartCapture` e o `await`: o frame chega no pool e fica esperando; sem breakpoint ali.
- Para testar o caminho WARP: desabilitar o adaptador de vídeo no Gerenciador de Dispositivos (VM) ou forçar `D3DDriverTypeWarp` em código temporário.
- Para testar device-lost: `dxcap -forcetdr` (ferramenta do Windows SDK) provoca um TDR; a próxima captura deve recriar o device e funcionar.
- Borda amarela aparecendo: `GraphicsCaptureAccess.RequestAccessAsync(Borderless)` devolveu algo diferente de `Allowed` — o log em `Debug` mostra o status.

### Bandeja

- Ícone não aparece: `Shell_NotifyIcon` devolve `false` → `GetLastError`; `cbSize` errado da `NOTIFYICONDATA` é a causa clássica.
- Ícone some após `explorer.exe` reiniciar: o handler de `TaskbarCreated` não está readicionando. Teste: `taskkill /f /im explorer.exe` e reinicie o Explorer.
- Menu abre e fecha sozinho: faltou `SetForegroundWindow(hwnd)` antes de `TrackPopupMenuEx` (comportamento documentado do Windows).

### Notificações

- `Register()` lança em sessão sem suporte: o app deve seguir sem toast; se o processo cair, falta o `try/catch` do ADR-07.
- Botões do toast não fazem nada: a ativação chega por COM em outra thread → `DispatcherQueue.TryEnqueue`.
- Toast com ícone errado: `NotificationLogo.png` não copiado para a pasta de saída (`Content Include="Assets\**"`).

### settings.json

- Mudança não persiste: `SaveAsync` lançou (olhar log) ou a UI editou `Current` direto em vez de clonar + salvar.
- App abre com padrões "do nada": há um `settings.json.corrupt-*` ao lado — o arquivo anterior estava inválido; abra-o para ver o quê.

## Checagens rápidas de regressão (manuais)

1. Dois monitores com escalas diferentes: captura de cada um nas dimensões físicas certas.
2. Região com badge "1280 × 720" → PNG 1280×720.
3. Captura com a janela principal no centro → ela não aparece.
4. Sair pelo menu → sem ícone fantasma, sem processo, `Ctrl+Shift+1` livre.
5. `settings.json` com lixo → backup criado, app abre.

Os critérios formais estão em [`../project-context/criterios-de-aceitacao.md`](../project-context/criterios-de-aceitacao.md).

---

Setup: [`README.md`](README.md) · Comandos: [`comandos.md`](comandos.md) · Erros: [`../architecture/erros.md`](../architecture/erros.md)
