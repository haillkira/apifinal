# Inmobiliaria - Luilton y Yerferson
Base inicial .NET 10, solo alquiler. Yerferson Rosario Peña: 100444062. Datos de Luilton pendientes.

## Arranque rapido
Extraer en una carpeta nueva. Abrir la carpeta Inmobiliaria_Progm4 que contiene Inmobiliaria.sln.
```powershell
dotnet build
dotnet test Inmobiliaria.sln
dotnet run --project .\api\Inmobiliaria.Checks
dotnet run --project .\api\Inmobiliaria.Api
```
Abrir http://localhost:5180/ y http://localhost:5180/health . El health valida el proceso, NO conectividad SQL.
Sin conexion Azure la API arranca en Development y las rutas /api devuelven 503 explicativo. Compilar no necesita una base configurada. NuGet requiere Internet.

## Contenido implementado
Solucion y proyectos API/Contracts/Checks. 26 entidades EF y mapeo de claves, relaciones e indices. Registro comprador/vendedor, login JWT, perfil, bootstrap administrador, lista/aprobacion/rechazo de vendedores, catalogos, crear propiedad/publicacion de alquiler y listado publico paginado. Solo endpoint publico expone campos elegidos, no hashes ni cedulas.
Checks es un ejecutable de comprobaciones de reglas, NO un proyecto xUnit: dotnet test no lo ejecuta.
Las pruebas HTTP/SQL estan en tests/Inmobiliaria.IntegrationTests y usan SQL Server LocalDB en Windows. Ejecuta ./tests/ejecutar-integracion.ps1 para preparar la instancia y probar; dotnet test Inmobiliaria.sln ejecuta ambas suites. Cada ejecucion crea y elimina su propia base temporal. Consulta tests/Inmobiliaria.IntegrationTests/README.md. La validacion contra Azure sigue pendiente.
Las pruebas unitarias de los cuatro servicios estan en tests/Inmobiliaria.Application.UnitTests, con xUnit y Moq. Se ejecutan con dotnet test Inmobiliaria.sln y usan repositorios simulados, sin conexion a SQL ni secretos. Cubren permisos, registro/login, decisiones administrativas, propiedades y publicaciones; no sustituyen las pruebas de integracion SQL/HTTP.
Blazor incluye registro/login, catalogo, creacion/edicion de propiedades, publicacion de alquileres y envio/consulta de solicitudes. La administracion sigue reservada a Windows Forms; por ahora la aprobacion se prueba mediante API. Escritorio y Android siguen pendientes.
Imagenes: hasta 8 por propiedad/publicacion, carga JPG/PNG/WebP y galeria en el catalogo. Consulta docs/imagenes.md para limites y almacenamiento local.
Consulta docs/recorrido-local.md para preparar una base LocalDB separada, iniciar API/Web y reproducir el recorrido completo. scripts/recorrido-local.ps1 incluye preparacion, arranque y aprobacion administrativa por API.

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
