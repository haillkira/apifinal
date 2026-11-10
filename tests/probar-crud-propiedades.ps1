param(
    [pscredential]$Vendedor,
    [string]$BaseUrl = 'http://localhost:5180'
)

$ErrorActionPreference = 'Stop'
$api = "$($BaseUrl.TrimEnd('/'))/api/v1"
if (-not $Vendedor) {
    $Vendedor = Get-Credential -Message 'Correo y clave de un vendedor aprobado'
}
if (-not $Vendedor) { throw 'Se requiere un vendedor aprobado.' }

$login = @{
    correo = $Vendedor.UserName
    password = $Vendedor.GetNetworkCredential().Password
} | ConvertTo-Json -Compress
$sesion = Invoke-RestMethod -Method Post -Uri "$api/auth/login" `
    -ContentType 'application/json' -Body $login
if ($sesion.usuario.rol -ne 'Vendedor') { throw 'La cuenta debe tener el rol Vendedor.' }
$headers = @{ Authorization = "Bearer $($sesion.accessToken)" }

$tipos = Invoke-RestMethod -Uri "$api/catalogos/tipos"
$tipo = $tipos | Where-Object { $_.nombre -eq 'Apartamento' } | Select-Object -First 1
$provincias = Invoke-RestMethod -Uri "$api/catalogos/provincias"
$provincia = $provincias | Where-Object { $_.nombre -eq 'Maria Trinidad Sanchez' } | Select-Object -First 1
if (-not $tipo -or -not $provincia) { throw 'Falta el tipo Apartamento o la provincia de prueba.' }

$datos = @{
    idTipoPropiedad = $tipo.idTipoPropiedad
    idProvincia = $provincia.idProvincia
    titulo = "Prueba CRUD $([guid]::NewGuid().ToString('N'))"
    descripcion = 'Propiedad temporal para verificar Database First.'
    ciudad = 'Nagua'
    sector = 'Centro'
    direccion = 'Direccion ficticia'
    habitaciones = 2
    banos = 1
    parqueos = 1
    metrosCuadrados = 75
}

$uri = $null
try {
    $creada = Invoke-RestMethod -Method Post -Uri "$api/propiedades" -Headers $headers `
        -ContentType 'application/json' -Body ($datos | ConvertTo-Json -Compress)
    if (-not $creada.idPropiedad) { throw 'La API no devolvio el id de la propiedad creada.' }
    $uri = "$api/propiedades/$($creada.idPropiedad)"

    $encontrada = Invoke-RestMethod -Uri $uri -Headers $headers
    if ($encontrada.idPropiedad -ne $creada.idPropiedad -or $encontrada.titulo -ne $datos.titulo) {
        throw 'Buscar no devolvio la propiedad esperada.'
    }
    if ($encontrada.PSObject.Properties.Name -contains 'idVendedor') {
        throw 'Buscar expuso un campo interno fuera del DTO.'
    }

    $datos.titulo += ' editada'
    Invoke-RestMethod -Method Put -Uri $uri -Headers $headers -ContentType 'application/json' `
        -Body ($datos | ConvertTo-Json -Compress) | Out-Null

    $lista = Invoke-RestMethod -Uri "$api/propiedades" -Headers $headers
    $actualizada = $lista | Where-Object { $_.idPropiedad -eq $creada.idPropiedad }
    if ($actualizada.titulo -ne $datos.titulo) { throw 'GetList no devolvio el cambio guardado.' }

    Invoke-RestMethod -Method Delete -Uri $uri -Headers $headers | Out-Null
    $eliminadaUri = $uri
    $uri = $null
    try {
        Invoke-RestMethod -Uri $eliminadaUri -Headers $headers | Out-Null
        throw 'Buscar encontro una propiedad eliminada.'
    }
    catch {
        if ([int]$_.Exception.Response.StatusCode -ne 404) { throw }
    }

    Write-Host "OK: CRUD de propiedades y DTO comprobados con la propiedad temporal $($creada.idPropiedad)."
}
finally {
    if ($uri) {
        try { Invoke-RestMethod -Method Delete -Uri $uri -Headers $headers | Out-Null }
        catch { Write-Warning "No se pudo retirar la propiedad temporal $uri" }
    }
}
