# Arranque compartido con Azure SQL

Requisitos: Windows, .NET SDK 10 y acceso a la base Azure SQL del equipo. Agregar la IP publica de cada integrante al firewall del servidor Azure, y compartir credenciales por un canal privado. Los secretos de usuario no se incluyen al copiar el proyecto ni se suben a Git.

En la base Azure EXISTENTE, revisar y ejecutar database/004_archivado.sql mediante SSMS antes de usar esta version. Es aditivo e idempotente. No ejecutar 001_esquema_reconstruido.sql contra una base con datos. La configuracion verifica la columna EliminadoEn, pero la compatibilidad completa del esquema requiere ejecutar los flujos contra Azure.

Cada integrante ejecuta desde la raiz:

```powershell
.\recorrido-azure.cmd -Modo Configurar
```

Pegar la cadena Azure SQL cuando se solicite (entrada oculta). El script exige cifrado, valida certificado y conexion y guarda la cadena y una clave JWT local en user-secrets. No modifica tablas de Azure automaticamente. Esta configuracion reemplaza la conexion API guardada para ese usuario; recorrido-local.cmd sigue disponible para la demostracion local aislada.

En dos terminales:

```powershell
.\recorrido-azure.cmd -Modo Api
```

```powershell
.\recorrido-azure.cmd -Modo Web
```

Abrir http://localhost:5175. Ambos integrantes usaran los mismos registros Azure. Cada API emite sus propios tokens; iniciar sesion desde la API que se este utilizando. El administrador local de la demostracion no se copia a Azure; usar una cuenta administrativa existente en Azure.

Las fotos siguen siendo archivos locales en api/Inmobiliaria.Api/App_Data/imagenes. Compartir SQL no comparte esos archivos: para ver las mismas fotos ambos deben consumir una API comun con almacenamiento persistente, o implementar Azure Blob Storage antes del despliegue compartido. No subir fotos ni secretos al repositorio.

Las pruebas automaticas conservan bases temporales en SQLEXPRESS; no ejecutarlas contra la base compartida Azure.
