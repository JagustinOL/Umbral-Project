#Requires -Version 5.1
<#
.SYNOPSIS
  Comprueba cobertura de líneas >= umbral (default 90%) en total y por ensamblado.
#>
param(
    [double]$MinimumPercent = 90,
    [switch]$RunTests
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ReportDir = Join-Path $RepoRoot "coverage-report"
$SummaryPath = Join-Path $ReportDir "Summary.txt"

$RequiredAssemblies = @(
    "MissionManagement.Domain",
    "MissionManagement.Application",
    "SessionManagement.Domain",
    "SessionManagement.Application"
)

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

$failed = $false
if ($lineCoverage -lt $MinimumPercent) {
    Write-Host "FALLO: cobertura total por debajo del umbral." -ForegroundColor Red
    $failed = $true
}

Write-Host ""
Write-Host "Cobertura por ensamblado:" -ForegroundColor Cyan
foreach ($assembly in $RequiredAssemblies) {
    # Summary lines look like: "MissionManagement.Domain    93.6%"
    $asmMatch = [regex]::Match(
        $content,
        "(?m)^$([regex]::Escape($assembly))\s+([\d.,]+)\s*%\s*$")
    if (-not $asmMatch.Success) {
        Write-Host "  $assembly : NO ENCONTRADO" -ForegroundColor Red
        $failed = $true
        continue
    }

    $pct = [double]($asmMatch.Groups[1].Value.Replace(",", "."))
    $ok = $pct -ge $MinimumPercent
    $color = if ($ok) { "Green" } else { "Red" }
    $label = if ($ok) { "OK" } else { "FALLO" }
    Write-Host ("  {0,-32} {1,5:N1}%  [{2}]" -f $assembly, $pct, $label) -ForegroundColor $color
    if (-not $ok) { $failed = $true }
}

if ($failed) {
    Write-Host ""
    Write-Host "FALLO: uno o más umbrales de cobertura no se cumplen." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "OK: se cumple el umbral de cobertura (total y por ensamblado)." -ForegroundColor Green
exit 0
