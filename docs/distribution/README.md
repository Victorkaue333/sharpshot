# Distribuição — SharpShot

> Como o SharpShot é publicado, empacotado e entregue: pasta **unpackaged self-contained** como caminho principal, MSIX documentado como secundário, dependências de runtime, geração dos executáveis e checklist de validação. Instalação/desinstalação em [`instalacao.md`](instalacao.md); compatibilidade em [`compatibilidade.md`](compatibilidade.md); processo de release em [`../DEPLOY.md`](../DEPLOY.md).
> Fontes: `src/SharpShot.App/SharpShot.App.csproj`, `app.manifest`, `Directory.Build.props` (verificados) · [ADR-01](../DECISIONS.md#adr-01--winui-3-unpackaged-e-self-contained-como-distribuição-primária).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves · **Nenhum publish foi executado ainda**: tudo que depende da pasta gerada está marcado "verificar na pasta publicada".

## Ordem de leitura

1. Este arquivo — caminhos de distribuição, runtime, geração, checklist.
2. [`instalacao.md`](instalacao.md) — instalar, iniciar com o Windows, desinstalar sem resíduo.
3. [`compatibilidade.md`](compatibilidade.md) — versões do Windows, arquiteturas, cenários (VM, RDP, HDR).

## Caminhos de distribuição

| Caminho | Status | Como | Prós | Contras |
|---------|--------|------|------|---------|
| **Pasta unpackaged self-contained** (zip) | **principal** (ADR-01) | `dotnet publish … --self-contained`; zip por arquitetura; GitHub Release | Sem instalador, sem admin, sem certificado, sem runtime a instalar; roda de pendrive | Pasta grande; sem atualização automática; registro de notificação/Run feito pelo próprio app |
| **MSIX** | secundário, documentado | `WindowsPackageType=MSIX` (hoje `None`), `Package.appxmanifest`, assinatura com certificado | Instalação/desinstalação limpa pelo Windows, `StartupTask`, caminho para a Store | Exige certificado de assinatura (ou Store); instalação de certificado autoassinado é atrito; identidade de pacote muda APIs (notificações, startup) |
| MSI/Inno Setup | não planejado | — | — | Mais uma peça para manter; MSIX cobre o caso |

## Pasta publicada (unpackaged)

Gerada por:

```bash
dotnet publish src/SharpShot.App/SharpShot.App.csproj -c Release -p:Platform=x64   -r win-x64   --self-contained -o artifacts/publish/win-x64
```

Propriedades do `.csproj` que definem o resultado: `WindowsPackageType=None` · `SelfContained=true` · `WindowsAppSDKSelfContained=true` · `RuntimeIdentifiers=win-x64` · `PublishTrimmed=false` · `PublishReadyToRun=false` · `UseRidGraph=false` · `ApplicationIcon=Assets\SharpShot.ico` · `AssemblyName=SharpShot`.

Conteúdo esperado (**verificar na pasta publicada**):

```text
artifacts/publish/win-x64/
├── SharpShot.exe                         host nativo (ícone embutido)
├── SharpShot.dll                         código do App
├── SharpShot.Core.dll · SharpShot.Capture.dll · SharpShot.Storage.dll
├── SharpShot.runtimeconfig.json · SharpShot.deps.json
├── coreclr.dll · clrjit.dll · hostfxr.dll · hostpolicy.dll · System.*.dll · Microsoft.Extensions.*.dll   runtime .NET 10
├── Microsoft.WindowsAppRuntime.Bootstrap.dll · Microsoft.WindowsAppRuntime.dll · Microsoft.ui.xaml.dll
│   Microsoft.UI.Xaml.Controls.dll · Microsoft.Windows.ApplicationModel.*.dll · …                         runtime do Windows App SDK (self-contained)
├── WinRT.Runtime.dll · Microsoft.Windows.SDK.NET.dll · Microsoft.WinUI.dll                                projeções
├── Microsoft.InteractiveExperiences.Projection.dll · (WebView2Loader.dll?)                                 transitivos — conferir
├── resources.pri                         recursos XAML
├── Assets/                               SharpShot.ico · Tray.ico · NotificationLogo.png · tiles
└── (msvcp140.dll · vcruntime140.dll · vcruntime140_1.dll ?)                                              ver "Dependências de runtime"
```

Tamanho da pasta e do zip: **pendente de medição** (ordem de grandeza esperada: dezenas de MB).

## Dependências de runtime

| Dependência | Vem na pasta? | Observação |
|-------------|---------------|------------|
| .NET 10 runtime | **sim** (`SelfContained=true`) | Nenhuma instalação no usuário |
| Windows App SDK 2.6.5 runtime (WinUI, Foundation, notificações, `AppInstance`) | **sim** (`WindowsAppSDKSelfContained=true`) | O bootstrap não procura o runtime instalado na máquina |
| **VC++ Redistributable** (`msvcp140.dll`, `vcruntime140.dll`, `vcruntime140_1.dll`) | **verificar na pasta publicada** | O Windows App SDK self-contained normalmente traz as DLLs do VC++ que ele precisa; se não vierem, máquinas sem o VC++ 2015–2022 Redistributable falham ao abrir. Checar com `Get-ChildItem *.dll` e testar em VM limpa (CA-20). Se faltar: adicionar `<PackageReference Include="Microsoft.VCRTForwarders.140">` ou documentar o redistribuível como requisito |
| `WebView2Loader.dll` / `Microsoft.Web.WebView2.Core.dll` | provavelmente **sim**, transitivo | O SharpShot **não usa WebView2**; vem com o pacote WinUI. Não precisa do runtime Evergreen do WebView2 instalado (só seria exigido se um `WebView2` fosse instanciado) |
| Windows Graphics Capture, D3D11, WIC, DWM | sistema | Windows 10 2004+ / 11 |
| Segoe UI Variable / Segoe Fluent Icons | sistema | Win11 tem; Win10 cai para Segoe UI / Segoe MDL2 Assets (ver [`compatibilidade.md`](compatibilidade.md)) |
| Modo de desenvolvedor, sideloading | **não** | App unpackaged não é "sideloaded" |

## Runtime VC++ incluído no pacote

O `tools/release/make-release.ps1` copia para a pasta publicada as DLLs `vcruntime140.dll`, `vcruntime140_1.dll`, `msvcp140.dll`, `msvcp140_1.dll`, `msvcp140_2.dll` e `concrt140.dll` (Redist do Visual Studio; distribuição permitida pela licença do VC++ Redistributable). Sem elas, as bibliotecas nativas do Windows App SDK exigiriam o *Microsoft Visual C++ Redistributable 2015-2022 x64* instalado na máquina. O zip final (`artifacts/release/SharpShot-<versão>-win-x64.zip`, 66,5 MB em 04/nov/2026) é autossuficiente: descompactar e executar.

## Geração dos executáveis (release)

1. Versão em `Directory.Build.props` (`<Version>`) e `app.manifest` (`assemblyIdentity version`) alinhadas; `CHANGELOG.md` com a seção da versão.
2. CI verde no commit.
3. `pwsh tools/release/make-release.ps1` — faz o `dotnet publish` `win-x64` em **Release**, inclui o runtime VC++ e remove `.pdb`.
4. Smoke test da pasta (checklist abaixo), pelo menos uma vez em VM limpa.
5. Zip: `SharpShot-<versão>-win-x64.zip` (gerado pelo script em `artifacts/release/`, com `.sha256` ao lado; os arquivos ficam na raiz do zip).
6. SHA-256 de cada zip (`Get-FileHash`) nas notas da release.
7. Tag `v<versão>` e GitHub Release **manuais** pelo mantenedor.

**Assinatura de código:** não há certificado no MVP. O SmartScreen pode mostrar "Editor desconhecido" na primeira execução de um zip baixado — comportamento esperado; documentado em [`instalacao.md`](instalacao.md). Assinar (certificado OV/EV ou Azure Trusted Signing) é decisão futura com ADR.

## Checklist de validação da pasta publicada

Executar em cada arquitetura, de preferência em VM limpa (Windows 10 22H2 e Windows 11), sem .NET, sem Windows App SDK, sem VC++ Redistributable:

- [ ] `SharpShot.exe` abre sem erro de DLL ausente (se reclamar de `vcruntime140.dll`, ver tabela acima).
- [ ] Janela principal mostra logo, `NavigationView` com Início · Configurações · Sobre; versão correta em Sobre.
- [ ] `Ctrl+Shift+1` salva PNG em `Imagens\SharpShot`; toast aparece com **Abrir pasta** / **Ver imagem** funcionando.
- [ ] `Ctrl+Shift+2` abre overlay em todos os monitores; `Esc` cancela; `Enter` salva com as dimensões do badge.
- [ ] Ícone na bandeja com os 5 itens; **Sair** encerra limpo.
- [ ] Iniciar com o Windows cria a chave `Run`; relogar abre na bandeja (`--minimized`).
- [ ] Tema Claro/Escuro/Sistema aplica.
- [ ] `settings.json` criado em `%LOCALAPPDATA%\SharpShot`; logs em `logs\`.
- [ ] Nenhuma conexão de rede do processo (Monitor de Recursos).
- [ ] Desinstalação por [`instalacao.md`](instalacao.md) não deixa resíduo.
- [ ] Registrar: tamanho da pasta/zip, lista de DLLs do VC++ presentes, build do Windows da VM, data — em [`../CHANGELOG.md`](../CHANGELOG.md).

## Caminho MSIX (secundário — não usado)

Para empacotar:

1. `WindowsPackageType=MSIX` no `.csproj` (ou remover a propriedade — MSIX é o default com `EnableMsixTooling=true`), `WindowsAppSDKSelfContained` pode ficar `true` ou passar a depender do runtime instalado via dependência de pacote.
2. Criar `Package.appxmanifest` com identidade (`Name`, `Publisher`), capacidades (nenhuma especial: WGC não exige capability para monitor em desktop; `runFullTrust`), `StartupTask` (substitui a chave `Run` — ADR-08), extensão de notificação (`com.microsoft.windows.notifications`? — verificar; o `AppNotificationManager` com identidade usa o AUMID do pacote).
3. Assinar: certificado de teste (`New-SelfSignedCertificate` + instalar em *Pessoas Confiáveis* na máquina do usuário) ou certificado comercial/Store.
4. `dotnet publish -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=true -p:PackageCertificateThumbprint=…` ou pelo VS (*Publicar → Criar pacotes do aplicativo*).
5. Os tiles já existem em `Assets/` (`Square44x44Logo`, `Square150x150Logo`, `Wide310x150Logo`, `StoreLogo`, `SplashScreen`, `LockScreenLogo`), gerados pelo script de branding.

Diferenças de comportamento com identidade de pacote: `settings.json` **continua** em `%LOCALAPPDATA%\SharpShot` (o app usa `Environment.GetFolderPath`, não `ApplicationData`), mas o MSIX pode virtualizar `%LOCALAPPDATA%` para `…\Packages\<id>\LocalCache\Local` — **verificar**; chave `Run` deve dar lugar ao `StartupTask`; notificações passam a usar a identidade do pacote. Nada disso está implementado nem testado — é o roteiro para quando o caminho MSIX/Store entrar (roadmap).

---

Instalação: [`instalacao.md`](instalacao.md) · Compatibilidade: [`compatibilidade.md`](compatibilidade.md) · Processo de release: [`../DEPLOY.md`](../DEPLOY.md) · Ambiente de build: [`../ENVIRONMENT.md`](../ENVIRONMENT.md)
