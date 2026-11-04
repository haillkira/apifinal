# Alcance confirmado v1.1
Equipo: Luilton y Yerferson Rosario Peña. Plataforma de alquiler de apartamentos y casas en Republica Dominicana. Ventas para version futura.
Azure SQL Database InmobiliariaDB, API .NET 10 en VS Code, EF Core SQL Server y App Service.
Android Kotlin y Blazor para comprador/vendedor; Windows Forms como unico cliente administrativo. Todos consumen API HTTPS y ninguno accede directamente a SQL.
Esquema previsto: Roles, Usuarios, Compradores, Vendedores, Administradores, TiposPropiedad, Provincias, Caracteristicas, Propiedades, PropiedadCaracteristicas, Publicaciones, ImagenesPropiedades, Favoritos, BusquedasGuardadas, Solicitudes, Visitas, Reservaciones, Conversaciones, Mensajes, Reportes, HistorialAprobaciones, Notificaciones, Pagos, Contratos, Suspensiones e HistorialAcciones.
Reglas: vendedor aprobado/activo para publicar, comprador no publica, dueño modifica sus recursos, solo participantes leen mensajes, administrador autoriza segun nivel, suspensiones bloquean acceso efectivo. Solo Alquiler en anuncios, Informacion/Alquiler en solicitudes y Alquiler en contratos. Mantener compatibilidad de valores Venta/Compra para futuro sin habilitarlos en interfaz o API.
Datos fisicos en Propiedades, oferta en Publicaciones. Dinero decimal exacto, moneda DOP/USD, UTC tecnica, archivos sensibles privados. Ninguna contraseña SQL/JWT/keystore en repositorio.
Pagos academicos registrados/simulados, no cobros con tarjeta. Formato de cedula no es verificacion real de identidad.
Este ZIP implementa exclusivamente lo listado en README. Las tablas adicionales no implican endpoints terminados. El SQL se reconstruyo del inventario; no fue contrastado con el original ni ejecutado contra Azure.
