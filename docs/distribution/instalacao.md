# Instalação e desinstalação — SharpShot

> O SharpShot não tem instalador: instala-se copiando uma pasta e desinstala-se apagando o que ele criou. Este arquivo lista **exatamente** o que o app toca no sistema, para a desinstalação não deixar resíduo. Tudo é por usuário (`HKCU`, `%LOCALAPPDATA%`, pasta Imagens); nada exige administrador.
> Fontes: [`../DATABASE.md`](../DATABASE.md) · [ADR-01](../DECISIONS.md), [ADR-07](../DECISIONS.md), [ADR-08](../DECISIONS.md) · App (em implementação) para `Run` e notificações.
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves · **Pendente de validação**: nenhum release existe; os passos descrevem o comportamento projetado e verificado no `.csproj`/`Storage`.

## Instalar

1. Baixe `SharpShot-<versão>-win-x64.zip` da página de Releases do GitHub. Confira o SHA-256 (arquivo `.sha256` anexado).
2. Extraia para uma pasta de sua escolha, por exemplo `C:\Users\<você>\Apps\SharpShot\` ou `D:\Ferramentas\SharpShot\`. Evite `C:\Program Files` (exige admin e o app não precisa disso).
3. Execute `SharpShot.exe`. Na primeira execução de um arquivo baixado, o **SmartScreen** pode avisar "Editor desconhecido" (o MVP não é assinado): *Mais informações → Executar assim mesmo*. Alternativa: *Propriedades → Desbloquear* no zip antes de extrair.
4. O app abre na janela principal; o ícone aparece na bandeja. Pronto: `Ctrl+Shift+1` captura a tela inteira; `Ctrl+Shift+2` seleciona região.

Nada é instalado fora da pasta até você ligar opções específicas (abaixo). A pasta pode ser movida depois; o único ponto que guarda o caminho é a chave `Run`, que o app reescreve ao iniciar.

## O que o app cria no sistema

| Item | Caminho | Quando é criado | Dono |
|------|---------|-----------------|------|
| Configurações | `%LOCALAPPDATA%\SharpShot\settings.json` (+ `settings.json.tmp` transitório, `settings.json.corrupt-*` se houver backup) | Primeira gravação de configuração | `SettingsService` (implementado) |
| Logs | `%LOCALAPPDATA%\SharpShot\logs\*.log` | Primeira execução | App (em implementação) |
| Capturas | `<Imagens>\SharpShot\SharpShot_*.png` (padrão) ou a pasta configurada | Primeira captura | `ScreenshotStorageService` (implementado) |
| Iniciar com o Windows | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` → valor `SharpShot` = `"<pasta>\SharpShot.exe" --minimized` | Só se a opção for ligada | App (em implementação) |
| Registro de notificações | Registro por usuário do `AppNotificationManager` (AUMID `SharpShot` + servidor COM de ativação apontando para o `.exe`), em `HKCU\Software\Classes\…` | Primeira execução (`Register()`), se suportado | App (em implementação) |

Não toca: `HKLM`, `Program Files`, `ProgramData`, pasta Inicializar, serviços, tarefas agendadas, associações de arquivo, variáveis de ambiente, firewall (não há rede).

## Iniciar com o Windows

Configurações → **Geral** → *Iniciar com o Windows*. O app escreve o valor acima; o Windows executa `SharpShot.exe --minimized` no logon e o app nasce na bandeja. Desligar a opção remove o valor. Se você desativar pelo **Gerenciador de Tarefas → Aplicativos de inicialização**, o Windows marca o item como desativado (em `…\Explorer\StartupApproved\Run`) — o app lê o estado real ao abrir Configurações e mostra o toggle desligado.

Se mover a pasta do SharpShot com a opção ligada, abra o app uma vez da nova pasta: ele reescreve o caminho na chave.

## Atualizar

Feche o SharpShot (**Sair** no menu da bandeja), apague o conteúdo da pasta antiga e extraia o novo zip no mesmo lugar (ou em outro — a chave `Run` é corrigida na próxima execução). `settings.json` e as capturas ficam onde estão; versões `0.x` mantêm o `schemaVersion` compatível ([`../DATABASE.md`](../DATABASE.md)). Não há verificação automática de versão (ADR-15).

## Desinstalar (sem resíduo)

1. **Sair** do SharpShot pelo menu da bandeja (libera atalhos e ícone).
2. **Desligar "Iniciar com o Windows"** nas Configurações antes de sair — ou remover manualmente:

   ```powershell
   Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "SharpShot" -ErrorAction SilentlyContinue
   Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run" -Name "SharpShot" -ErrorAction SilentlyContinue
   ```

3. **Apagar a pasta** onde `SharpShot.exe` foi extraído.
4. **Apagar os dados** (configurações e logs):

   ```powershell
   Remove-Item -Recurse -Force "$env:LOCALAPPDATA\SharpShot" -ErrorAction SilentlyContinue
   ```

5. **Registro de notificações.** O caminho limpo é o app chamar `AppNotificationManager.Default.Unregister()` — planejado como opção **Sobre → Remover registro de notificações** ou argumento `SharpShot.exe --unregister` (**decisão pendente no App**). Manualmente: Configurações do Windows → Sistema → Notificações → remover/desligar "SharpShot"; a entrada de classe COM por usuário fica em `HKCU\Software\Classes\CLSID\{…}` e `HKCU\Software\Classes\AppUserModelId\SharpShot` (nomes exatos **verificar na primeira execução registrada**).
6. **Capturas**: ficam. Apague `Imagens\SharpShot` só se quiser.

Checklist de verificação de resíduo (CA-20): `Get-ChildItem "$env:LOCALAPPDATA\SharpShot"` → inexistente; `Get-ItemProperty HKCU:\…\Run` → sem `SharpShot`; Notificações do Windows → sem SharpShot; nenhum processo `SharpShot.exe`.

## Instalação portátil (pendrive)

Funciona: a pasta é autossuficiente. Observações: `settings.json` continua em `%LOCALAPPDATA%` da máquina (não na pasta) — modo "portátil de verdade" (configurações ao lado do `.exe`) **não existe** no MVP; é candidato a roadmap. Não ligue "Iniciar com o Windows" em pendrive.

## Vários usuários na mesma máquina

Cada usuário tem o próprio `settings.json`, a própria chave `Run` e o próprio registro de notificações. A pasta do `.exe` pode ser compartilhada (por exemplo em `C:\Apps\SharpShot`), desde que todos tenham permissão de leitura/execução.

---

Distribuição: [`README.md`](README.md) · Compatibilidade: [`compatibilidade.md`](compatibilidade.md) · Persistência: [`../DATABASE.md`](../DATABASE.md)
