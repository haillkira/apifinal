# Recorrido de alquiler

El recorrido termina cuando el comprador envia una solicitud pendiente y el vendedor puede consultarla. Aceptar/rechazar solicitudes, visitas, reservas, pagos y contratos son etapas posteriores. La administracion permanece reservada a Windows Forms; mientras se desarrolla ese cliente, la aprobacion se ejecuta mediante la API.

## Preparacion local

Requiere .NET 10, SQL Server Express (instancia SQLEXPRESS) y sqlcmd. El lanzador CMD funciona con Windows PowerShell y habilita scripts solo para su proceso. Desde la raiz:

```powershell
.\recorrido-local.cmd -Modo Preparar
```

Crea exclusivamente `Inmobiliaria_RecorridoLocal` en `.\SQLEXPRESS`, aplica los scripts SQL y pide una contraseña de administrador de al menos 16 caracteres. El correo predeterminado es `admin@demo.local`. Usa datos ficticios. No cambia los secretos existentes de la API ni la base Azure. Si la base ya existe, conserva los datos y no vuelve a crear el administrador. Si falla la preparacion parcial, conserva la base para diagnostico y no la borra automaticamente.

En dos terminales distintas:

```powershell
.\recorrido-local.cmd -Modo Api
```

```powershell
.\recorrido-local.cmd -Modo Web
```

Abre http://localhost:5175. La API utiliza http://localhost:5180. Los puertos deben estar disponibles. El script Api genera una clave JWT temporal al iniciar: reiniciar la API requiere volver a iniciar sesion. La sesion Blazor vive en el circuito actual y tampoco se conserva al recargar completamente la pagina; utiliza los enlaces internos para navegar.

## Demostracion

1. En **Quiero publicar**, registra al vendedor con correo y cedula ficticios unicos. Inicia sesion: **Mis propiedades** explica que requiere aprobacion.
2. En otra terminal, lista los vendedores pendientes:

   ```powershell
   .\recorrido-local.cmd -Modo Aprobar
   ```

   Introduce la contraseña administrativa. Repite con `-IdVendedor NUMERO` usando el ID devuelto para aprobar exclusivamente esa cuenta.
3. Como vendedor, pulsa **Actualizar listado**, luego **Crear propiedad**. Completa tipo, provincia, direccion y caracteristicas. Guarda. Puedes editarla desde el listado.
4. Pulsa **Publicar alquiler**, indica precio mensual y moneda; deposito y mantenimiento son opcionales. Publica y abre el catalogo.
5. Cierra sesion. Registra un comprador e inicia sesion. En el catalogo selecciona **Solicitar alquiler**, escribe un mensaje opcional y confirma el envio.
6. Abre **Mis solicitudes**: aparece en estado **Pendiente**. Un segundo envio activo para la misma publicacion se rechaza.
7. Cierra sesion y entra como el vendedor. En **Solicitudes recibidas** veras la propiedad, comprador, mensaje y estado. Otros compradores y vendedores no ven esa solicitud.

## Verificacion

```powershell
dotnet build Inmobiliaria.sln
dotnet test Inmobiliaria.sln
```

`RecorridoTests` reproduce registro, rechazo del vendedor pendiente, aprobacion administrativa, creacion/publicacion, consulta del catalogo y envio concurrente de dos solicitudes iguales (solo una se guarda). Verifica la visibilidad de comprador/vendedor, el aislamiento de otro comprador y el rechazo de publicaciones pausadas contra SQL real temporal. `SolicitudesServiceTests` verifica autorizacion, normalizacion, errores y filtros por usuario.

`Inmobiliaria.Web.Tests` usa bUnit para probar los formularios de creacion/publicacion y el envio de solicitudes desde el catalogo con HTTP simulado. Esto verifica los componentes; no sustituye una prueba visual en navegador.
