# Installs the latest winbox release for the current user. No admin rights needed.
#
#   irm https://github.com/raman2221/winbox/releases/latest/download/install.ps1 | iex
#
# Everything lives inside functions so that running this through `iex` doesn't leave
# variables or preference changes behind in the caller's session (only these function definitions).

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

function Assert-WinboxNotRunning([string] $installDir) {
    # Checked before deleting anything, so a running winbox.exe can't leave a half-removed install behind.
    $exe = Join-Path $installDir 'winbox.exe'
    if (-not (Test-Path $exe)) { return }
    try {
        [IO.File]::Open($exe, 'Open', 'ReadWrite', 'None').Dispose()
    } catch {
        throw "winbox.exe is in use. Close any running winbox.exe (including MCP servers) and retry."
    }
}

function Clear-WinboxInstallDir([string] $installDir) {
    if (-not (Test-Path $installDir)) { return }
    Assert-WinboxNotRunning $installDir
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
    $entries = $pathList -split ';' | ForEach-Object {
        [Environment]::ExpandEnvironmentVariables($_).Trim().TrimEnd('\')
    }
    return $entries -contains $folder.TrimEnd('\')
}

function Get-UserPathUnexpanded {
    # Read without expanding, so entries such as %USERPROFILE%\... are written back unchanged.
    $key = Get-Item -Path 'HKCU:\Environment'
    return [string] $key.GetValue('Path', '', 'DoNotExpandEnvironmentNames')
}

function Set-UserPathExpandable([string] $userPath) {
    # [Environment]::SetEnvironmentVariable would store PATH as REG_SZ and flatten every %VARIABLE% entry.
    Set-ItemProperty -Path 'HKCU:\Environment' -Name 'Path' -Value $userPath -Type ExpandString
    # A direct registry write doesn't notify Explorer; setting and clearing a throwaway variable does.
    [Environment]::SetEnvironmentVariable('WINBOX_INSTALL_REFRESH', '1', 'User')
    [Environment]::SetEnvironmentVariable('WINBOX_INSTALL_REFRESH', $null, 'User')
}

function Add-WinboxToSessionPath([string] $installDir) {
    if (Test-PathListContains $env:Path $installDir) { return }
    $env:Path = "$($env:Path.TrimEnd(';'));$installDir"
}

function Add-WinboxToUserPath([string] $installDir) {
    Add-WinboxToSessionPath $installDir
    $userPath = Get-UserPathUnexpanded
    if (Test-PathListContains $userPath $installDir) { return $false }

    Set-UserPathExpandable ((@($userPath.TrimEnd(';'), $installDir) | Where-Object { $_ }) -join ';')
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
