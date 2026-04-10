$ErrorActionPreference = 'Stop'

function Get-CommandPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [string[]]$Fallbacks = @()
    )

    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    foreach ($fallback in $Fallbacks) {
        if (Test-Path $fallback) {
            return $fallback
        }
    }

    throw "No se encontro el ejecutable requerido: $Name"
}

function Get-ListeningProcess {
    param(
        [Parameter(Mandatory = $true)]
        [int]$Port
    )

    try {
        return Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop |
            Select-Object -First 1
    }
    catch {
        return $null
    }
}

function Start-DevWindow {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,
        [Parameter(Mandatory = $true)]
        [string]$ScriptText
    )

    $powerShellExe = Get-CommandPath -Name 'powershell.exe' -Fallbacks @(
        "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
    )

    $encodedCommand = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($ScriptText))

    Start-Process -FilePath $powerShellExe `
        -WorkingDirectory $WorkingDirectory `
        -ArgumentList '-NoExit', '-ExecutionPolicy', 'Bypass', '-EncodedCommand', $encodedCommand | Out-Null
}

$repoRoot = $PSScriptRoot
$backendDir = Join-Path $repoRoot 'ProDoctivityDS'
$frontendDir = Join-Path $repoRoot 'prodoctivityDS-frontend'
$backendProject = Join-Path $backendDir 'ProDoctivityDS.csproj'
$frontendPackage = Join-Path $frontendDir 'package.json'

if (-not (Test-Path $backendProject)) {
    throw "No se encontro el proyecto backend en $backendProject"
}

if (-not (Test-Path $frontendPackage)) {
    throw "No se encontro el frontend en $frontendPackage"
}

$dotnetExe = Get-CommandPath -Name 'dotnet' -Fallbacks @(
    'C:\Program Files\dotnet\dotnet.exe'
)

$npmCmd = Get-CommandPath -Name 'npm.cmd' -Fallbacks @(
    'C:\Program Files\nodejs\npm.cmd'
)

$backendListener = Get-ListeningProcess -Port 7278
$frontendListener = Get-ListeningProcess -Port 4200

if ($backendListener) {
    Write-Host "Backend ya esta escuchando en https://localhost:7278 (PID $($backendListener.OwningProcess))." -ForegroundColor Yellow
}
else {
    $backendPath = $backendDir.Replace("'", "''")
    $backendDotnet = $dotnetExe.Replace("'", "''")

    $backendScript = @"
Set-Location '$backendPath'
`$Host.UI.RawUI.WindowTitle = 'ProDoctivityDS Backend'
Write-Host '[Backend] Levantando API en https://localhost:7278' -ForegroundColor Cyan
& '$backendDotnet' run --launch-profile https
"@

    Start-DevWindow -WorkingDirectory $backendDir -ScriptText $backendScript
    Write-Host 'Backend lanzado en una nueva ventana.' -ForegroundColor Green
}

if ($frontendListener) {
    Write-Host "Frontend ya esta escuchando en http://localhost:4200 (PID $($frontendListener.OwningProcess))." -ForegroundColor Yellow
}
else {
    $frontendPath = $frontendDir.Replace("'", "''")
    $frontendNpm = $npmCmd.Replace("'", "''")

    $frontendScript = @"
Set-Location '$frontendPath'
`$Host.UI.RawUI.WindowTitle = 'ProDoctivityDS Frontend'
`$env:Path = 'C:\Program Files\nodejs;' + `$env:Path
if (-not (Test-Path '.\node_modules')) {
    Write-Host '[Frontend] node_modules no existe. Ejecutando npm install...' -ForegroundColor Yellow
    & '$frontendNpm' install
}
Write-Host '[Frontend] Levantando app en http://localhost:4200' -ForegroundColor Cyan
& '$frontendNpm' start
"@

    Start-DevWindow -WorkingDirectory $frontendDir -ScriptText $frontendScript
    Write-Host 'Frontend lanzado en una nueva ventana.' -ForegroundColor Green
}

Write-Host ''
Write-Host 'URLs de desarrollo:' -ForegroundColor Cyan
Write-Host '  Frontend: http://localhost:4200/'
Write-Host '  Backend:  https://localhost:7278/'
Write-Host ''
Write-Host 'Si PowerShell bloquea el script, ejecuta: .\start-dev.cmd' -ForegroundColor DarkGray
