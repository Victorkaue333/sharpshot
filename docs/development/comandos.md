# Comandos — SharpShot

> Referência de todos os comandos do projeto, com flags, quando usar e o que esperar. Copie daqui; não invente variantes. Os mesmos comandos estão no [`README.md`](../../README.md), no [`CLAUDE.md`](../../CLAUDE.md) e no CI.
> Última atualização: 04/nov/2026 · Mantenedor: Victor Alves

## Resumo

| Tarefa | Comando |
|--------|---------|
| Restaurar | `dotnet restore SharpShot.slnx` |
| Compilar | `dotnet build SharpShot.slnx -c Debug -p:Platform=x64` |
| Testar Core + Storage | `dotnet test tests/SharpShot.Core.Tests -c Debug -p:Platform=x64` |
| Testar Capture | `dotnet test tests/SharpShot.Capture.Tests -c Debug -p:Platform=x64` |
| Executar | `src\SharpShot.App\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\SharpShot.exe` |
| Publicar x64 | `dotnet publish src/SharpShot.App/SharpShot.App.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained -o artifacts/publish/win-x64` |
| Publicar arm64 | `dotnet publish src/SharpShot.App/SharpShot.App.csproj -c Release -p:Platform=arm64 -r win-arm64 --self-contained -o artifacts/publish/win-arm64` |
| Branding | `python tools/branding/generate_branding.py` |
| Formatar | `dotnet format SharpShot.slnx` |

## Restore

```bash
dotnet restore SharpShot.slnx
```

- Lê `Directory.Packages.props` (versões centrais) e baixa Windows App SDK 2.6.5, Microsoft.Extensions 10.0.14, xunit, o targeting pack do Windows SDK 10.0.26100 e os BuildTools.
- Cria `obj/project.assets.json` em cada projeto. Sem ele, `build --no-restore` falha.
- Rodar de novo sempre que `Directory.Packages.props` ou um `.csproj` mudar.

## Build

```bash
dotnet build SharpShot.slnx -c Debug -p:Platform=x64
dotnet build SharpShot.slnx -c Release -p:Platform=x64
dotnet build SharpShot.slnx -c Debug -p:Platform=arm64
```

- `-p:Platform` é **obrigatório** para o `.slnx` (WinUI não compila AnyCPU). Para um projeto isolado (`dotnet build src/SharpShot.Core`), `Directory.Build.props` assume x64.
- `--no-restore` depois de um restore recente; `--no-incremental` para forçar rebuild.
- Warnings de analisador aparecem aqui (`AnalysisLevel=latest-recommended`); `TreatWarningsAsErrors` está desligado, mas warning novo não deve entrar em PR.
- Saídas: `src/<Projeto>/bin/x64/<Config>/<TFM>/[win-x64/]`. O App tem o RID na pasta por ser self-contained.

## Test

```bash
dotnet test tests/SharpShot.Core.Tests -c Debug -p:Platform=x64
dotnet test tests/SharpShot.Capture.Tests -c Debug -p:Platform=x64

# variantes úteis
dotnet test tests/SharpShot.Core.Tests -c Debug -p:Platform=x64 --no-build
dotnet test tests/SharpShot.Core.Tests -c Debug -p:Platform=x64 --filter "FullyQualifiedName~RegionMath"
dotnet test tests/SharpShot.Core.Tests -c Debug -p:Platform=x64 --logger "trx;LogFileName=core.trx"
dotnet test tests/SharpShot.Core.Tests -c Debug -p:Platform=x64 --collect:"XPlat Code Coverage"
```

- Um projeto por comando (o CI faz igual); `dotnet test SharpShot.slnx -p:Platform=x64` também funciona.
- `Capture.Tests` em runner sem sessão de desktop: o teste de captura de integração aparece como **Skipped** com o motivo. Isso é esperado, não falha.
- **Hoje:** zero testes nos dois projetos (só `.csproj`). O comando passa trivialmente.

## Run

```bash
src\SharpShot.App\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\SharpShot.exe
src\SharpShot.App\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\SharpShot.exe --minimized
dotnet run --project src/SharpShot.App -c Debug -p:Platform=x64
```

- Segunda execução com a primeira aberta: ativa a existente e sai (instância única — em implementação).
- Logs em `%LOCALAPPDATA%\SharpShot\logs` (em implementação) e no Output do VS.

## Publish

```bash
dotnet publish src/SharpShot.App/SharpShot.App.csproj -c Release -p:Platform=x64   -r win-x64   --self-contained -o artifacts/publish/win-x64
dotnet publish src/SharpShot.App/SharpShot.App.csproj -c Release -p:Platform=arm64 -r win-arm64 --self-contained -o artifacts/publish/win-arm64
```

- `--self-contained` já é o padrão do `.csproj` (`SelfContained=true`), repetido para deixar explícito.
- `-p:Platform` e `-r` precisam combinar (x64 ↔ `win-x64`, arm64 ↔ `win-arm64`).
- `-o` pode ser qualquer pasta; `artifacts/` está no `.gitignore`.
- Não usar `-p:PublishTrimmed=true` nem `-p:PublishReadyToRun=true` sem ADR — o `.csproj` os desliga de propósito (ADR-01).
- Resultado e checklist: [`../distribution/README.md`](../distribution/README.md).

## Release (pacote)

```powershell
pwsh tools/release/make-release.ps1                # publish + VC++ runtime + zip + sha256 em artifacts/release/
pwsh tools/release/make-release.ps1 -SkipPublish   # só reempacota um publish já existente
```

Saída esperada: `==> Done: ...\artifacts\release\SharpShot-0.1.0-win-x64.zip (66.5 MB)` e a linha `SHA-256`. A tag `v*` no GitHub dispara `.github/workflows/release.yml`, que anexa o zip à Release.

## Branding

```bash
pip install pillow
python tools/branding/generate_branding.py
```

Roda da raiz do repo (o script resolve caminhos a partir do próprio arquivo). Saída: `assets/branding/`, `assets/icons/`, `src/SharpShot.App/Assets/`, `src/SharpShot.App/Components/LogoGeometry.cs`.

## Formatação e análise

```bash
dotnet format SharpShot.slnx                       # aplica .editorconfig (whitespace + style)
dotnet format SharpShot.slnx --verify-no-changes   # falha se houver algo a formatar (sugerido para o CI)
dotnet build SharpShot.slnx -c Debug -p:Platform=x64 -warnaserror   # pontual: trata warnings como erro
```

## Limpeza

```bash
dotnet clean SharpShot.slnx -c Debug -p:Platform=x64
# limpeza total (Git Bash / PowerShell):
git clean -xdn   # só lista — NUNCA rodar git clean -xdf sem conferir; apaga artifacts/ e bin/obj
```

> Assistentes de IA: `git clean`, `git reset`, `git checkout` e afins são proibidos ([`../../CLAUDE.md`](../../CLAUDE.md)). Para limpar build, apague `bin/` e `obj/` manualmente.

## Git (só leitura para agentes)

```bash
git status --short
git diff
git log --oneline
```

## CI (`.github/workflows/build.yml`, escrito pelo lead)

A sequência do CI é exatamente: restore → build Debug x64 → test Core → test Capture → publish win-x64 → upload do artifact. Qualquer comando novo entra **primeiro aqui**, depois no workflow.

---

Setup: [`README.md`](README.md) · Debug: [`debug.md`](debug.md) · Publicação: [`../DEPLOY.md`](../DEPLOY.md)
