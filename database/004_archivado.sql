-- Actualizacion aditiva e idempotente. No elimina datos existentes.
SET XACT_ABORT ON;
IF OBJECT_ID(N'dbo.Propiedades', N'U') IS NULL THROW 50003, 'Falta el esquema de propiedades.', 1;
IF COL_LENGTH('dbo.Propiedades', 'EliminadoEn') IS NULL
    ALTER TABLE dbo.Propiedades ADD EliminadoEn datetime2(7) NULL;
GO
CREATE OR ALTER VIEW dbo.Vista_PublicacionesDisponibles AS
SELECT p.*, v.IdVendedor FROM dbo.Publicaciones p
JOIN dbo.Propiedades i ON i.IdPropiedad=p.IdPropiedad
JOIN dbo.Vendedores v ON v.IdVendedor=i.IdVendedor
JOIN dbo.Usuarios u ON u.IdUsuario=v.IdUsuario
WHERE p.TipoOperacion='Alquiler' AND p.EstadoPublicacion='Publicada'
AND i.EstadoPropiedad='Disponible' AND i.EliminadoEn IS NULL
AND v.EstadoAprobacion='Aprobado' AND v.CuentaAprobada=1 AND u.EstadoCuenta='Activa';
