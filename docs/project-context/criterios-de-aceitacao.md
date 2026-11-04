# Critérios de aceitação — SharpShot 0.1.0

> O que precisa **passar** para o MVP existir. Cada critério é verificável (automatizado ou manual com passos), rastreia a um RF/RNF e tem um status. **Validação de 04/nov/2026** registrada na seção final deste arquivo (máquina do autor, Windows 11 26200, 1920×1080 @ 100 % + monitor retrato 1080×1920). Marcar `[x]` só com evidência (saída do teste, data e máquina do teste manual).
> Fontes: [`requisitos.md`](requisitos.md) · [`../PRD.md`](../PRD.md).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Legenda

- **Auto** — teste xUnit em `tests/` (planejado).
- **Manual** — roteiro executado por pessoa; registrar data, build do Windows e configuração de monitores.
- **VM limpa** — Windows 10 22H2 ou 11 sem .NET nem Windows App SDK instalados.

## Captura e fidelidade

- [ ] **CA-01 · Tela inteira em multi-DPI** (RF-01, RNF-03) — *Manual.* Dois monitores (ex.: 2560×1440 @ 150 % e 1920×1080 @ 100 %). `Ctrl+Shift+1` com o cursor em cada um salva um PNG com as dimensões **físicas** exatas daquele monitor (2560×1440 e 1920×1080). Repetir com o monitor secundário posicionado à esquerda (coordenadas negativas).
- [ ] **CA-02 · Round-trip sem perdas** (RF-03, RNF-01) — *Auto* (`Capture.Tests`). `PixelBuffer` sintético com gradientes + ruído → `PngImageEncoder.EncodeAsync` → `BitmapDecoder` → `GetPixelDataAsync(Bgra8, Ignore)` é **byte-idêntico** ao buffer de origem (alpha = 255 nos dois).
- [ ] **CA-03 · Região exata** (RF-02, RNF-03) — *Manual.* No monitor em 150 %, selecionar uma região até o badge mostrar "1280 × 720"; o PNG resultante tem 1280×720 px e o conteúdo corresponde ao que o overlay mostrava (sem deslocamento de 1 px nas bordas — comparar com a captura de tela inteira recortada em um editor).
- [ ] **CA-04 · Sem o próprio app na captura** (RF-12) — *Manual.* Com a janela principal visível e centralizada, `Ctrl+Shift+1`: o PNG mostra o que estava **atrás** da janela, sem nenhum traço dela. Idem com o overlay aberto: a captura de região nunca contém a máscara, a borda ou o badge.
- [ ] **CA-05 · Nunca sobrescreve** (RF-03) — *Auto* (`Core.Tests`) + *Manual.* `FileNaming.MakeUnique` devolve `_1`, `_2`… quando o nome existe; manualmente, duas capturas em sequência rápida nunca perdem a primeira. Arquivo existente bloqueado por outro processo → novo nome, não erro.
- [ ] **CA-06 · Cancelar não grava** (RF-02) — *Manual.* `Ctrl+Shift+2`, desenhar, `Esc`: nenhum arquivo novo na pasta; overlays de todos os monitores fecharam; janela principal volta ao estado anterior (visível ou na bandeja).

## Notificação, status e erros

- [ ] **CA-07 · Toast com ações** (RF-04) — *Manual.* Após salvar: toast "Captura salva com sucesso!" com o nome do arquivo; **Abrir pasta** abre o Explorer com o arquivo selecionado; **Ver imagem** abre o visualizador padrão. Com "Mostrar notificação" desligado, nenhum toast. Em sessão sem suporte a notificações, a captura é salva mesmo assim e o log registra o motivo.
- [ ] **CA-08 · Erros de gravação distintos** (RF-07, RNF-05) — *Auto* (classificação) + *Manual.* Pasta somente leitura → mensagem de permissão; unidade cheia (ou cota) → mensagem de disco cheio; `C:\con\x` → nome reservado; `relativa\pasta` → "informe um caminho completo"; unidade removida → pasta não encontrada. Cada caso mostra a mensagem correspondente de [`../architecture/erros.md`](../architecture/erros.md) e nenhum arquivo parcial fica na pasta.
- [ ] **CA-18 · Linha de status** (RF-15) — *Manual.* Após captura, a Home mostra o caminho completo do arquivo; após erro, a mensagem classificada; a linha "Salvar em" reflete a pasta efetiva (padrão ou configurada, com `%VAR%` expandido).

## Configurações e persistência

- [ ] **CA-09 · settings.json robusto** (RF-16) — *Auto* (`Core.Tests`). Round-trip de `AppSettings` com todos os campos; arquivo com `"fullScreenHotkey": "1"` (sem modificador) carrega com o padrão; arquivo com JSON inválido é renomeado para `settings.json.corrupt-<timestamp>` e os padrões assumem; `saveDirectory: "   "` vira `null`; gravação é atômica (não existe `.tmp` após salvar).
- [ ] **CA-11 · Atalhos e conflito** (RF-06) — *Manual.* Registrar `Ctrl+Shift+1` em outro app (ex.: AutoHotkey) antes de abrir o SharpShot: `InfoBar` de conflito aparece, o outro atalho continua funcionando; trocar para `Ctrl+Alt+1` resolve; **Restaurar padrões** volta a `Ctrl+Shift+1`/`2`; combinação sem modificador é recusada pelo editor.
- [ ] **CA-12 · Iniciar com o Windows** (RF-08) — *Manual.* Ligar o toggle cria `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\SharpShot` = `"<caminho>\SharpShot.exe" --minimized`; desligar remove; reiniciar a sessão abre o app **só na bandeja**; desativar pelo Gerenciador de Tarefas é refletido ao reabrir Configurações.
- [ ] **CA-14 · Tema** (RF-10) — *Manual.* Claro/Escuro aplicam na hora sem reiniciar; "Seguir o sistema" acompanha a troca do tema do Windows; a escolha persiste após reiniciar o app.

## Bandeja, instância e ciclo de vida

- [ ] **CA-10 · Bandeja completa** (RF-05) — *Manual.* Ícone presente após abrir; menu com os 5 itens funcionando; `taskkill /f /im explorer.exe` + reiniciar o Explorer → o ícone volta sozinho.
- [ ] **CA-13 · Fechar e Sair** (RF-09) — *Manual.* Com "Minimizar para a bandeja" ligado, o X esconde a janela e os atalhos continuam funcionando; com a opção desligada, o X encerra. **Sair** encerra sem ícone fantasma na bandeja, sem atalho registrado (outro app consegue registrar `Ctrl+Shift+1` em seguida) e sem processo residual.
- [ ] **CA-15 · Instância única** (RF-11) — *Manual.* Com o app na bandeja, executar `SharpShot.exe` de novo mostra a janela da instância existente e não cria segundo ícone nem segundo processo.
- [ ] **CA-16 · Sobre** (RF-13) — *Manual.* Versão exibida = `0.1.0` (`Directory.Build.props`); link do repositório abre no navegador; licença MIT e créditos presentes.
- [ ] **CA-17 · Logs** (RF-14) — *Manual.* Após uma captura e um erro provocado, `%LOCALAPPDATA%\SharpShot\logs` contém as linhas correspondentes com timestamp; nenhum byte de imagem no log.

## Engenharia e distribuição

- [ ] **CA-19 · CI verde** (RNF-09) — *Auto.* `build.yml` em `windows-latest`: restore, build Debug x64, `Core.Tests` e `Capture.Tests` verdes (integração de captura **pulada** com motivo no runner), publish win-x64 gera artifact.
- [ ] **CA-20 · Roda em VM limpa** (RNF-02, RNF-08) — *Manual.* Extrair o zip publicado em uma VM Windows 10 22H2 e em uma Windows 11 sem .NET nem Windows App SDK: `SharpShot.exe` abre, CA-01/CA-03/CA-07 passam, e apagar a pasta + `%LOCALAPPDATA%\SharpShot` + chave `Run` não deixa resíduo (registro de notificação conferido).
- [ ] **CA-21 · Core sem Windows** (RNF-09) — *Auto.* `Core.Tests` roda com `dotnet test` em um runner que **não** tem Windows App SDK; `SharpShot.Core.dll` e `SharpShot.Storage.dll` não referenciam `Microsoft.Windows.SDK.NET` nem `WinRT.Runtime` (verificar com `ildasm`/reflexão — teste de arquitetura sugerido).
- [ ] **CA-22 · Sem rede** (RNF-04) — *Manual.* Durante 10 capturas e navegação por todas as páginas, um monitor de conexões (ex.: Monitor de Recursos) não mostra nenhuma conexão do processo `SharpShot.exe`.

## Resumo

| Grupo | Critérios | Auto | Manual |
|-------|-----------|------|--------|
| Captura e fidelidade | CA-01..06 | 2 | 5 |
| Notificação, status, erros | CA-07, CA-08, CA-18 | 1 | 3 |
| Configurações | CA-09, CA-11, CA-12, CA-14 | 1 | 3 |
| Bandeja e ciclo de vida | CA-10, CA-13, CA-15, CA-16, CA-17 | 0 | 5 |
| Engenharia e distribuição | CA-19..22 | 2 | 2 |

**0 de 22 validados** em 04/nov/2026.

---

Requisitos: [`requisitos.md`](requisitos.md) · Contexto: [`README.md`](README.md) · Erros: [`../architecture/erros.md`](../architecture/erros.md) · Distribuição: [`../distribution/README.md`](../distribution/README.md)

## Validação executada em 04/nov/2026

Ambiente: máquina do autor — Windows 11 (26200), monitor principal 1920×1080 @ 100 %, segundo monitor retrato 1080×1920; builds Debug e Release (`artifacts/publish/win-x64`). Evidências: PNGs gerados em `Imagens\SharpShot` (dimensões conferidas com Pillow), `%LOCALAPPDATA%\SharpShot\logs`, `settings.json`, capturas da UI e saída do `dotnet test`.

| Critério | Resultado | Evidência |
|----------|-----------|-----------|
| Captura de tela inteira por `Ctrl+Shift+1` (Debug e Release) | **validado** | `SharpShot_2026-11-04_13-55-56-743.png` 1920×1080 (e outras 5), log "Captured \\.\DISPLAY1: 1920x1080 px" |
| Janela do SharpShot ausente da captura | **validado** | captura inspecionada; `WDA_EXCLUDEFROMCAPTURE` + `DwmFlush` |
| Seleção de região por `Ctrl+Shift+2` (overlay nos 2 monitores, arrastar, `Enter`) | **validado** | recortes exatos 600×400, 500×300, 300×240 (2 capturas consecutivas) |
| Overlay: máscara, borda laranja tracejada, 8 handles, badge "500 × 300", Capturar/Cancelar, `Esc` | **validado** | captura da UI do overlay |
| Nome `SharpShot_AAAA-MM-DD_HH-mm-ss-fff.png` em `Imagens\SharpShot`, sem sobrescrita | **validado** | arquivos gerados; teste `FileNaming`/`ScreenshotStorageService` |
| PNG sem perdas (byte a byte) | **validado** | `PngImageEncoderTests` (round-trip via `BitmapDecoder`) |
| Toast "Captura salva com sucesso!" com Abrir pasta / Ver imagem | **validado** | toast exibido; "Ver imagem" abriu a captura |
| Configurações persistem em `settings.json` (toggles, tema) | **validado** | JSON conferido após cada clique |
| Iniciar com o Windows grava/remove `HKCU\...\Run` com `--minimized` | **validado** | chave conferida via registro |
| Editor de atalhos (lápis → teclas) regrava e re-registra | **validado** | `fullScreenHotkey: Ctrl+Alt+F9` no JSON e na UI |
| Fechar janela → oculta na bandeja, processo continua | **validado** | `IsWindowVisible=false`, processo vivo |
| Ícone na bandeja | **validado** | log "Tray icon added"; ícone visível na barra |
| Restaurar padrões (via UI) | implementado · **pendente** | caminho `SetHotkeyAsync` validado pelo editor; clique na UI não reproduzido no teste automatizado |
| Menu da bandeja (ações e Sair) | implementado · **pendente** | não exercitado |
| `--minimized`, instância única, ativação de toast com o app fechado | implementado · **pendente** | não exercitados |
| DPI 125/150/200 %, DPI misto, HDR, Windows 10, apps em tela cheia exclusiva | **pendente** | ambiente não disponível |
| Testes automatizados | **validado** | Core 285 ✔ · Capture 35 ✔ (Debug e Release) |
| Pacote de release (`make-release.ps1`) e exe publicado | **validado** | zip 66,5 MB; exe publicado capturou 1920×1080 |
| CI no GitHub | **pendente** | workflow escrito, nunca executado (sem push) |

Ocorrência registrada: uma saída silenciosa do processo logo após uma captura de região em uma execução inicial (máquina com 92 % de RAM em uso e botões de toast sendo clicados na mesma execução). Depois disso foram adicionados logging global de exceções e endurecimento do overlay; não voltou a ocorrer em várias execuções. Não está confirmado como corrigido.
