# Pendientes para producto completo
1. Validar SQL en SQL Server/Azure y contrastar con el archivo original. Pruebas integrales y concurrencia.
2. Triggers notificaciones de vendedores/reportes, CHECK restantes y coherencia de roles/perfiles.
3. CRUD completo, imagenes y Blob Storage, favoritos, busquedas, solicitudes y visitas.
4. Reservas sin solapamientos, pagos registrados, contratos, mensajeria y reportes.
5. Panel Forms, cliente Blazor, Android y APK firmado. Esta entrega no implementa esos clientes.
6. Suspension/reactivacion administrativa, endpoints de auditoria y matriz completa de permisos.
7. Registro/login: rate limit especifico, recuperacion, bloqueo, verificacion, revocacion, normalizacion robusta y auditoria.
8. Unicidad de documento entre perfiles requiere pruebas concurrentes y/o restriccion de identidad central revisada.
9. Publicaciones concurrentes: definir anuncios activos por propiedad y restricciones antes de produccion.
10. CI de equipo, PR y revision humana. No se incluyen commits inventados.
11. Swagger/Scalar/OpenAPI no incluidos aun; peticiones .http incluidas para probar sin esa dependencia.
