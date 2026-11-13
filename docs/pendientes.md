# Pendientes para producto completo
1. Validar SQL en SQL Server/Azure y contrastar con el archivo original. Pruebas integrales y concurrencia.
2. Triggers notificaciones de vendedores/reportes, CHECK restantes y coherencia de roles/perfiles.
3. CRUD de propiedades y envio/consulta de solicitudes implementados. Pendientes: decisiones sobre solicitudes, imagenes y Blob Storage, favoritos, busquedas y visitas.
4. Reservas sin solapamientos, pagos registrados, contratos, mensajeria y reportes.
5. Panel Forms, Android y APK firmado pendientes. Blazor ya permite registro/login, catalogo, creacion/edicion, publicacion y envio/consulta de solicitudes.
6. Suspension/reactivacion administrativa, endpoints de auditoria y matriz completa de permisos.
7. Registro/login: rate limit especifico, recuperacion, bloqueo, verificacion, revocacion, normalizacion robusta y auditoria.
8. Unicidad de documento entre perfiles requiere pruebas concurrentes y/o restriccion de identidad central revisada.
9. Publicaciones concurrentes: definir anuncios activos por propiedad y restricciones antes de produccion.
10. CI de equipo, PR y revision humana. No se incluyen commits inventados.
11. OpenAPI/Scalar disponibles en Development; ampliar documentacion y pruebas al agregar nuevos endpoints.
