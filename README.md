# Inmobiliaria - Luilton y Yerferson
Base inicial .NET 10, solo alquiler. Yerferson Rosario Peña: 100444062. Datos de Luilton pendientes.

## Arranque rapido
Extraer en una carpeta nueva. Abrir la carpeta Inmobiliaria_Progm4 que contiene Inmobiliaria.sln.
```powershell
dotnet build
dotnet run --project .\api\Inmobiliaria.Checks
dotnet run --project .\api\Inmobiliaria.Api
```
Abrir http://localhost:5180/ y http://localhost:5180/health . El health valida el proceso, NO conectividad SQL.
Sin conexion Azure la API arranca en Development y las rutas /api devuelven 503 explicativo. Compilar no necesita una base configurada. NuGet requiere Internet.

## Contenido implementado
Solucion y proyectos API/Contracts/Checks. 26 entidades EF y mapeo de claves, relaciones e indices. Registro comprador/vendedor, login JWT, perfil, bootstrap administrador, lista/aprobacion/rechazo de vendedores, catalogos, crear propiedad/publicacion de alquiler y listado publico paginado. Solo endpoint publico expone campos elegidos, no hashes ni cedulas.
Checks es un ejecutable de comprobaciones de reglas, NO un proyecto xUnit: dotnet test no lo ejecuta. Pruebas SQL/HTTP pendientes de entorno.
Clientes web, escritorio y Android reservados con README; no se presentan como aplicaciones hechas.

## Base Azure: paso posterior al build
1. Crear Azure SQL InmobiliariaDB vacia. No ejecutar contra una base con datos existentes.
2. Revisar y ejecutar en SSMS database/001_esquema_reconstruido.sql, luego 002_catalogos.sql y 003_vistas.sql. No se ejecutan automaticamente ni se sobrescribe nada.
3. Configurar secretos localmente, desde api/Inmobiliaria.Api:
```powershell
dotnet user-secrets set "ConnectionStrings:InmobiliariaConnection" "TU_CONEXION_SQL_CON_CIFRADO"
dotnet user-secrets set "Jwt:Key" "TU_CLAVE_ALEATORIA_DE_AL_MENOS_32_BYTES"
```
Esos textos son marcadores que debes sustituir localmente. No subir secretos a Git. Generar la clave con generador criptografico, no una frase facil.
4. Reiniciar API y consultar catalogos. Verificar firewall, servidor, base, permisos y cifrado.
5. Para bootstrap local configurar Bootstrap:Email y Bootstrap:Password (minimo 16 caracteres) mediante user-secrets y ejecutar:
```powershell
dotnet run --project .\api\Inmobiliaria.Api -- --bootstrap-admin
```
Comando desde la RAIZ. Bloqueado si ya hay administrador; requiere catalogos existentes. Borrar secretos Bootstrap despues. No hay administrador con contraseña incluida en el ZIP.
6. Registrar vendedor desde /api/v1/auth/vendedores. Cedula de 11 digitos solo valida formato; usar datos ficticios. Login admin -> pendientes -> aprobar -> login vendedor -> crear propiedad y alquiler. Consultar contratos DTO en api/Inmobiliaria.Contracts.

## Produccion
Aun no listo para datos reales ni entrega final. Completar docs/pendientes.md y pruebas antes de desplegar. Production exige conexion y Jwt:Key y debe utilizar HTTPS. Configurar ConnectionStrings__InmobiliariaConnection y Jwt__Key en App Service. No publicar con secretos de desarrollo.

## Publish posterior
```powershell
dotnet publish .\api\Inmobiliaria.Api -c Release -o .\artifacts\api
Compress-Archive -Path .\artifacts\api\* -DestinationPath .\artifacts\Inmobiliaria.Api.zip -Force
```
Crear salida limpia antes de repetir publicacion. No confundir este ZIP de codigo fuente con un ZIP publicado para Azure.

## Flujo equipo
pull -> rama -> cambios/pruebas -> commit -> push -> PR -> revision del compañero -> merge -> pull -> limpieza. Usar identidad Git de cada integrante.
