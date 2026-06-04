#Requires -Version 5.1
<#
.SYNOPSIS
  Comprueba que la cobertura de líneas (Domain + Application) sea >= umbral (default 90%).
#>
param(
    [double]$MinimumPercent = 90,
    [switch]$RunTests
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ReportDir = Join-Path $RepoRoot "coverage-report"
$SummaryPath = Join-Path $ReportDir "Summary.txt"

if ($RunTests) {
    & (Join-Path $PSScriptRoot "run-coverage.ps1")
}

if (-not (Test-Path $SummaryPath)) {
    Write-Error @"
No existe $SummaryPath. Ejecuta primero:
  .\scripts\run-coverage.ps1
"@
}

$content = Get-Content $SummaryPath -Raw
$match = [regex]::Match($content, "Line coverage:\s*([\d.,]+)\s*%")
if (-not $match.Success) {
    Write-Error "No se pudo leer 'Line coverage' en $SummaryPath"
}

$lineCoverage = [double]($match.Groups[1].Value.Replace(",", "."))
Write-Host "Cobertura de líneas (Domain + Application): $([math]::Round($lineCoverage, 2))%" -ForegroundColor Cyan
Write-Host "Umbral requerido: $MinimumPercent%" -ForegroundColor Cyan

if ($lineCoverage -lt $MinimumPercent) {
    Write-Host "FALLO: cobertura por debajo del umbral." -ForegroundColor Red
    exit 1
}

Write-Host "OK: se cumple el umbral de cobertura." -ForegroundColor Green
exit 0
