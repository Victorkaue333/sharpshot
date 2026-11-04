# Desenvolvimento — SharpShot

> Como montar o ambiente, compilar, testar, executar e depurar o SharpShot. Comandos exatos em [`comandos.md`](comandos.md); depuração em [`debug.md`](debug.md); estilo de código em [`../Code-Style.md`](../Code-Style.md).
> Fontes: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.csproj` (verificados) · [`../ENVIRONMENT.md`](../ENVIRONMENT.md).
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Ordem de leitura

1. Este arquivo — pré-requisitos, instalação do SDK, Visual Studio, primeiro build.
2. [`comandos.md`](comandos.md) — todos os comandos com flags e saída esperada.
3. [`debug.md`](debug.md) — F5, logs, símbolos, cenários difíceis (hotkey, overlay, WGC).
4. [`../Code-Style.md`](../Code-Style.md) — antes de escrever código.
5. [`../../CONTRIBUTING.md`](../../CONTRIBUTING.md) — antes de abrir PR.

## Pré-requisitos

| Item | Obrigatório | Detalhe |
|------|-------------|---------|
| **Windows 10 2004 (19041)+ ou Windows 11**, sessão de desktop | sim | O App e `Capture.Tests` só rodam em Windows; o motor exige sessão de desktop (WGC). `Core.Tests` roda em qualquer runner .NET |
| **.NET SDK 10.0.x** | sim | `global.json` pede `10.0.100` com `rollForward: latestFeature` — qualquer SDK 10.0.1xx ou mais novo (ex.: 10.0.4xx) serve; 9.x ou 11.x **não** |
| **Windows SDK 10.0.26100** (targeting pack) | sim, automático | Vem pelo NuGet no restore (`Microsoft.Windows.SDK.NET.Ref`); os BuildTools 10.0.26100.4654 vêm como dependência transitiva do Windows App SDK. Não precisa do Visual Studio Installer |
| **Visual Studio 2022 17.14+** | opcional | Para XAML designer/hot reload e debug confortável. Workload **.NET Desktop Development**; componente **Windows App SDK C# Templates** (em *Componentes individuais*) |
| **Python 3.10+ + Pillow** | só para branding | `pip install pillow`; `python tools/branding/generate_branding.py` |
| **Git** | sim | — |
| Modo de desenvolvedor do Windows | **não** | App unpackaged não exige |
| Certificado de assinatura | **não** | Só para o caminho MSIX secundário |

## Instalar o SDK

```powershell
winget install Microsoft.DotNet.SDK.10
dotnet --list-sdks        # deve listar 10.0.xxx
dotnet --version          # na pasta do repo: resolve pelo global.json
```

Se `dotnet --version` reclamar que o SDK do `global.json` não foi encontrado, o SDK instalado é mais antigo que `10.0.100` ou de outra major. `rollForward: latestFeature` só anda para frente dentro de `10.0`.

## Visual Studio 2022 (opcional)

1. Visual Studio Installer → **Modificar** → workload **Desenvolvimento para desktop com .NET**.
2. Aba *Componentes individuais* → marcar **Modelos C# do SDK do Aplicativo Windows** (Windows App SDK C# Templates). O SDK do Windows 10.0.26100 *pode* ser marcado, mas não é necessário (NuGet traz o targeting pack).
3. Abrir `SharpShot.slnx` (o VS 17.10+ abre `.slnx` nativamente; em versões anteriores, habilitar *Preview Features → Use Solution File Persistence Model*).
4. **Plataforma da solução: x64** (única suportada no 0.1.0). *Any CPU* não compila projetos WinUI — `Directory.Build.props` força x64 em builds de linha de comando, mas o VS respeita o que está selecionado na barra.
5. Projeto de inicialização: `SharpShot.App`. F5 inicia `SharpShot.exe` com o depurador (unpackaged, sem deploy de pacote).
6. *Ferramentas → Opções → Depuração → Geral*: desmarcar **Apenas Meu Código** quando for depurar interop/WinRT.

Hot Reload de XAML funciona em Debug; mudanças em `app.manifest` ou `.csproj` exigem rebuild.

## Primeiro build

```bash
git clone https://github.com/Victorkaue333/sharpshot.git
cd sharpshot
dotnet restore SharpShot.slnx
dotnet build SharpShot.slnx -c Debug -p:Platform=x64
```

Saída: `src\SharpShot.App\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\SharpShot.exe`. O primeiro restore baixa ~300 MB de pacotes (Windows App SDK, runtime, BuildTools) — tamanho exato **pendente de validação**.

> **Um build por vez.** Dois `dotnet build`/`dotnet test` simultâneos no mesmo checkout (por exemplo, dois agentes ou o VS + terminal) corrompem `obj/`. Se acontecer: fechar tudo, apagar `obj/` e `bin/` dos projetos afetados, restaurar e compilar de novo.

## Testes

```bash
dotnet test tests/SharpShot.Core.Tests -c Debug -p:Platform=x64
dotnet test tests/SharpShot.Capture.Tests -c Debug -p:Platform=x64
```

- `Core.Tests` cobre `Core` **e** `Storage`; roda sem Windows App SDK.
- `Capture.Tests` precisa de Windows; o teste de captura de integração se **pula** quando `IScreenCaptureService.IsSupported` é falso (VM sem DWM, sessão de serviço, runner headless).
- **Status:** os dois projetos têm só o `.csproj`; os testes estão sendo escritos. `dotnet test` hoje reporta zero testes.

## Executar

- Linha de comando: `src\SharpShot.App\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\SharpShot.exe` (opcional `--minimized`).
- Visual Studio: F5 com `SharpShot.App` como inicialização.
- `dotnet run --project src/SharpShot.App -c Debug -p:Platform=x64` também funciona, mas é mais lento que executar o `.exe` já compilado.

Durante o desenvolvimento, o app usa o **mesmo** `%LOCALAPPDATA%\SharpShot\settings.json` de uma instalação real. Para testar com configurações limpas, renomeie a pasta antes.

## Publicar localmente

```bash
dotnet publish src/SharpShot.App/SharpShot.App.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained -o artifacts/publish/win-x64
```

Detalhes (conteúdo da pasta, runtime VC++, checklist): [`../distribution/README.md`](../distribution/README.md).

## Regenerar o branding

Só quando o logo mudar (dono: `tools/branding/generate_branding.py`):

```bash
pip install pillow
python tools/branding/generate_branding.py
```

Reescreve `assets/branding/`, `assets/icons/`, `src/SharpShot.App/Assets/` e `src/SharpShot.App/Components/LogoGeometry.cs`. Nunca editar esses arquivos à mão (ADR-13).

## Problemas comuns

| Sintoma | Causa | Solução |
|---------|-------|---------|
| `error MSB4126: The specified solution configuration "Debug|Any CPU" is invalid` | Plataforma não informada no VS ou em comando que não passa por `Directory.Build.props` | `-p:Platform=x64`; no VS, selecionar x64 |
| `The current .NET SDK does not support targeting .NET 10.0` | SDK antigo | Instalar o SDK 10 |
| `NETSDK1100`/`Windows SDK` ausente | Restore incompleto | `dotnet restore SharpShot.slnx` de novo; verificar proxy do NuGet |
| `XamlCheckProcessRequirements` lança ao iniciar | Runtime do Windows App SDK não encontrado | Build unpackaged self-contained já copia o runtime; apagar `bin/obj` e recompilar |
| Build ok, mas `obj/` com erros estranhos (`.g.cs` duplicados) | Builds paralelos | Fechar tudo, apagar `obj/`, rebuild |
| `Capture.Tests` pula o teste de captura | WGC não suportado na sessão | Esperado em VM/RDP sem DWM; rodar em sessão local |
| Captura preta de um app | Conteúdo protegido / `WDA_EXCLUDEFROMCAPTURE` do outro app | Comportamento do Windows ([`../capture-engine/limitacoes.md`](../capture-engine/limitacoes.md)) |

---

Comandos: [`comandos.md`](comandos.md) · Debug: [`debug.md`](debug.md) · Ambiente: [`../ENVIRONMENT.md`](../ENVIRONMENT.md) · Estilo: [`../Code-Style.md`](../Code-Style.md)
