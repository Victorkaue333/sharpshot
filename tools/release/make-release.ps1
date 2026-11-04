<#
.SYNOPSIS
    Builds the distributable SharpShot package (unpackaged, self-contained, x64).

.DESCRIPTION
    1. dotnet publish (Release, win-x64, self-contained .NET + Windows App SDK runtime)
    2. Copies the Visual C++ runtime DLLs next to SharpShot.exe (Windows App SDK native
       libraries depend on vcruntime140/msvcp140). They are taken from the VC redist
       folder of the installed Visual Studio / Build Tools; when not found a warning is
       printed and the package still works on machines that have the VC++ Redistributable.
    3. Zips the folder to artifacts/release/SharpShot-<version>-win-x64.zip and writes a SHA-256 file.

.EXAMPLE
    pwsh tools/release/make-release.ps1
    pwsh tools/release/make-release.ps1 -Version 0.1.0 -Configuration Release
#>
[CmdletBinding()]
param(
    [string] $Version = "",
    [string] $Configuration = "Release",
    [string] $Runtime = "win-x64",
    [switch] $SkipPublish,
    [switch] $SkipInstaller
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $root

if (-not $Version) {
    $props = [xml](Get-Content (Join-Path $root "Directory.Build.props"))
    $Version = ($props.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)
}
if (-not $Version) { throw "Could not determine the version (Directory.Build.props)." }

$platform = if ($Runtime -eq "win-arm64") { "arm64" } else { "x64" }
$publishDir = Join-Path $root "artifacts\publish\$Runtime"
$releaseDir = Join-Path $root "artifacts\release"
$zipPath = Join-Path $releaseDir "SharpShot-$Version-$Runtime.zip"

if (-not $SkipPublish) {
    Write-Host "==> dotnet publish ($Configuration, $Runtime)"
    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
    dotnet publish "src/SharpShot.App/SharpShot.App.csproj" -c $Configuration -p:Platform=$platform -r $Runtime --self-contained -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }
}

# --- Visual C++ runtime --------------------------------------------------------------------
$crtFiles = @("vcruntime140.dll", "vcruntime140_1.dll", "msvcp140.dll", "msvcp140_1.dll", "msvcp140_2.dll", "concrt140.dll")
$redistCandidates = @()
if ($env:VCToolsRedistDir) { $redistCandidates += Join-Path $env:VCToolsRedistDir "$platform\Microsoft.VC143.CRT" }
foreach ($vsRoot in @("${env:ProgramFiles}\Microsoft Visual Studio\2022", "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022")) {
    if (Test-Path $vsRoot) {
        $redistCandidates += Get-ChildItem -Path $vsRoot -Directory -Recurse -Depth 8 -Filter "Microsoft.VC143.CRT" -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match "\\Redist\\MSVC\\[^\\]+\\$platform\\" } |
            Sort-Object FullName -Descending | Select-Object -ExpandProperty FullName
    }
}
$redist = $redistCandidates | Where-Object { $_ -and (Test-Path (Join-Path $_ "vcruntime140.dll")) } | Select-Object -First 1
if ($redist) {
    Write-Host "==> Copying VC++ runtime from $redist"
    foreach ($f in $crtFiles) {
        $src = Join-Path $redist $f
        if (Test-Path $src) { Copy-Item $src -Destination $publishDir -Force }
    }
}
else {
    Write-Warning "VC++ redist folder not found; the package will rely on the Microsoft Visual C++ Redistributable being installed."
}

# --- Trim debug symbols from the package ---------------------------------------------------
Get-ChildItem $publishDir -Filter "*.pdb" | Remove-Item -Force

# --- Zip ------------------------------------------------------------------------------------
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Write-Host "==> Creating $zipPath"
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -CompressionLevel Optimal
$hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path $zipPath -Leaf)" | Set-Content -Encoding ascii "$zipPath.sha256"

$size = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host "==> Done: $zipPath ($size MB)"
Write-Host "    SHA-256: $hash"

# --- Installer (Inno Setup) — the primary download ------------------------------------------
if (-not $SkipInstaller) {
    $iscc = $null
    foreach ($candidate in @(
            "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
            "$env:ProgramFiles\Inno Setup 6\ISCC.exe")) {
        if ($candidate -and (Test-Path $candidate)) { $iscc = $candidate; break }
    }
    if (-not $iscc) {
        $cmd = Get-Command iscc -ErrorAction SilentlyContinue
        if ($cmd) { $iscc = $cmd.Source }
    }
    if ($iscc) {
        Write-Host "==> Compiling installer with $iscc"
        & $iscc "/Qp" "/DMyAppVersion=$Version" "/DSourceDir=$publishDir" "/DOutputDir=$releaseDir" (Join-Path $root "installer\SharpShot.iss")
        if ($LASTEXITCODE -ne 0) { throw "ISCC failed with exit code $LASTEXITCODE." }
        $setupPath = Join-Path $releaseDir "SharpShot-Setup-$Version.exe"
        $setupHash = (Get-FileHash $setupPath -Algorithm SHA256).Hash.ToLowerInvariant()
        "$setupHash  $(Split-Path $setupPath -Leaf)" | Set-Content -Encoding ascii "$setupPath.sha256"
        $setupSize = [math]::Round((Get-Item $setupPath).Length / 1MB, 1)
        Write-Host "==> Installer: $setupPath ($setupSize MB)"
        Write-Host "    SHA-256: $setupHash"
    }
    else {
        Write-Warning "Inno Setup (ISCC.exe) not found; installer skipped. Install it with: winget install JRSoftware.InnoSetup"
    }
}
