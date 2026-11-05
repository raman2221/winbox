# Builds the same self-contained release zip as .github/workflows/release.yml, into dist\.
#
#   .\scripts\package.ps1

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot 'out'
$distDir = Join-Path $repoRoot 'dist'
$zipName = 'winbox-win-x64.zip'
$zipPath = Join-Path $distDir $zipName

function Invoke-Publish {
    if (Test-Path $publishDir) { Remove-Item -Path $publishDir -Recurse -Force }
    $project = Join-Path $repoRoot 'src\Winbox.Cli'
    dotnet publish $project -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
}

function New-ReleaseZip {
    New-Item -ItemType Directory -Force -Path $distDir | Out-Null
    if (Test-Path $zipPath) { Remove-Item -Path $zipPath -Force }
    Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath
}

function Write-ReleaseHash {
    $hash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    # Written as raw ASCII bytes so both PowerShell editions produce an identical file without a BOM.
    [IO.File]::WriteAllText("$zipPath.sha256", "$hash  $zipName", [Text.Encoding]::ASCII)
    return $hash
}

Invoke-Publish
New-ReleaseZip
$hash = Write-ReleaseHash
Write-Host "Created $zipPath"
Write-Host "SHA256 $hash"
