$ErrorActionPreference = 'Stop'
& sqlcmd -S '.\SQLEXPRESS' -E -C -b -l 5 -Q 'SELECT 1' | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'No se pudo conectar a SQL Server Express (.\SQLEXPRESS).' }
$project = Join-Path $PSScriptRoot 'Inmobiliaria.IntegrationTests/Inmobiliaria.IntegrationTests.csproj'
& dotnet test $project --nologo
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas de integracion.' }
