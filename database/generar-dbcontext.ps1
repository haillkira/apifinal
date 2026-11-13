$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$proyecto = 'api/Inmobiliaria.Data/Inmobiliaria.Data.csproj'
$inicio = 'api/Inmobiliaria.Api/Inmobiliaria.Api.csproj'
$dirContexto = 'api/Inmobiliaria.Data/DatabaseFirst'
$dirModelos = 'api/Inmobiliaria.Data/Models'
$contexto = Join-Path $dirContexto 'InmobiliariaDbFirstContext.cs'
$aspnetAnterior = $env:ASPNETCORE_ENVIRONMENT
$dotnetAnterior = $env:DOTNET_ENVIRONMENT
$respaldo = Join-Path (Join-Path $raiz 'obj') "Inmobiliaria-Scaffold-$([guid]::NewGuid())"
$rutasAnteriores = @(
    $dirContexto
    $dirModelos
    'api/Inmobiliaria.Data/Entities/DatabaseFirst'
    'api/Inmobiliaria.Api/Data/DatabaseFirst'
    'api/Inmobiliaria.Api/Entities/DatabaseFirst'
)
$movidas = @()
$scaffoldIniciado = $false

function Mover-Generados([string]$ruta, [string]$destino) {
    New-Item -ItemType Directory -Path (Split-Path $destino -Parent) -Force | Out-Null
    Move-Item -LiteralPath $ruta -Destination $destino
}

Push-Location $raiz
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:DOTNET_ENVIRONMENT = 'Development'
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo restaurar dotnet-ef.' }

    foreach ($ruta in $rutasAnteriores) {
        if (Test-Path -LiteralPath $ruta) {
            Mover-Generados $ruta (Join-Path $respaldo $ruta)
            $movidas += $ruta
        }
    }

    dotnet build $inicio -m:1
    if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion del proyecto de inicio.' }

    $scaffoldIniciado = $true
    dotnet tool run dotnet-ef -- dbcontext scaffold 'Name=ConnectionStrings:InmobiliariaConnection' `
        Microsoft.EntityFrameworkCore.SqlServer --project $proyecto --startup-project $inicio `
        --context InmobiliariaDbFirstContext --context-dir DatabaseFirst `
        --context-namespace Inmobiliaria.Data.DatabaseFirst `
        --output-dir Models --namespace Inmobiliaria.Data.Entities.DatabaseFirst `
        --use-database-names --no-pluralize --no-onconfiguring --no-build --force
    if ($LASTEXITCODE -ne 0) { throw 'Fallo el scaffold. Revisa el error de EF Core.' }
    if (-not (Test-Path $contexto)) { throw "EF Core no genero $contexto." }

    dotnet build Inmobiliaria.sln -m:1
    if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion del codigo generado.' }

    Write-Host "DbContext generado desde Azure SQL: $contexto"
    Write-Host "Modelos generados: $dirModelos"
    if ($movidas.Count -gt 0) { Write-Host "Scaffold anterior respaldado en $respaldo" }
}
catch {
    if ($scaffoldIniciado) {
        foreach ($ruta in @($dirContexto, $dirModelos)) {
            if (Test-Path -LiteralPath $ruta) {
                Mover-Generados $ruta (Join-Path (Join-Path $respaldo 'fallido') $ruta)
            }
        }
    }
    foreach ($ruta in $movidas) {
        Mover-Generados (Join-Path $respaldo $ruta) $ruta
    }
    throw
}
finally {
    $env:ASPNETCORE_ENVIRONMENT = $aspnetAnterior
    $env:DOTNET_ENVIRONMENT = $dotnetAnterior
    Pop-Location
}
