# PRD — SharpShot

> **Documento de Requisitos do Produto.** Camada de produto — fina e navegável: descreve o **quê** e delega o **como** à arquitetura e ao código. Os requisitos numerados (RF-xx, RNF-xx) e os critérios de aceitação completos vivem em [`project-context/`](project-context/README.md); este arquivo resume.
> Versão alvo: **0.1.0 (MVP)** · Status: **em desenvolvimento** · Mantenedor: Victor Alves.
> **Precedência:** havendo divergência entre este PRD e [`project-context/requisitos.md`](project-context/requisitos.md), **os requisitos numerados prevalecem**; havendo divergência com o código, o código é a realidade e o doc precisa ser corrigido.
> Última atualização: 04/nov/2026

## Visão

**SharpShot** é uma ferramenta nativa de captura de tela para Windows 10/11 que faz uma coisa bem: tirar a foto exata do que está na tela — pixel a pixel, sem reamostragem, sem compressão com perdas — e salvá-la em PNG com um atalho de teclado. Não é um editor, não é um gravador de vídeo, não é um serviço na nuvem.

Diferenciais do MVP: captura pela **Windows Graphics Capture** (funciona com DirectX e apps em tela cheia), **DPI exato** em monitores com escalas diferentes, **seleção de região sobre frame congelado** (o que você vê é o que sai), **zero rede** (nada sai do computador), **sem instalador** (copie a pasta e execute) e **código aberto** sob MIT.

## Personas

| Persona | Necessidade | Contexto de uso |
|---------|-------------|-----------------|
| **Desenvolvedor / QA** | Print fiel de UI para bug report, documentação e PR | Vários monitores, DPIs misturados, apps com DirectX; quer o arquivo na pasta certa com nome ordenável |
| **Redator técnico / suporte** | Recorte limpo de uma região, sem cursor, nome previsível | Tira dezenas por dia; valoriza atalho fixo, toast com "Abrir pasta" |
| **Usuário comum** | Substituir o `PrtScn` por algo que salva direto como arquivo | Quer que "simplesmente funcione", em português, sem configurar nada |
| **Usuário preocupado com privacidade** | Garantia de que a imagem não sai da máquina | Verifica que não há rede, telemetria nem conta |

## Requisitos funcionais (resumo)

IDs completos, prioridade e critério de aceite por requisito em [`project-context/requisitos.md`](project-context/requisitos.md). **Deve** = inegociável no MVP; **Deveria** = entra se couber; **Roadmap** = não implementar agora.

| ID | Requisito | Prioridade | Status |
|----|-----------|------------|--------|
| RF-01 | Captura de tela inteira (monitor principal) por atalho global `Ctrl+Shift+1`, pelo card da Home e pelo menu da bandeja | Deve | implementado e validado |
| RF-02 | Seleção de região por `Ctrl+Shift+2`: overlay por monitor com frame congelado, máscara, borda + 8 handles, badge de tamanho, `Enter`/`Esc` | Deve | implementado e validado |
| RF-03 | Salvar PNG sem perdas na pasta configurada; nunca sobrescrever; nome `SharpShot_yyyy-MM-dd_HH-mm-ss-fff.png` | Deve | implementado (Storage + Encoder) |
| RF-04 | Notificação "Captura salva com sucesso!" com nome do arquivo e ações **Abrir pasta** / **Ver imagem** (desligável) | Deve | implementado |
| RF-05 | Ícone na bandeja com menu: Capturar tela inteira · Selecionar região · Abrir SharpShot · Abrir pasta de capturas · Sair | Deve | implementado |
| RF-06 | Atalhos configuráveis (editor por teclas), **Restaurar padrões**, detecção de conflito com outro app (`InfoBar`) | Deve | implementado e validado |
| RF-07 | Pasta de salvamento configurável com validação (caminho completo, nomes reservados, caracteres inválidos) e botão **Abrir pasta** | Deve | validação implementada · UI implementado |
| RF-08 | Iniciar com o Windows (chave `Run` do HKCU, `--minimizado` na bandeja) | Deve | implementado |
| RF-09 | Fechar = minimizar para a bandeja (configurável); **Sair** encerra e libera atalhos, bandeja e notificações | Deve | implementado |
| RF-10 | Tema: Seguir o sistema / Claro / Escuro | Deve | implementado e validado |
| RF-11 | Instância única (segunda execução ativa a primeira) | Deve | implementado |
| RF-12 | Janelas do SharpShot nunca aparecem na captura (`WDA_EXCLUDEFROMCAPTURE` + esconder + `DwmFlush`) | Deve | `CompositionSync` implementado · afinidade implementado |
| RF-13 | Página **Sobre** com versão, licença MIT, link do repositório e créditos de terceiros | Deveria | implementado |
| RF-14 | Logs em `%LOCALAPPDATA%\SharpShot\logs` para diagnóstico | Deveria | implementado |
| RF-15 | Linha de status na Home com o resultado da última captura (caminho) ou o erro classificado | Deveria | implementado |

## Requisitos não-funcionais (resumo)

Detalhe e método de verificação em [`project-context/requisitos.md`](project-context/requisitos.md#requisitos-não-funcionais).

- **RNF-01 · Fidelidade** — PNG byte-idêntico ao frame capturado (round-trip pela `BitmapDecoder`); sem reamostragem; captura em pixels físicos.
- **RNF-02 · Compatibilidade** — Windows 10 2004 (build 19041)+ e Windows 11; x64 (arm64 em versão futura); self-contained.
- **RNF-03 · DPI** — PerMonitorV2; seleção exata em monitores com escalas diferentes e coordenadas negativas.
- **RNF-04 · Privacidade** — nenhuma conexão de rede; nenhuma telemetria; tudo em `HKCU`/`%LOCALAPPDATA%`/pasta do usuário.
- **RNF-05 · Robustez** — todo erro de captura e de gravação é classificado (`CaptureErrorKind`, `SaveErrorKind`) e explicado em pt-BR; a captura nunca falha em silêncio; nunca sobrescreve arquivo.
- **RNF-06 · Recursos** — device D3D, frame pool, sessão, `SoftwareBitmap` e buffers liberados a cada captura; memória volta ao patamar anterior (**pendente de medição**).
- **RNF-07 · Desempenho** — captura percebida como instantânea; timeout de 5 s para o primeiro frame. **Sem números publicados até haver medição reproduzível.**
- **RNF-08 · Instalação** — sem admin, sem instalador: copiar pasta e executar; desinstalar = apagar pasta + `HKCU` + `%LOCALAPPDATA%\SharpShot`.
- **RNF-09 · Qualidade de engenharia** — `Nullable` e `AnalysisLevel=latest-recommended` em todos os projetos; lógica em `Core` sem Windows e coberta por xUnit; CI em `windows-latest`.
- **RNF-10 · UX nativa** — WinUI 3 / Fluent, controles nativos, teclado (`Enter`/`Esc`), pt-BR.

**Constraints:** solo dev · sem backend · sem dependência nativa além do Windows App SDK · branding gerado de uma fonte única · MIT.

## Fluxos principais

### Captura de tela inteira

1. Usuário pressiona `Ctrl+Shift+1` (ou card da Home, ou menu da bandeja).
2. Janela principal é escondida; `DwmFlush` ×2 garante que saiu da composição.
3. Monitor sob o cursor é capturado (WGC), convertido em `PixelBuffer`.
4. PNG codificado (WIC) e gravado com nome único na pasta configurada.
5. Toast com o nome do arquivo e ações; linha de status da Home atualizada.

### Seleção de região

1. `Ctrl+Shift+2`. Cada monitor é capturado uma vez.
2. Overlay por monitor com o frame congelado; usuário arrasta, ajusta pelos handles, vê "L × A".
3. `Enter`/**Capturar** → `PixelBuffer.Crop` → PNG → disco → toast. `Esc`/**Cancelar** → nada gravado.

### Configurar

1. Abrir Configurações (NavigationView). Seções: Geral · Atalhos · Salvamento · Aparência.
2. Cada mudança é gravada em `settings.json` (escrita atômica) e aplicada na hora (atalhos re-registrados; conflito mostrado em `InfoBar`).

Diagramas de sequência: [`architecture/fluxos.md`](architecture/fluxos.md).

## Critérios de aceite (resumo)

Definition of Done do MVP — lista completa e rastreável em [`project-context/criterios-de-aceitacao.md`](project-context/criterios-de-aceitacao.md). **Nenhum item abaixo está validado nesta data** (o App está sendo escrito; a lista é o alvo).

- [ ] `Ctrl+Shift+1` em um setup de 2 monitores com 100 % e 150 % salva um PNG com as dimensões físicas exatas do monitor sob o cursor.
- [ ] Round-trip PNG (`EncodeAsync` → `BitmapDecoder`) é byte-idêntico ao buffer de origem.
- [ ] Seleção de região mostra "1280 × 720" e o PNG resultante tem 1280×720 px.
- [ ] Nenhuma janela do SharpShot aparece em nenhuma captura.
- [ ] Duas capturas no mesmo milissegundo geram `…-fff.png` e `…-fff_1.png` — nada é sobrescrito.
- [ ] Atalho já usado por outro app é reportado como conflito; restaurar padrões volta a `Ctrl+Shift+1`/`2`.
- [ ] `settings.json` corrompido é renomeado para `.corrupt-<timestamp>` e o app abre com padrões.
- [ ] Pasta sem permissão, disco cheio e caminho inválido mostram mensagens distintas em pt-BR.
- [ ] Iniciar com o Windows cria/remove o valor em `HKCU\…\Run`; `--minimized` abre só na bandeja.
- [ ] Fechar minimiza para a bandeja; **Sair** encerra sem deixar ícone fantasma nem hotkey registrado.
- [ ] `dotnet test` dos dois projetos de teste verde no CI (`windows-latest`).
- [ ] `dotnet publish` gera uma pasta que roda em uma VM limpa de Windows 10 22H2 sem instalar nada.

## Roadmap (não implementado)

Nenhum item abaixo existe no código. Ordem indicativa, sem datas.

| Item | Nota |
|------|------|
| Histórico de capturas | Página "Histórico" no `NavigationView` (hoje só Início · Configurações · Sobre); provavelmente SQLite |
| Captura de janela | `IGraphicsCaptureItemInterop.CreateForWindow` já declarado |
| Captura com cursor | `IsCursorCaptureEnabled = true` como opção |
| JPEG / WebP | Novos `IImageEncoder` via WIC; seletor de formato |
| Copiar para a área de transferência | Opção pós-captura |
| Anotações | Setas, retângulos, texto — editor leve |
| i18n | `.resw` + en-US |
| MSIX / Microsoft Store | Caminho secundário já documentado em [`distribution/`](distribution/README.md) |
| HDR 16-bit | Pool em FP16 + tone mapping ou PNG 16-bit; ver [`capture-engine/hdr.md`](capture-engine/hdr.md) |

## Fora de escopo

Não implementar sem decisão registrada em [`DECISIONS.md`](DECISIONS.md): backend, API REST, banco de dados remoto, autenticação/conta, telemetria, sincronização, upload para nuvem, gravação de vídeo/GIF, OCR, suporte a macOS/Linux.

---

Contexto de negócio: [`../PROJECT_CONTEXT.md`](../PROJECT_CONTEXT.md) · Requisitos numerados: [`project-context/requisitos.md`](project-context/requisitos.md) · Arquitetura: [`ARCHITECTURE.md`](ARCHITECTURE.md)
