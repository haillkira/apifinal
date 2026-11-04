# Verificacion del paquete inicial - 4 noviembre 2026
Entorno de validacion: Linux x64, SDK .NET 10.0.100. Usuario trabajara en Windows con su SDK .NET 10.
- dotnet build: completado, 0 advertencias y 0 errores.
- Checks ejecutable: tipos permitidos, rechazo Venta/moneda/importe incorrectos, modelo EF 26 entidades y generacion de DDL sin conexion: correctos.
- Proceso API en Development: GET / y /health devolvieron 200.
- GET /api/v1/catalogos/tipos sin conexion configurada: devolvio 503 con ProblemDetails y explicacion.
- SQL reconstruido no ejecutado contra Azure. Login/aprobaciones/publicaciones requieren probar con base real; compilacion no demuestra su funcionamiento integral.
- No se genero APK ni se compilaron clientes: estan pendientes.
- Se fijo System.Security.Cryptography.Pkcs 10.0.13 por avisos transitivos de la rama 9.0.11. No se suprimieron avisos.
- Fuentes de correccion: https://github.com/advisories/GHSA-846f-f764-76mx y https://github.com/advisories/GHSA-2fmv-rv2c-59x7
No se incluyen bin/obj ni credenciales en el ZIP; el destinatario debe restaurar NuGet al compilar.
