# Levanta backends en Docker y arranca Expo en el host (Expo Go / emulador nativo).
# Para Player en navegador sin Node local: docker compose --profile full up -d --build

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

Write-Host "Levantando infraestructura y backends..." -ForegroundColor Cyan
docker compose up -d db mq keycloak mission-management-service session-management-service scoring-audit-service

Write-Host "Esperando a que Keycloak este healthy (puede tardar 2-3 min la primera vez)..." -ForegroundColor Yellow
$maxAttempts = 60
$attempt = 0
while ($attempt -lt $maxAttempts) {
    $status = docker compose ps keycloak --format json 2>$null | ConvertFrom-Json
    if ($status -and $status.Health -eq "healthy") {
        Write-Host "Keycloak listo." -ForegroundColor Green
        break
    }
    Start-Sleep -Seconds 5
    $attempt++
}
if ($attempt -ge $maxAttempts) {
    Write-Warning "Keycloak no reporto healthy a tiempo. Revisa: docker compose ps"
}

Set-Location (Join-Path $Root "PlayerMobile")

if (-not (Test-Path "node_modules")) {
    Write-Host "Instalando dependencias (npm ci)..." -ForegroundColor Cyan
    npm ci
}

Write-Host ""
Write-Host "Iniciando Expo en el host (puerto 19000)..." -ForegroundColor Cyan
Write-Host "Teléfono físico: sustituye localhost por la IPv4 de tu PC en PlayerMobile/.env" -ForegroundColor Yellow
Write-Host "Emulador Android: usa 10.0.2.2 en lugar de localhost en .env" -ForegroundColor Yellow
Write-Host ""

npm start
