# Levanta backends en Docker y arranca Expo en el host (Expo Go / emulador nativo).
# Detecta la IPv4 LAN y la inyecta en Metro + PlayerMobile/.env para Expo Go en telefono.
# Para Player en navegador sin Node local: docker compose --profile full up -d --build

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

function Get-LanIPv4 {
    $virtualPattern = '(?i)(VPN|Virtual|VMware|Hyper-V|vEthernet|WSL|Docker|Loopback|Tailscale|Proton|Zerotier|Hamachi|Bluetooth|vboxnet)'

    $withGateway = @(Get-NetIPConfiguration -ErrorAction SilentlyContinue |
        Where-Object {
            $_.NetAdapter.Status -eq 'Up' -and
            $null -ne $_.IPv4DefaultGateway -and
            $null -ne $_.IPv4Address -and
            $_.InterfaceAlias -notmatch $virtualPattern -and
            $_.IPv4DefaultGateway.NextHop -and
            $_.IPv4DefaultGateway.NextHop -ne '0.0.0.0'
        } |
        Sort-Object {
            $preferWifi = if ($_.InterfaceAlias -match '(?i)(Wi-?Fi|WLAN|Ethernet|Local Area Connection)') { 0 } else { 1 }
            $metric = if ($null -ne $_.IPv4DefaultGateway.InterfaceMetric) {
                [int]$_.IPv4DefaultGateway.InterfaceMetric
            } else {
                9999
            }
            ($preferWifi * 10000) + $metric
        })

    if ($withGateway.Count -gt 0) {
        return @($withGateway[0].IPv4Address)[0].IPAddress
    }

    $fallback = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object {
            $_.IPAddress -notmatch '^(127\.|169\.254\.)' -and
            $_.PrefixOrigin -ne 'WellKnown' -and
            $_.InterfaceAlias -notmatch $virtualPattern -and
            (
                $_.IPAddress -match '^192\.168\.' -or
                $_.IPAddress -match '^10\.' -or
                $_.IPAddress -match '^172\.(1[6-9]|2[0-9]|3[0-1])\.'
            )
        } |
        Sort-Object InterfaceMetric |
        Select-Object -First 1

    if ($fallback) {
        return $fallback.IPAddress
    }

    return $null
}

function Update-PlayerMobileEnvHost {
    param(
        [Parameter(Mandatory = $true)][string]$EnvPath,
        [Parameter(Mandatory = $true)][string]$LanIp
    )

    if (-not (Test-Path $EnvPath)) {
        $example = "$EnvPath.example"
        if (Test-Path $example) {
            Copy-Item $example $EnvPath
            Write-Host "Creado PlayerMobile/.env desde .env.example" -ForegroundColor Cyan
        } else {
            Write-Warning "No existe PlayerMobile/.env ni .env.example; omitiendo ajuste de APIs."
            return
        }
    }

    $lines = Get-Content $EnvPath
    $changed = $false
    $updated = foreach ($line in $lines) {
        if ($line -match '^(EXPO_PUBLIC_[A-Z0-9_]+)=(https?):\/\/([^\/:\s]+)(:\d+)?(.*)$') {
            $name = $Matches[1]
            $scheme = $Matches[2]
            $oldHost = $Matches[3]
            $port = $Matches[4]
            $rest = $Matches[5]
            if ($oldHost -ne $LanIp) {
                $changed = $true
                "${name}=${scheme}://${LanIp}${port}${rest}"
            } else {
                $line
            }
        } else {
            $line
        }
    }

    if ($changed) {
        Set-Content -Path $EnvPath -Value $updated -Encoding utf8
        Write-Host "PlayerMobile/.env actualizado con host $LanIp" -ForegroundColor Green
    } else {
        Write-Host "PlayerMobile/.env ya usa host $LanIp" -ForegroundColor DarkGray
    }
}

Write-Host "Deteniendo PlayerMobile web en Docker (libera puerto 19000 para Metro)..." -ForegroundColor Cyan
# Docker escribe progreso en stderr; con ErrorAction Stop PowerShell lo trata como fallo fatal.
$prevEap = $ErrorActionPreference
$ErrorActionPreference = "Continue"
try {
    docker compose stop player-mobile-web 2>&1 | Out-Null
} catch {
    # El contenedor puede no existir o ya estar detenido.
}
$ErrorActionPreference = $prevEap

Write-Host "Levantando infraestructura y backends..." -ForegroundColor Cyan
$ErrorActionPreference = "Continue"
docker compose up -d db mq keycloak user-service mission-management-service session-management-service scoring-audit-service api-gateway
if ($LASTEXITCODE -ne 0) {
    $ErrorActionPreference = $prevEap
    throw "docker compose up fallo (exit $LASTEXITCODE)."
}
$ErrorActionPreference = $prevEap

Write-Host "Esperando a que Keycloak este healthy (puede tardar 2-3 min la primera vez)..." -ForegroundColor Yellow
$maxAttempts = 60
$attempt = 0
$ErrorActionPreference = "Continue"
while ($attempt -lt $maxAttempts) {
    $raw = docker compose ps keycloak --format json 2>$null
    if ($raw) {
        try {
            $status = $raw | ConvertFrom-Json
            if ($status -and $status.Health -eq "healthy") {
                Write-Host "Keycloak listo." -ForegroundColor Green
                break
            }
        } catch {
            # JSON incompleto o salida mixta; reintentar.
        }
    }
    Start-Sleep -Seconds 5
    $attempt++
}
$ErrorActionPreference = $prevEap
if ($attempt -ge $maxAttempts) {
    Write-Warning "Keycloak no reporto healthy a tiempo. Revisa: docker compose ps"
}

Set-Location (Join-Path $Root "PlayerMobile")

if (-not (Test-Path "node_modules")) {
    Write-Host "Instalando dependencias (npm ci)..." -ForegroundColor Cyan
    npm ci
}

$lanIp = Get-LanIPv4
if (-not $lanIp) {
    Write-Warning "No se pudo detectar IPv4 LAN. Expo usara 127.0.0.1 y el telefono no conectara."
    Write-Warning "Define manualmente: `$env:REACT_NATIVE_PACKAGER_HOSTNAME='TU.IP.LAN'"
} else {
    $env:REACT_NATIVE_PACKAGER_HOSTNAME = $lanIp
    Update-PlayerMobileEnvHost -EnvPath (Join-Path (Get-Location) ".env") -LanIp $lanIp
    Write-Host ""
    Write-Host "IP LAN detectada: $lanIp" -ForegroundColor Green
    Write-Host "Metro/QR deberia mostrar: exp://${lanIp}:19000" -ForegroundColor Green
    Write-Host "PC y telefono deben estar en la misma Wi-Fi." -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "Iniciando Expo en el host (puerto 19000)..." -ForegroundColor Cyan
Write-Host ""

npm start
