SET XACT_ABORT ON;
IF DB_NAME() <> N'InmobiliariaDB' THROW 50001, 'Selecciona InmobiliariaDB.', 1;
BEGIN TRANSACTION;
INSERT INTO dbo.Roles(Nombre,Descripcion) SELECT v.Nombre,v.Nombre FROM (VALUES(N'Administrador'),(N'Vendedor'),(N'Comprador')) v(Nombre) WHERE NOT EXISTS(SELECT 1 FROM dbo.Roles r WHERE r.Nombre=v.Nombre);
INSERT INTO dbo.TiposPropiedad(Nombre,Descripcion) SELECT v.Nombre,v.Nombre FROM (VALUES(N'Apartamento'),(N'Casa'),(N'Penthouse'),(N'Villa'),(N'Estudio'),(N'Habitacion'),(N'Local comercial'),(N'Terreno')) v(Nombre) WHERE NOT EXISTS(SELECT 1 FROM dbo.TiposPropiedad t WHERE t.Nombre=v.Nombre);
COMMIT;

INSERT INTO dbo.Provincias(Nombre) SELECT v.Nombre FROM (VALUES(N'Azua'),(N'Bahoruco'),(N'Barahona'),(N'Dajabon'),(N'Distrito Nacional'),(N'Duarte'),(N'El Seibo'),(N'Elias Piña'),(N'Espaillat'),(N'Hato Mayor'),(N'Hermanas Mirabal'),(N'Independencia'),(N'La Altagracia'),(N'La Romana'),(N'La Vega'),(N'Maria Trinidad Sanchez'),(N'Monseñor Nouel'),(N'Monte Cristi'),(N'Monte Plata'),(N'Pedernales'),(N'Peravia'),(N'Puerto Plata'),(N'Samana'),(N'San Cristobal'),(N'San Jose de Ocoa'),(N'San Juan'),(N'San Pedro de Macoris'),(N'Sanchez Ramirez'),(N'Santiago'),(N'Santiago Rodriguez'),(N'Santo Domingo'),(N'Valverde')) v(Nombre) WHERE NOT EXISTS(SELECT 1 FROM dbo.Provincias p WHERE p.Nombre=v.Nombre);
