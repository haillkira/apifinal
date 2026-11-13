# Integracion HTTP y SQL local

Requisitos: Windows, SDK .NET 10 y SQL Server Express (instancia SQLEXPRESS) y sqlcmd. En este equipo SQLEXPRESS esta disponible.

Desde la raiz del repositorio:

```powershell
./tests/ejecutar-integracion.ps1
```

Para ejecutar todas las pruebas unitarias y de integracion:

```powershell
dotnet test Inmobiliaria.sln
```

La fixture usa SQL Server real en `.\SQLEXPRESS`, con autenticacion integrada de Windows. Genera una base exclusiva `Inmobiliaria_Integration_<GUID>`, aplica los tres scripts de database en orden y elimina esa base al terminar. Solo adapta en memoria la comprobacion del nombre de base que contienen los scripts. No usa Azure ni la conexion SQL guardada en los secretos de la API. La clave JWT se genera para cada servidor de pruebas; el administrador y su password fijo son datos de prueba exclusivos de la base temporal.

Las peticiones HTTP pasan por WebApplicationFactory/TestServer dentro del proceso: controllers, validacion, middleware, JWT, servicios, repositorios EF Core y SQL reales. No requieren levantar la API en un puerto ni simulan servicios o repositorios. No verifican TLS, proxy ni despliegue en Azure.

Los seis escenarios comprueban registro/login/perfil y duplicados; permisos y validacion; CRUD y aislamiento entre vendedores; aprobacion con historial y notificacion; publicaciones y precio decimal; bloqueo de borrado con publicaciones; restricciones SQL de unicidad y clave foranea; y rollback de transacciones. Algunos escenarios contienen varias operaciones y aserciones.

Las pruebas comparten una base por ejecucion, crean usuarios distintos y se ejecutan sin paralelismo dentro de la coleccion. Conservan el limitador HTTP real de la API. No se omiten silenciosamente si SQL no esta disponible: fallan con el error de conexion.

Al finalizar normalmente se elimina la base, incluso si falla una prueba. Una terminacion forzada del proceso puede dejar una base temporal; revisar su nombre y que no exista una ejecucion activa antes de eliminarla manualmente. No ejecutar los scripts de esquema contra una base con datos existentes.
