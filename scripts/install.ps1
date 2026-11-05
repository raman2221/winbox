# Installs the latest winbox release for the current user. No admin rights needed.
#
#   irm https://github.com/raman2221/winbox/releases/latest/download/install.ps1 | iex
#
# Everything lives inside functions so that running this through `iex` doesn't leave
# variables or preference changes behind in the caller's session.

function Get-WinboxDownload([string] $url, [string] $destination) {
    Invoke-WebRequest -Uri $url -OutFile $destination -UseBasicParsing
}

function Assert-WinboxHash([string] $zipPath, [string] $hashPath) {
    # The .sha256 file uses the `sha256sum` layout: "<hex hash>  <file name>".
    $expected = ((Get-Content -Path $hashPath -Raw).Trim() -split '\s+')[0]
    $actual = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash
    if ($actual -ne $expected) {
        throw "Checksum mismatch for winbox-win-x64.zip (expected $expected, got $actual). Nothing was installed."
    }
}

function Clear-WinboxInstallDir([string] $installDir) {
    if (-not (Test-Path $installDir)) { return }
    try {
        Get-ChildItem -Path $installDir -Force | Remove-Item -Recurse -Force
    } catch {
        throw "Couldn't replace $installDir. Close any running winbox.exe (including MCP servers) and retry."
    }
}

function Expand-WinboxZip([string] $zipPath, [string] $installDir) {
    Clear-WinboxInstallDir $installDir
    New-Item -ItemType Directory -Force -Path $installDir | Out-Null
    Expand-Archive -Path $zipPath -DestinationPath $installDir -Force
}

function Test-PathListContains([string] $pathList, [string] $folder) {
    $entries = $pathList -split ';' | ForEach-Object { $_.Trim().TrimEnd('\') }
    return $entries -contains $folder.TrimEnd('\')
}

function Add-WinboxToUserPath([string] $installDir) {
    $userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    if ($null -eq $userPath) { $userPath = '' }
    if (Test-PathListContains $userPath $installDir) { return $false }

    $newPath = (@($userPath.TrimEnd(';'), $installDir) | Where-Object { $_ }) -join ';'
    [Environment]::SetEnvironmentVariable('Path', $newPath, 'User')
    $env:Path = "$env:Path;$installDir"
    return $true
}

function Write-WinboxNextSteps([string] $installDir, [bool] $pathChanged) {
    Write-Host ''
    Write-Host "winbox installed to $installDir" -ForegroundColor Green
    if ($pathChanged) {
        Write-Host 'Added it to your user PATH. Open a new terminal so other windows pick it up.'
    }
    Write-Host ''
    Write-Host 'Next steps:'
    Write-Host '  winbox setup        check that Windows Sandbox is ready'
    Write-Host '  winbox start        boot the sandbox'
    Write-Host '  winbox mcp-config   print the command that registers winbox with Claude Code'
}

function Install-Winbox {
    $ErrorActionPreference = 'Stop'
    # Windows PowerShell 5.1 renders a slow progress bar per chunk and may default to older TLS versions.
    $ProgressPreference = 'SilentlyContinue'
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor
        [Net.SecurityProtocolType]::Tls12

    $baseUrl = 'https://github.com/raman2221/winbox/releases/latest/download'
    $installDir = Join-Path $env:LOCALAPPDATA 'Programs\winbox'
    $tempDir = Join-Path ([IO.Path]::GetTempPath()) ('winbox-install-' + [Guid]::NewGuid().ToString('N'))
    $zipPath = Join-Path $tempDir 'winbox-win-x64.zip'
    $hashPath = "$zipPath.sha256"

    New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
    try {
        Write-Host 'Downloading winbox...'
        Get-WinboxDownload "$baseUrl/winbox-win-x64.zip" $zipPath
        Get-WinboxDownload "$baseUrl/winbox-win-x64.zip.sha256" $hashPath
        Assert-WinboxHash $zipPath $hashPath
        Expand-WinboxZip $zipPath $installDir
        $pathChanged = Add-WinboxToUserPath $installDir
        Write-WinboxNextSteps $installDir $pathChanged
    } finally {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Install-Winbox
