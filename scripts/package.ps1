# Builds the self-contained release zip and its .sha256 into dist\.
# .github/workflows/release.yml runs this same script, so a local build matches the published one.
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
    dotnet publish $project -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
}

function New-ReleaseZip {
    New-Item -ItemType Directory -Force -Path $distDir | Out-Null
    if (Test-Path $zipPath) { Remove-Item -Path $zipPath -Force }
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::Open($zipPath, 'Create')
    try {
        Get-ChildItem -Path $publishDir -Recurse -File | ForEach-Object { Add-ZipEntry $zip $_.FullName }
    } finally {
        $zip.Dispose()
    }
}

function Add-ZipEntry($zip, [string] $file) {
    # Entry names are built by hand because Windows PowerShell's Compress-Archive and ZipFile.CreateFromDirectory
    # store "guest\..." with backslashes, which the zip format forbids and non-Windows unzip tools mishandle.
    $entryName = $file.Substring($publishDir.Length).TrimStart('\').Replace('\', '/')
    [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file, $entryName, 'Optimal') | Out-Null
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
