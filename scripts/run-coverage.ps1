#Requires -Version 5.1
<#
.SYNOPSIS
  Ejecuta pruebas unitarias (Domain/Application.Tests), genera cobertura y informe HTML.
#>
param(
    [switch]$NoClean
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
    Write-Error @"
reportgenerator no está instalado. Ejecuta:
  dotnet tool install --global dotnet-reportgenerator-globaltool
"@
}

$UnitTestProjects = @(
    "MissionManagement/MissionManagement.Domain.Tests/MissionManagement.Domain.Tests.csproj",
    "MissionManagement/MissionManagement.Application.Tests/MissionManagement.Application.Tests.csproj",
    "SessionManagement/SessionManagement.Domain.Tests/SessionManagement.Domain.Tests.csproj",
    "SessionManagement/SessionManagement.Application.Tests/SessionManagement.Application.Tests.csproj"
) | ForEach-Object { Join-Path $RepoRoot $_ }

$TestResultsDir = Join-Path $RepoRoot "TestResults"
$ReportDir = Join-Path $RepoRoot "coverage-report"

if (-not $NoClean) {
    if (Test-Path $TestResultsDir) { Remove-Item $TestResultsDir -Recurse -Force }
    if (Test-Path $ReportDir) { Remove-Item $ReportDir -Recurse -Force }
}

New-Item -ItemType Directory -Path $TestResultsDir -Force | Out-Null

Write-Host "Ejecutando pruebas unitarias (Domain + Application)..." -ForegroundColor Cyan
Push-Location $RepoRoot
try {
    foreach ($project in $UnitTestProjects) {
        Write-Host "  -> $(Split-Path $project -Leaf)" -ForegroundColor DarkGray
        & dotnet test $project `
            --collect:"XPlat Code Coverage" `
            --results-directory $TestResultsDir `
            -- `
            "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura"
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test falló en $project (código $LASTEXITCODE)"
        }
    }
}
finally {
    Pop-Location
}

$coverageFiles = Get-ChildItem -Path $TestResultsDir -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue
if (-not $coverageFiles) {
    throw "No se encontró coverage.cobertura.xml en $TestResultsDir"
}

Write-Host "Generando informe (Domain + Application)..." -ForegroundColor Cyan
$reportsPattern = (Join-Path $TestResultsDir "**") + "\coverage.cobertura.xml"

& reportgenerator `
    -reports:$reportsPattern `
    -targetdir:$ReportDir `
    -reporttypes:"Html;TextSummary" `
    -assemblyfilters:"+MissionManagement.Domain;+MissionManagement.Application;+SessionManagement.Domain;+SessionManagement.Application"

if ($LASTEXITCODE -ne 0) {
    throw "reportgenerator finalizó con código $LASTEXITCODE"
}

$indexHtml = Join-Path $ReportDir "index.html"
Write-Host ""
Write-Host "Informe HTML: $indexHtml" -ForegroundColor Green
Write-Host "Resumen:      $(Join-Path $ReportDir 'Summary.txt')" -ForegroundColor Green
Write-Host ""
Write-Host "Verificar umbral 90%: .\scripts\check-coverage-threshold.ps1" -ForegroundColor Yellow
