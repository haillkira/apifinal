-- SSMS: GO separa los lotes.
CREATE OR ALTER VIEW dbo.Vista_PublicacionesDisponibles AS
SELECT p.*, v.IdVendedor FROM dbo.Publicaciones p JOIN dbo.Propiedades i ON i.IdPropiedad=p.IdPropiedad JOIN dbo.Vendedores v ON v.IdVendedor=i.IdVendedor JOIN dbo.Usuarios u ON u.IdUsuario=v.IdUsuario
WHERE p.TipoOperacion='Alquiler' AND p.EstadoPublicacion='Publicada' AND i.EstadoPropiedad='Disponible' AND v.EstadoAprobacion='Aprobado' AND v.CuentaAprobada=1 AND u.EstadoCuenta='Activa';
GO
CREATE OR ALTER VIEW dbo.Vista_VendedoresPendientes AS
SELECT v.*,u.Nombre,u.Apellido,u.Correo FROM dbo.Vendedores v JOIN dbo.Usuarios u ON u.IdUsuario=v.IdUsuario WHERE v.EstadoAprobacion='Pendiente';
GO
CREATE OR ALTER VIEW dbo.Vista_ReportesPendientes AS
SELECT * FROM dbo.Reportes WHERE EstadoReporte IN ('Pendiente','EnRevision');
GO
CREATE OR ALTER VIEW dbo.Vista_SolicitudesVendedor AS
SELECT s.*,i.IdVendedor FROM dbo.Solicitudes s JOIN dbo.Publicaciones p ON p.IdPublicacion=s.IdPublicacion JOIN dbo.Propiedades i ON i.IdPropiedad=p.IdPropiedad;
GO
