# Levanta solo lo necesario para probar PlayerMobile (Docker)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Set-Location $root
Write-Host "Starting UMBRAL mobile backend stack..." -ForegroundColor Cyan
docker compose up -d db mq keycloak mission-management-service session-management-service

Write-Host ""
Write-Host "Waiting for services (up to ~90s on first run)..." -ForegroundColor Yellow
$deadline = (Get-Date).AddSeconds(90)
$ready = $false
while ((Get-Date) -lt $deadline) {
    $mm = docker inspect -f "{{.State.Health.Status}}" umbral-mission-management-service 2>$null
    $sm = docker inspect -f "{{.State.Status}}" umbral-session-management-service 2>$null
    if ($sm -eq "running") {
        try {
            Invoke-WebRequest -Uri "http://localhost:5260/openapi/v1.json" -UseBasicParsing -TimeoutSec 3 | Out-Null
            Invoke-WebRequest -Uri "http://localhost:5278/openapi/v1.json" -UseBasicParsing -TimeoutSec 3 | Out-Null
            $ready = $true
            break
        } catch {
            Start-Sleep -Seconds 3
        }
    }
    Start-Sleep -Seconds 3
}

Write-Host ""
if ($ready) {
    Write-Host "Backend ready." -ForegroundColor Green
} else {
    Write-Host "Backend may still be starting. Check: docker compose ps" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "URLs:" -ForegroundColor Cyan
Write-Host "  Keycloak:            http://localhost:8081"
Write-Host "  MissionManagement:   http://localhost:5260"
Write-Host "  SessionManagement:   http://localhost:5278"
Write-Host ""
Write-Host "Mobile app (.env already in PlayerMobile/):" -ForegroundColor Cyan
Write-Host "  cd PlayerMobile"
Write-Host "  npm install"
Write-Host "  npm start"
Write-Host "  Then press w for web browser."
Write-Host ""
Write-Host "Full guide: PlayerMobile/COMO_PROBAR.md" -ForegroundColor Gray
