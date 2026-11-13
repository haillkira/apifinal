param(
    [ValidateSet('Preparar', 'Api', 'Web', 'Aprobar')][string]$Modo = 'Preparar',
    [string]$CorreoAdministrador = 'admin@demo.local',
    [SecureString]$PasswordAdministrador,
    [int]$IdVendedor,
    [switch]$SoloBase
)
$ErrorActionPreference = 'Stop'
$raizProyecto = Split-Path $PSScriptRoot -Parent
$servidorDemo = '.\SQLEXPRESS'
$baseDemo = 'Inmobiliaria_RecorridoLocal'
$conexionDemo = "Server=$servidorDemo;Database=$baseDemo;Integrated Security=true;TrustServerCertificate=true"

function Invoke-SqlDemo([string]$Consulta, [string]$Base = 'master') {
    $salidaSql = & sqlcmd -S $servidorDemo -E -C -b -d $Base -Q $Consulta
    if ($LASTEXITCODE -ne 0) { throw "Fallo SQL en $Base. No se continuo la preparacion." }
    return $salidaSql
}
function Get-PasswordDemo {
    if (-not $script:PasswordAdministrador) { $script:PasswordAdministrador = Read-Host 'Password del administrador local (minimo 16 caracteres)' -AsSecureString }
    return [System.Net.NetworkCredential]::new('', $script:PasswordAdministrador).Password
}

Push-Location $raizProyecto
$variablesDemo = @('ASPNETCORE_ENVIRONMENT', 'ConnectionStrings__InmobiliariaConnection', 'Jwt__Key', 'Jwt__Issuer', 'Jwt__Audience', 'Bootstrap__Email', 'Bootstrap__Password', 'ApiBaseUrl')
$anterioresDemo = @{}
foreach ($nombre in $variablesDemo) { $anterioresDemo[$nombre] = [Environment]::GetEnvironmentVariable($nombre, 'Process') }
try {
    if ($Modo -eq 'Preparar') {
        $existe = & sqlcmd -S $servidorDemo -E -C -b -l 5 -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name=N'$baseDemo'"
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo conectar a SQL Server Express (.\SQLEXPRESS). Comprueba que el servicio SQL Server (SQLEXPRESS) este iniciado.' }
        if (($existe | Out-String).Trim() -eq '0') {
            Invoke-SqlDemo "CREATE DATABASE [$baseDemo]" | Out-Null
        }
        $tablas = & sqlcmd -S $servidorDemo -E -C -b -h -1 -W -d $baseDemo -Q 'SET NOCOUNT ON; SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0'
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo inspeccionar la base de demostracion.' }
        if (($tablas | Out-String).Trim() -eq '0') {
            foreach ($archivo in Get-ChildItem (Join-Path $raizProyecto 'database') -Filter '*.sql' | Sort-Object Name) {
                $temporalSql = [IO.Path]::GetTempFileName()
                try {
                    $sqlDemo = [IO.File]::ReadAllText($archivo.FullName).Replace("N'InmobiliariaDB'", "N'$baseDemo'")
                    [IO.File]::WriteAllText($temporalSql, $sqlDemo, [Text.UTF8Encoding]::new($true))
                    & sqlcmd -S $servidorDemo -E -C -I -b -d $baseDemo -i $temporalSql
                    if ($LASTEXITCODE -ne 0) { throw "Fallo el script $($archivo.Name). La base se conserva para diagnostico." }
                } finally { Remove-Item -LiteralPath $temporalSql -Force }
            }
        }
        $administradores = & sqlcmd -S $servidorDemo -E -C -b -h -1 -W -d $baseDemo -Q 'SET NOCOUNT ON; SELECT COUNT(*) FROM Administradores'
        if ($LASTEXITCODE -ne 0) { throw 'La base local no contiene el esquema esperado; revisala antes de continuar.' }
        if (($administradores | Out-String).Trim() -eq '0' -and -not $SoloBase) {
            $env:ASPNETCORE_ENVIRONMENT = 'Development'
            $env:ConnectionStrings__InmobiliariaConnection = $conexionDemo
            $env:Bootstrap__Email = $CorreoAdministrador
            $env:Bootstrap__Password = Get-PasswordDemo
            & dotnet run --project api/Inmobiliaria.Api --no-launch-profile -- --bootstrap-admin
            if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear el administrador local.' }
        }
        Write-Host 'Base local preparada. Inicia Api y Web en dos terminales con este mismo script.'
    }
    elseif ($Modo -eq 'Api') {
        & sqlcmd -S $servidorDemo -E -C -I -b -d $baseDemo -i (Join-Path $raizProyecto 'database/005_respuesta_solicitudes.sql')
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo aplicar la actualizacion de solicitudes.' }
        & sqlcmd -S $servidorDemo -E -C -I -b -d $baseDemo -i (Join-Path $raizProyecto 'database/004_archivado.sql')
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo aplicar la actualizacion de archivado.' }
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
        $env:ConnectionStrings__InmobiliariaConnection = $conexionDemo
        $bytesClave = New-Object byte[] 48
        $generadorClave = [Security.Cryptography.RandomNumberGenerator]::Create()
        try { $generadorClave.GetBytes($bytesClave) } finally { $generadorClave.Dispose() }
        $env:Jwt__Key = [Convert]::ToBase64String($bytesClave)
        $env:Jwt__Issuer = 'inmobiliaria-recorrido-local'
        $env:Jwt__Audience = 'inmobiliaria-recorrido-local'
        & dotnet run --project api/Inmobiliaria.Api --no-launch-profile -- --urls http://localhost:5180
        if ($LASTEXITCODE -ne 0) { throw 'La API termino con un error.' }
    }
    elseif ($Modo -eq 'Web') {
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
        $env:ApiBaseUrl = 'http://localhost:5180'
        & dotnet run --project web/Inmobiliaria.Web --no-launch-profile -- --urls http://localhost:5175
        if ($LASTEXITCODE -ne 0) { throw 'La web termino con un error.' }
    }
    else {
        $credenciales = @{ correo = $CorreoAdministrador; password = (Get-PasswordDemo) } | ConvertTo-Json
        $login = Invoke-RestMethod -Uri 'http://localhost:5180/api/v1/auth/login' -Method Post -ContentType 'application/json' -Body $credenciales
        $cabeceras = @{ Authorization = "Bearer $($login.accessToken)" }
        if ($IdVendedor -lt 1) {
            Invoke-RestMethod -Uri 'http://localhost:5180/api/v1/admin/vendedores/pendientes' -Headers $cabeceras | Format-Table
            Write-Host 'Repite con -IdVendedor NUMERO para aprobar el vendedor elegido.'
        }
        else {
            Invoke-RestMethod -Uri "http://localhost:5180/api/v1/admin/vendedores/$IdVendedor/aprobar" -Method Post -Headers $cabeceras -ContentType 'application/json' -Body '{"motivo":null}'
            Write-Host "Vendedor $IdVendedor aprobado."
        }
    }
}
finally {
    foreach ($nombre in $variablesDemo) { [Environment]::SetEnvironmentVariable($nombre, $anterioresDemo[$nombre], 'Process') }
    Pop-Location
}
