#Requires -Version 5.1
<#
.SYNOPSIS
  Ejecuta pruebas unitarias (Domain/Application.Tests), genera cobertura y informe HTML.

  Usa una copia en %TEMP% para evitar bloqueos intermitentes de Windows Application Control
  (0x800711C7) sobre DLLs recién compiladas bajo el árbol del repositorio.
#>
param(
    [switch]$NoClean,
    [int]$AppControlSettleSeconds = 5
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
    Write-Error @"
reportgenerator no está instalado. Ejecuta:
  dotnet tool install --global dotnet-reportgenerator-globaltool
"@
}

$coverlet = Join-Path $env:USERPROFILE ".dotnet\tools\coverlet.exe"
if (-not (Test-Path $coverlet)) {
    Write-Error @"
coverlet (coverlet.console) no está instalado. Ejecuta:
  dotnet tool install --global coverlet.console
"@
}

$UnitTestProjects = @(
    @{
        Project = "MissionManagement/MissionManagement.Domain.Tests/MissionManagement.Domain.Tests.csproj"
        Bin     = "MissionManagement/MissionManagement.Domain.Tests/bin/Debug/net10.0"
        Dll     = "MissionManagement.Domain.Tests.dll"
        Stage   = "MM.Domain"
    },
    @{
        Project = "MissionManagement/MissionManagement.Application.Tests/MissionManagement.Application.Tests.csproj"
        Bin     = "MissionManagement/MissionManagement.Application.Tests/bin/Debug/net10.0"
        Dll     = "MissionManagement.Application.Tests.dll"
        Stage   = "MM.Application"
    },
    @{
        Project = "SessionManagement/SessionManagement.Domain.Tests/SessionManagement.Domain.Tests.csproj"
        Bin     = "SessionManagement/SessionManagement.Domain.Tests/bin/Debug/net10.0"
        Dll     = "SessionManagement.Domain.Tests.dll"
        Stage   = "SM.Domain"
    },
    @{
        Project = "SessionManagement/SessionManagement.Application.Tests/SessionManagement.Application.Tests.csproj"
        Bin     = "SessionManagement/SessionManagement.Application.Tests/bin/Debug/net10.0"
        Dll     = "SessionManagement.Application.Tests.dll"
        Stage   = "SM.Application"
    }
)

$TestResultsDir = Join-Path $RepoRoot "TestResults"
$ReportDir = Join-Path $RepoRoot "coverage-report"

if (-not $NoClean) {
    if (Test-Path $TestResultsDir) { Remove-Item $TestResultsDir -Recurse -Force }
    if (Test-Path $ReportDir) { Remove-Item $ReportDir -Recurse -Force }
}

New-Item -ItemType Directory -Path $TestResultsDir -Force | Out-Null

$stagingRoot = Join-Path $env:TEMP ("umbral-coverage-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

try {
    Push-Location $RepoRoot
    try {
        Write-Host "Compilando proyectos de prueba..." -ForegroundColor Cyan
        foreach ($item in $UnitTestProjects) {
            $project = Join-Path $RepoRoot $item.Project
            Write-Host "  build -> $(Split-Path $project -Leaf)" -ForegroundColor DarkGray
            & dotnet build $project -v q
            if ($LASTEXITCODE -ne 0) {
                throw "dotnet build falló en $project (código $LASTEXITCODE)"
            }
        }
    }
    finally {
        Pop-Location
    }

    Write-Host "Copiando ensamblados a TEMP (mitiga Application Control)..." -ForegroundColor Cyan
    foreach ($item in $UnitTestProjects) {
        $src = Join-Path $RepoRoot $item.Bin
        $dst = Join-Path $stagingRoot $item.Stage
        if (-not (Test-Path $src)) {
            throw "No existe el directorio de salida: $src"
        }
        Copy-Item -Path $src -Destination $dst -Recurse -Force
    }

    if ($AppControlSettleSeconds -gt 0) {
        Write-Host "Esperando ${AppControlSettleSeconds}s..." -ForegroundColor Yellow
        Start-Sleep -Seconds $AppControlSettleSeconds
    }

    Write-Host "Ejecutando pruebas + coverlet (Domain + Application)..." -ForegroundColor Cyan
    $coverageIndex = 0
    foreach ($item in $UnitTestProjects) {
        $dll = Join-Path (Join-Path $stagingRoot $item.Stage) $item.Dll
        $outBase = Join-Path $TestResultsDir ("coverage-" + $coverageIndex)
        Write-Host "  -> $($item.Dll)" -ForegroundColor DarkGray

        $targetArgs = "vstest `"$dll`" --logger:console;verbosity=minimal"
        & $coverlet $dll `
            --target "dotnet" `
            --targetargs $targetArgs `
            --format cobertura `
            --output $outBase `
            --verbosity minimal

        if ($LASTEXITCODE -ne 0) {
            throw "coverlet/vstest falló en $($item.Dll) (código $LASTEXITCODE)"
        }

        $produced = @(
            "$outBase.cobertura.xml",
            "$outBase.xml",
            $outBase
        ) | Where-Object { Test-Path $_ } | Select-Object -First 1

        if (-not $produced) {
            throw "No se generó Cobertura para $($item.Dll)"
        }

        if ((Split-Path $produced -Leaf) -ne "coverage.cobertura.xml") {
            $destDir = Join-Path $TestResultsDir $item.Stage
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
            Copy-Item $produced (Join-Path $destDir "coverage.cobertura.xml") -Force
        }

        $coverageIndex++
    }
}
finally {
    if (Test-Path $stagingRoot) {
        Remove-Item $stagingRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$coverageFiles = Get-ChildItem -Path $TestResultsDir -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue
if (-not $coverageFiles) {
    # coverlet may write coverage-N.cobertura.xml at TestResults root
    $coverageFiles = Get-ChildItem -Path $TestResultsDir -Filter "*.cobertura.xml" -ErrorAction SilentlyContinue
}
if (-not $coverageFiles) {
    throw "No se encontró coverage.cobertura.xml en $TestResultsDir"
}

Write-Host "Generando informe (Domain + Application)..." -ForegroundColor Cyan
$reportsPattern = (Join-Path $TestResultsDir "**") + "\*.cobertura.xml"

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
