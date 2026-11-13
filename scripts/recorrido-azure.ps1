param([ValidateSet('Configurar','Api','Web')][string]$Modo = 'Api')
$ErrorActionPreference = 'Stop'
$raizAzure = Split-Path $PSScriptRoot -Parent
Push-Location $raizAzure
$nombresAzure = @('ASPNETCORE_ENVIRONMENT','ConnectionStrings__InmobiliariaConnection','Jwt__Key','ApiBaseUrl')
$anterioresAzure = @{}
foreach ($nombre in $nombresAzure) { $anterioresAzure[$nombre] = [Environment]::GetEnvironmentVariable($nombre,'Process') }
try {
    if ($Modo -eq 'Configurar') {
        $entradaAzure = Read-Host 'Cadena SQL de Azure (entrada oculta)' -AsSecureString
        $cadenaAzure = [Net.NetworkCredential]::new('', $entradaAzure).Password
        $constructorAzure = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
        try { $constructorAzure.set_ConnectionString($cadenaAzure) }
        catch { throw 'Introduce la cadena SQL completa: Server=tcp:SERVIDOR.database.windows.net,1433;Database=BASE;User ID=USUARIO;Password=CLAVE; No introduzcas solamente la contrasena.' }
        if ($constructorAzure.DataSource -notmatch '(?i)\.database\.windows\.net(?:,1433)?$') { throw 'La cadena debe apuntar al servidor Azure SQL de tu proyecto.' }
        $constructorAzure.set_Encrypt($true)
        $constructorAzure.set_TrustServerCertificate($false)
        $constructorAzure.set_ConnectTimeout(15)
        $conexionAzure = New-Object System.Data.SqlClient.SqlConnection($constructorAzure.ConnectionString)
        try {
            try { $conexionAzure.Open() }
            catch {
                $causaAzure = $_.Exception
                while ($causaAzure.InnerException) { $causaAzure = $causaAzure.InnerException }
                if ($causaAzure -is [System.Data.SqlClient.SqlException]) {
                    $numeroAzure = $causaAzure.Number
                    switch ($numeroAzure) {
                        40615 { throw 'Azure bloqueo tu IP. En el portal Azure, abre Redes del servidor SQL y agrega la IP publica actual del cliente al firewall. Guarda los cambios y reintenta.' }
                        18456 { throw 'Azure rechazo el usuario o la contrasena SQL. Revisa User ID y Password; sustituye el marcador {your_password} de la cadena.' }
                        4060 { throw 'No se pudo abrir la base indicada. Revisa Database/Initial Catalog y los permisos del usuario SQL.' }
                        -2 { throw 'La conexion a Azure agoto el tiempo de espera. Revisa el servidor, acceso publico, firewall y salida de red al puerto 1433.' }
                        default { throw "Fallo de conexion SQL a Azure (codigo $numeroAzure). La cadena no se guardo. Comunica este codigo para continuar el diagnostico." }
                    }
                }
                throw 'No se pudo abrir la conexion Azure. Revisa el nombre de servidor, acceso de red y certificado. La cadena no se guardo.'
            }
            Write-Host 'Conexion a Azure correcta. Comprobando el esquema...'
            $consultaAzure = $conexionAzure.CreateCommand()
            $consultaAzure.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Propiedades') AND name='EliminadoEn'"
            if ([int]$consultaAzure.ExecuteScalar() -ne 1) { throw 'Falta aplicar database/004_archivado.sql en la base Azure existente antes de usar esta version.' }
        }
        finally { $conexionAzure.Dispose() }
        $bytesAzure = New-Object byte[] 48
        $generadorAzure = [Security.Cryptography.RandomNumberGenerator]::Create()
        try { $generadorAzure.GetBytes($bytesAzure) } finally { $generadorAzure.Dispose() }
        @{ 'ConnectionStrings:InmobiliariaConnection' = $constructorAzure.ConnectionString; 'Jwt:Key' = [Convert]::ToBase64String($bytesAzure) } | ConvertTo-Json | & dotnet user-secrets set --project api/Inmobiliaria.Api
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo guardar la configuracion local.' }
        Write-Host 'Conexion Azure validada y guardada en secretos locales.'
    }
    elseif ($Modo -eq 'Web') {
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
        $env:ApiBaseUrl = 'http://localhost:5180'
        & dotnet run --project web/Inmobiliaria.Web --no-launch-profile -- --urls http://localhost:5175
        if ($LASTEXITCODE -ne 0) { throw 'Fallo el arranque Web.' }
    }
    else {
        $rutaSecretoAzure = Join-Path $env:APPDATA 'Microsoft/UserSecrets/inmobiliaria-luilton-yerferson-dev/secrets.json'
        if (-not (Test-Path $rutaSecretoAzure)) { throw 'Ejecuta recorrido-azure.cmd -Modo Configurar primero.' }
        $secretosAzure = Get-Content -LiteralPath $rutaSecretoAzure -Raw | ConvertFrom-Json
        $validarAzure = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
        $validarAzure.set_ConnectionString($secretosAzure.'ConnectionStrings:InmobiliariaConnection')
        if ($validarAzure.DataSource -notmatch '(?i)\.database\.windows\.net(?:,1433)?$') { throw 'La conexion guardada no es Azure. Ejecuta recorrido-azure.cmd -Modo Configurar.' }
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
        Remove-Item Env:ConnectionStrings__InmobiliariaConnection -ErrorAction SilentlyContinue
        Remove-Item Env:Jwt__Key -ErrorAction SilentlyContinue
        & dotnet run --project api/Inmobiliaria.Api --no-launch-profile -- --urls http://localhost:5180
        if ($LASTEXITCODE -ne 0) { throw 'Fallo el arranque API.' }
    }
} finally {
    foreach ($nombre in $nombresAzure) { [Environment]::SetEnvironmentVariable($nombre,$anterioresAzure[$nombre],'Process') }
    Pop-Location
}
