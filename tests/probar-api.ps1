<#
 probar-api.ps1
 Prueba automatica del flujo completo de la API Inmobiliaria.

 Requisitos:
   - La API debe estar corriendo en otra terminal:
       dotnet run --project .\api\Inmobiliaria.Api
   - El administrador ya debe existir (bootstrap hecho).

 Uso (desde la raiz del repo):
   powershell -ExecutionPolicy Bypass -File .\tests\probar-api.ps1
   (te pedira la contrasena del admin; no se guarda ni se muestra)

 Opciones:
   -BaseUrl       http://localhost:5180
   -AdminEmail    admin@inmobiliaria.test
   -AdminPassword (si no la pones, te la pide)
   -Overrides     @{ nombreCampo = valor }  para forzar el valor de un campo
                  Ejemplo: -Overrides @{ idCiudad = 3; idMoneda = 2 }

 Los cuerpos de las peticiones se arman leyendo /openapi/v1.json de tu API,
 asi que se adaptan a los campos que tengan tus DTO.
 Cada ejecucion crea datos de prueba nuevos (vendedor, propiedad, publicacion).
#>
param(
    [string]$BaseUrl = "http://localhost:5180",
    [string]$AdminEmail = "admin@inmobiliaria.test",
    [string]$AdminPassword = "",
    [hashtable]$Overrides = @{}
)

$ErrorActionPreference = "Stop"
$script:Pass = 0
$script:Fail = 0
$script:Warnings = New-Object System.Collections.ArrayList
$script:Doc = $null
$script:Ids = @{}
$script:LastSent = ""
$script:Stamp = (Get-Date).ToString("yyyyMMddHHmmss")
$script:SellerEmail = "vendedor.$($script:Stamp)@inmobiliaria.test"
$script:SellerPass = "Prueba#2026-Ok1"
$script:Cedula = -join (1..11 | ForEach-Object { Get-Random -Minimum 0 -Maximum 10 })

if (-not $AdminPassword) {
    $sec = Read-Host "Contrasena del administrador" -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
    $AdminPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
}

# ---------------------------------------------------------------
# Utilidades HTTP
# ---------------------------------------------------------------
function Invoke-Api {
    param([string]$Method, [string]$Path, $Body = $null, [string]$Token = "")
    $uri = $BaseUrl.TrimEnd('/') + $Path
    $headers = @{}
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }
    $params = @{ Method = $Method; Uri = $uri; Headers = $headers; UseBasicParsing = $true }
    $script:LastSent = ""
    if ($null -ne $Body) {
        $script:LastSent = ConvertTo-Json -InputObject $Body -Depth 10 -Compress
        $params["Body"] = $script:LastSent
        $params["ContentType"] = "application/json"
    }
    $status = 0
    $raw = ""
    try {
        $r = Invoke-WebRequest @params
        $status = [int]$r.StatusCode
        $raw = [string]$r.Content
    }
    catch {
        $resp = $_.Exception.Response
        if ($null -eq $resp) {
            throw "No pude conectar con $uri . La API esta corriendo? ($($_.Exception.Message))"
        }
        $status = [int]$resp.StatusCode
        if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
            $raw = $_.ErrorDetails.Message
        }
        else {
            try {
                $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
                $raw = $sr.ReadToEnd()
            }
            catch { $raw = "" }
        }
    }
    $json = $null
    if ($raw) {
        try { $json = $raw | ConvertFrom-Json } catch { $json = $null }
    }
    return [pscustomobject]@{ Status = $status; Raw = $raw; Json = $json }
}

function Check {
    param([string]$Name, $Resp, [int[]]$Expected, [bool]$Critical = $true)
    if ($Expected -contains $Resp.Status) {
        Write-Host ("[OK]    {0}  (HTTP {1})" -f $Name, $Resp.Status) -ForegroundColor Green
        $script:Pass++
        return $true
    }
    Write-Host ("[FALLO] {0}  (HTTP {1}, esperaba {2})" -f $Name, $Resp.Status, ($Expected -join "/")) -ForegroundColor Red
    if ($Resp.Raw) {
        $t = $Resp.Raw
        if ($t.Length -gt 800) { $t = $t.Substring(0, 800) + "..." }
        Write-Host ("        Respuesta: " + $t) -ForegroundColor Yellow
    }
    if ($script:LastSent) {
        $shown = [regex]::Replace($script:LastSent, '("[^"]*(?:pass|contrasena|clave)[^"]*"\s*:\s*)"[^"]*"', '$1"***"', 'IgnoreCase')
        Write-Host ("        Cuerpo enviado: " + $shown) -ForegroundColor Yellow
    }
    $script:Fail++
    if ($Critical) { throw "Paso critico fallido: $Name" }
    return $false
}

function Assert-That {
    param([string]$Name, [bool]$Cond, [string]$Detail = "")
    if ($Cond) {
        Write-Host ("[OK]    {0}" -f $Name) -ForegroundColor Green
        $script:Pass++
    }
    else {
        Write-Host ("[FALLO] {0}" -f $Name) -ForegroundColor Red
        if ($Detail) { Write-Host ("        " + $Detail) -ForegroundColor Yellow }
        $script:Fail++
    }
}

function Add-Warn([string]$Msg) {
    if (-not $script:Warnings.Contains($Msg)) { [void]$script:Warnings.Add($Msg) }
}

# ---------------------------------------------------------------
# Lectura del OpenAPI y generacion de cuerpos
# ---------------------------------------------------------------
function Get-Prop($obj, [string]$name) {
    if ($null -eq $obj) { return $null }
    $p = $obj.PSObject.Properties | Where-Object { $_.Name -ieq $name } | Select-Object -First 1
    if ($p) { return $p.Value }
    return $null
}

function Resolve-Schema($schema) {
    for ($i = 0; $i -lt 10; $i++) {
        if (-not $schema) { break }
        $ref = Get-Prop $schema '$ref'
        if ($ref) {
            $schemas = Get-Prop $script:Doc.components 'schemas'
            $schema = Get-Prop $schemas ($ref.Split('/')[-1])
            continue
        }
        $next = $null
        foreach ($k in 'allOf', 'anyOf', 'oneOf') {
            $alt = Get-Prop $schema $k
            if ($alt -and -not $next) {
                $next = $alt | Where-Object { (Get-Prop $_ 'type') -ne 'null' } | Select-Object -First 1
            }
        }
        if ($next) { $schema = $next; continue }
        break
    }
    return $schema
}

function Get-SchemaType($s) {
    $t = Get-Prop $s 'type'
    if ($t -is [array]) { $t = $t | Where-Object { $_ -ne 'null' } | Select-Object -First 1 }
    return $t
}

function Resolve-IdValue([string]$Name) {
    $n = $Name.ToLower()
    if ($n -match 'propiedad|inmueble' -and $script:Ids.propiedad) { return $script:Ids.propiedad }
    if ($n -match 'tipo' -and $script:Ids.tipo) { return $script:Ids.tipo }
    if ($n -match 'provincia' -and $script:Ids.provincia) { return $script:Ids.provincia }
    if ($n -match 'vendedor|usuario|propietario|agente' -and $script:Ids.vendedor) { return $script:Ids.vendedor }
    Add-Warn "El campo '$Name' es un id que no pude deducir; use 1. Si falla, repite con -Overrides @{ $Name = <id valido> }"
    return 1
}

function Get-NumberSample($s, [string]$Name, [bool]$IsInt) {
    if ($IsInt -and ($Name -cmatch '^[iI]d([A-Z_]|$)|[a-z]Id$')) { return (Resolve-IdValue $Name) }
    $n = $Name.ToLower()
    $v = 1
    if ($n -match 'precio|monto|costo|valor|renta|alquiler') { $v = 25000 }
    elseif ($n -match 'deposito|fianza') { $v = 5000 }
    elseif ($n -match 'habitacion|dormitorio|cuarto') { $v = 2 }
    elseif ($n -match 'bano|parqueo|parking|garaje|estacionamiento') { $v = 1 }
    elseif ($n -match 'area|metro|m2|superficie|construccion|terreno') { $v = 100 }
    elseif ($n -match 'latitud') { $v = 18.4861 }
    elseif ($n -match 'longitud') { $v = -69.9312 }
    elseif ($n -match 'meses|duracion|plazo') { $v = 12 }
    $min = Get-Prop $s 'minimum'
    $max = Get-Prop $s 'maximum'
    if ($null -ne $min -and $v -lt $min) { $v = $min }
    if ($null -ne $max -and $v -gt $max) { $v = $max }
    if ($IsInt) { $v = [int][math]::Round([double]$v) }
    return $v
}

function Get-StringSample($s, [string]$Name) {
    $n = $Name.ToLower()
    $fmt = Get-Prop $s 'format'
    $min = Get-Prop $s 'minLength'
    $max = Get-Prop $s 'maxLength'
    $stamp = $script:Stamp
    $pat = Get-Prop $s 'pattern'
    if ($pat) { Add-Warn "El campo '$Name' tiene un patron ($pat); puede que mi valor de prueba no lo cumpla. Usa -Overrides si falla." }

    if ($fmt -eq 'email' -or $n -match 'correo|email') { $v = $script:SellerEmail }
    elseif ($n -match 'password|contrasena|clave') { $v = $script:SellerPass }
    elseif ($n -match 'cedula|identificacion|documento') { $v = $script:Cedula }
    elseif ($n -match 'telefono|celular|whatsapp') { $v = "8095551234" }
    elseif ($fmt -eq 'uuid') { $v = [guid]::NewGuid().ToString() }
    elseif ($fmt -eq 'date-time' -or $fmt -eq 'date' -or $n -match 'fecha') {
        $d = [DateTime]::UtcNow
        if ($n -match 'nacimiento') { $d = $d.AddYears(-30) }
        elseif ($n -match 'fin|hasta|venc|expira|termina') { $d = $d.AddYears(1) }
        if ($fmt -eq 'date') { $v = $d.ToString("yyyy-MM-dd") }
        else { $v = $d.ToString("yyyy-MM-ddTHH:mm:ssZ") }
    }
    elseif ($fmt -eq 'uri' -or $n -match 'url|foto|imagen') { $v = "https://example.com/prueba.jpg" }
    elseif ($n -match 'apellido') { $v = "Prueba" }
    elseif ($n -match 'nombre') { $v = "Vendedor" }
    elseif ($n -match 'moneda') { $v = "DOP" }
    elseif ($n -match 'titulo') { $v = "Casa de prueba $stamp" }
    elseif ($n -match 'direccion') { $v = "Calle Prueba 123 $stamp" }
    else { $v = "Prueba $Name $stamp" }

    if ($max -and $v.Length -gt [int]$max) { $v = $v.Substring(0, [int]$max) }
    if ($min -and $v.Length -lt [int]$min) { $v = $v.PadRight([int]$min, 'x') }
    return $v
}

function New-Sample {
    param($Schema, [string]$Name = "")
    foreach ($k in $Overrides.Keys) {
        if ($k -ieq $Name) { return $Overrides[$k] }
    }
    $s = Resolve-Schema $Schema
    if (-not $s) { return "x" }
    $enum = Get-Prop $s 'enum'
    if ($enum) { return @($enum)[0] }
    $type = Get-SchemaType $s
    if (-not $type -and (Get-Prop $s 'properties')) { $type = 'object' }

    switch ($type) {
        'object' {
            $o = [ordered]@{}
            $props = Get-Prop $s 'properties'
            if ($props) {
                $req = Get-Prop $s 'required'
                if ($req) { $req = @($req) } else { $req = @() }
                foreach ($p in $props.PSObject.Properties) {
                    if ($req.Count -eq 0 -or ($req -contains $p.Name)) {
                        $o[$p.Name] = New-Sample -Schema $p.Value -Name $p.Name
                    }
                }
            }
            return $o
        }
        'array' {
            return , @(New-Sample -Schema (Get-Prop $s 'items') -Name $Name)
        }
        'integer' { return (Get-NumberSample $s $Name $true) }
        'number' { return (Get-NumberSample $s $Name $false) }
        'boolean' { return $true }
        'string' { return (Get-StringSample $s $Name) }
        default { return "x" }
    }
}

function Get-BodySchema([string]$Path, [string]$Method) {
    $item = Get-Prop $script:Doc.paths $Path
    if (-not $item) { return $null }
    $op = Get-Prop $item $Method.ToLower()
    $rb = Get-Prop $op 'requestBody'
    $content = Get-Prop $rb 'content'
    if (-not $content) { return $null }
    $media = Get-Prop $content 'application/json'
    if (-not $media) { $media = $content.PSObject.Properties | Select-Object -First 1 | ForEach-Object { $_.Value } }
    return (Get-Prop $media 'schema')
}

function New-Body([string]$Path, [string]$Method) {
    $schema = Get-BodySchema $Path $Method
    if (-not $schema) { return $null }
    return (New-Sample -Schema $schema -Name "")
}

function Set-Field($Body, [string]$Regex, $Value) {
    if ($null -eq $Body) { return }
    foreach ($k in @($Body.Keys)) {
        if ($k -match $Regex) { $Body[$k] = $Value }
    }
}

function New-LoginBody([string]$Email, [string]$Pass) {
    $b = New-Body "/api/v1/auth/login" "post"
    if ($null -eq $b) { return @{ correo = $Email; password = $Pass } }
    Set-Field $b 'correo|email|usuario' $Email
    Set-Field $b 'pass|contrasena|clave' $Pass
    return $b
}

# ---------------------------------------------------------------
# Extraccion de datos de las respuestas
# ---------------------------------------------------------------
function Find-Token($o) {
    if ($null -eq $o) { return $null }
    if ($o -is [string]) {
        if ($o -match '^eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]*$') { return $o }
        return $null
    }
    if ($o -is [System.Management.Automation.PSCustomObject]) {
        foreach ($p in $o.PSObject.Properties) {
            $t = Find-Token $p.Value
            if ($t) { return $t }
        }
    }
    return $null
}

function Get-Items($json) {
    if ($null -eq $json) { return @() }
    if ($json -is [array]) { return @($json) }
    foreach ($k in 'items', 'data', 'results', 'value') {
        $v = Get-Prop $json $k
        if ($null -ne $v) { return @($v) }
    }
    return @($json)
}

function Get-IdCandidates($o, [string[]]$Prefer, [bool]$Generic) {
    $out = New-Object System.Collections.ArrayList
    if ($o -is [int] -or $o -is [long]) {
        [void]$out.Add([int]$o)
        return $out.ToArray()
    }
    if ($o -is [System.Management.Automation.PSCustomObject]) {
        foreach ($name in $Prefer) {
            $v = Get-Prop $o $name
            if (($v -is [int] -or $v -is [long]) -and (-not $out.Contains([int]$v))) { [void]$out.Add([int]$v) }
        }
        if ($Generic) {
            foreach ($p in $o.PSObject.Properties) {
                if ($p.Name -match '^id' -and ($p.Value -is [int] -or $p.Value -is [long]) -and (-not $out.Contains([int]$p.Value))) {
                    [void]$out.Add([int]$p.Value)
                }
            }
        }
        if ($out.Count -eq 0) {
            $d = Get-Prop $o 'data'
            if ($d) { return (Get-IdCandidates $d $Prefer $Generic) }
        }
    }
    return $out.ToArray()
}

function Test-PendingContains($json) {
    foreach ($it in @(Get-Items $json)) {
        $txt = ConvertTo-Json -InputObject $it -Depth 10 -Compress
        if ($txt -match [regex]::Escape($script:SellerEmail)) { return $true }
    }
    return $false
}

# ---------------------------------------------------------------
# Flujo de pruebas
# ---------------------------------------------------------------
try {
    Write-Host ""
    Write-Host "== Pruebas de la API en $BaseUrl ==" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "-- Arranque --" -ForegroundColor Cyan
    $r = Invoke-Api GET "/health"
    Check "Health responde" $r @(200) | Out-Null

    $r = Invoke-Api GET "/openapi/v1.json"
    Check "OpenAPI disponible (de aqui salen los cuerpos)" $r @(200) | Out-Null
    $script:Doc = $r.Json
    if (-not $script:Doc) { throw "No pude leer /openapi/v1.json como JSON." }

    Write-Host ""
    Write-Host "-- Administrador --" -ForegroundColor Cyan
    $r = Invoke-Api POST "/api/v1/auth/login" (New-LoginBody $AdminEmail "clave-incorrecta-xyz")
    Check "Login con contrasena incorrecta es rechazado" $r @(400, 401) $false | Out-Null

    $r = Invoke-Api POST "/api/v1/auth/login" (New-LoginBody $AdminEmail $AdminPassword)
    Check "Login del administrador" $r @(200) | Out-Null
    $adminToken = Find-Token $r.Json
    if (-not $adminToken) { throw "El login respondio 200 pero no encontre el token en la respuesta." }

    $r = Invoke-Api GET "/api/v1/auth/me" -Token $adminToken
    Check "GET /auth/me con token de admin" $r @(200) $false | Out-Null

    $r = Invoke-Api GET "/api/v1/auth/me"
    Check "GET /auth/me sin token es rechazado" $r @(401) $false | Out-Null

    $r = Invoke-Api GET "/api/v1/admin/vendedores/pendientes"
    Check "Pendientes sin token es rechazado" $r @(401) $false | Out-Null

    Write-Host ""
    Write-Host "-- Registro del vendedor --" -ForegroundColor Cyan
    $r = Invoke-Api GET "/api/v1/admin/vendedores/pendientes" -Token $adminToken
    Check "Foto inicial de vendedores pendientes" $r @(200) | Out-Null
    $before = @{}
    foreach ($it in @(Get-Items $r.Json)) {
        $before[(ConvertTo-Json -InputObject $it -Depth 10 -Compress)] = $true
    }

    $bad = New-Body "/api/v1/auth/vendedores" "post"
    if ($null -ne $bad) {
        Set-Field $bad 'pass|contrasena|clave' "abc"
        Set-Field $bad 'correo|email' "invalido.$($script:Stamp)@inmobiliaria.test"
        $r = Invoke-Api POST "/api/v1/auth/vendedores" $bad
        Check "Registro con contrasena corta es rechazado" $r @(400, 422) $false | Out-Null
    }

    $body = New-Body "/api/v1/auth/vendedores" "post"
    if ($null -eq $body) { throw "No encontre el esquema de POST /api/v1/auth/vendedores en el OpenAPI." }
    $r = Invoke-Api POST "/api/v1/auth/vendedores" $body
    Check "Registrar vendedor ($($script:SellerEmail))" $r @(200, 201, 204) | Out-Null

    Write-Host ""
    Write-Host "-- Aprobacion por el administrador --" -ForegroundColor Cyan
    $r = Invoke-Api GET "/api/v1/admin/vendedores/pendientes" -Token $adminToken
    Check "Listar vendedores pendientes" $r @(200) | Out-Null
    $items = @(Get-Items $r.Json)
    # Mi vendedor es el que trae mi correo, o si la lista no muestra correos,
    # el que no estaba en la foto inicial.
    $mine = @($items | Where-Object {
            $txt = ConvertTo-Json -InputObject $_ -Depth 10 -Compress
            ($txt -match [regex]::Escape($script:SellerEmail)) -or (-not $before.ContainsKey($txt))
        })
    if ($mine.Count -ne 1) {
        Write-Host ("        Pendientes recibidos: " + (ConvertTo-Json -InputObject $items -Depth 10 -Compress)) -ForegroundColor Yellow
        throw "No pude identificar a mi vendedor entre los pendientes ($($mine.Count) candidatos)."
    }
    $cands = @(Get-IdCandidates $mine[0] @('idVendedor', 'id', 'idUsuario') $false)
    if ($cands.Count -eq 0) { throw "No encontre un id en el vendedor pendiente." }

    $approved = $false
    foreach ($cid in $cands) {
        $b = New-Body "/api/v1/admin/vendedores/{id}/aprobar" "post"
        $r = Invoke-Api POST "/api/v1/admin/vendedores/$cid/aprobar" $b -Token $adminToken
        if ($r.Status -in 200, 201, 204) {
            Check "Aprobar vendedor (id $cid)" $r @(200, 201, 204) | Out-Null
            $script:Ids.vendedor = $cid
            $approved = $true
            break
        }
        Write-Host "        (el id $cid devolvio HTTP $($r.Status); pruebo el siguiente)" -ForegroundColor DarkYellow
    }
    if (-not $approved) { Check "Aprobar vendedor" $r @(200, 204) | Out-Null }

    $r = Invoke-Api GET "/api/v1/admin/vendedores/pendientes" -Token $adminToken
    Assert-That "El vendedor ya no aparece como pendiente" (-not (Test-PendingContains $r.Json))

    Write-Host ""
    Write-Host "-- Vendedor --" -ForegroundColor Cyan
    $r = Invoke-Api POST "/api/v1/auth/login" (New-LoginBody $script:SellerEmail $script:SellerPass)
    Check "Login del vendedor aprobado" $r @(200) | Out-Null
    $sellerToken = Find-Token $r.Json
    if (-not $sellerToken) { throw "Login del vendedor OK pero no encontre el token." }

    $r = Invoke-Api GET "/api/v1/admin/vendedores/pendientes" -Token $sellerToken
    Check "Un vendedor NO puede usar rutas de admin" $r @(401, 403) $false | Out-Null

    Write-Host ""
    Write-Host "-- Catalogos --" -ForegroundColor Cyan
    $r = Invoke-Api GET "/api/v1/catalogos/tipos"
    Check "Catalogo de tipos" $r @(200) | Out-Null
    $c = @(Get-IdCandidates (Get-Items $r.Json | Select-Object -First 1) @() $true)
    if ($c.Count -gt 0) { $script:Ids.tipo = $c[0] } else { Add-Warn "El catalogo de tipos parece vacio (corriste 002_catalogos.sql?)." }

    $r = Invoke-Api GET "/api/v1/catalogos/provincias"
    Check "Catalogo de provincias" $r @(200) | Out-Null
    $c = @(Get-IdCandidates (Get-Items $r.Json | Select-Object -First 1) @() $true)
    if ($c.Count -gt 0) { $script:Ids.provincia = $c[0] } else { Add-Warn "El catalogo de provincias parece vacio (corriste 002_catalogos.sql?)." }

    Write-Host ""
    Write-Host "-- Propiedad y publicacion de alquiler --" -ForegroundColor Cyan
    $body = New-Body "/api/v1/propiedades" "post"
    if ($null -eq $body) { throw "No encontre el esquema de POST /api/v1/propiedades en el OpenAPI." }
    $r = Invoke-Api POST "/api/v1/propiedades" $body -Token $sellerToken
    Check "Crear propiedad" $r @(200, 201) | Out-Null
    $pc = @(Get-IdCandidates $r.Json @('idPropiedad', 'id', 'idInmueble') $true)
    if ($pc.Count -eq 0) { throw "La propiedad se creo pero no encontre su id en la respuesta: $($r.Raw)" }
    $script:Ids.propiedad = $pc[0]

    $body = New-Body "/api/v1/publicaciones" "post"
    if ($null -eq $body) { throw "No encontre el esquema de POST /api/v1/publicaciones en el OpenAPI." }
    $r = Invoke-Api POST "/api/v1/publicaciones" $body -Token $sellerToken
    Check "Crear publicacion de alquiler" $r @(200, 201) | Out-Null

    Write-Host ""
    Write-Host "-- Listado publico --" -ForegroundColor Cyan
    $r = Invoke-Api GET "/api/v1/publicaciones"
    Check "Listado publico (sin token)" $r @(200) | Out-Null
    Assert-That "La publicacion nueva aparece en el listado" ($r.Raw -match [regex]::Escape($script:Stamp)) "No encontre el texto de prueba ($($script:Stamp)) en el listado. Revisalo a mano: puede que el listado no muestre esos campos o este paginado."
    $leak = ($r.Raw -match 'passwordhash|password_hash|"cedula"|"hash"') -or ($r.Raw -match [regex]::Escape($script:Cedula))
    Assert-That "El listado publico no expone hashes ni cedulas" (-not $leak)
}
catch {
    Write-Host ""
    Write-Host ("DETENIDO: " + $_.Exception.Message) -ForegroundColor Red
    $script:Fail++
}

Write-Host ""
Write-Host "== Resultado: $($script:Pass) correctas, $($script:Fail) con fallo ==" -ForegroundColor Cyan
if ($script:Warnings.Count -gt 0) {
    Write-Host "Avisos:" -ForegroundColor Yellow
    foreach ($w in $script:Warnings) { Write-Host ("  - " + $w) -ForegroundColor Yellow }
}
Write-Host "Datos de prueba creados: vendedor $($script:SellerEmail) (borralos en SSMS si quieres limpiar)."
if ($script:Fail -gt 0) { exit 1 } else { exit 0 }