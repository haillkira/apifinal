-- RECONSTRUIDO desde el inventario; no es el SQL original. Revision y pruebas SQL pendientes.
-- Ejecutar en SSMS conectado a la base de desarrollo InmobiliariaDB vacia.
SET XACT_ABORT ON;
IF DB_NAME() <> N'InmobiliariaDB' THROW 50001, 'Selecciona InmobiliariaDB.', 1;
IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0) THROW 50002, 'La base debe estar vacia; no se sobrescriben tablas.', 1;
BEGIN TRANSACTION;
CREATE TABLE dbo.[Roles] (
    [IdRol] int IDENTITY(1,1) NOT NULL,
    [Nombre] nvarchar(50) NOT NULL,
    [Descripcion] nvarchar(500) NULL,
    CONSTRAINT PK_Roles PRIMARY KEY ([IdRol])
);
CREATE TABLE dbo.[Usuarios] (
    [IdUsuario] int IDENTITY(1,1) NOT NULL,
    [IdRol] int NOT NULL,
    [Nombre] nvarchar(100) NOT NULL,
    [Apellido] nvarchar(100) NOT NULL,
    [Correo] nvarchar(254) NOT NULL,
    [PasswordHash] nvarchar(1000) NOT NULL,
    [Telefono] nvarchar(30) NOT NULL,
    [FotoPerfilUrl] nvarchar(1000) NULL,
    [EstadoCuenta] nvarchar(20) NOT NULL,
    [CorreoVerificado] bit NOT NULL,
    [FechaRegistro] datetime2(7) NOT NULL,
    [UltimoAcceso] datetime2(7) NULL,
    [ActualizadoEn] datetime2(7) NULL,
    CONSTRAINT PK_Usuarios PRIMARY KEY ([IdUsuario]),
    CONSTRAINT CK_Usuarios_1 CHECK (EstadoCuenta IN ('Activa','Pendiente','Suspendida','Bloqueada','Cerrada'))
);
CREATE TABLE dbo.[Compradores] (
    [IdComprador] int IDENTITY(1,1) NOT NULL,
    [IdUsuario] int NOT NULL,
    [Cedula] nvarchar(20) NOT NULL,
    [CuentaAprobada] bit NOT NULL,
    [FechaVerificacion] datetime2(7) NULL,
    [PreferenciaContacto] nvarchar(30) NULL,
    CONSTRAINT PK_Compradores PRIMARY KEY ([IdComprador])
);
CREATE TABLE dbo.[Vendedores] (
    [IdVendedor] int IDENTITY(1,1) NOT NULL,
    [IdUsuario] int NOT NULL,
    [Cedula] nvarchar(20) NOT NULL,
    [TipoVendedor] nvarchar(30) NOT NULL,
    [NombreComercial] nvarchar(150) NULL,
    [NumeroLicencia] nvarchar(100) NULL,
    [CuentaAprobada] bit NOT NULL,
    [EstadoAprobacion] nvarchar(20) NOT NULL,
    [FechaSolicitud] datetime2(7) NOT NULL,
    [FechaAprobacion] datetime2(7) NULL,
    [MotivoRechazo] nvarchar(1000) NULL,
    [VerificadoPor] int NULL,
    CONSTRAINT PK_Vendedores PRIMARY KEY ([IdVendedor]),
    CONSTRAINT CK_Vendedores_1 CHECK (EstadoAprobacion IN ('Pendiente','Aprobado','Rechazado')),
    CONSTRAINT CK_Vendedores_2 CHECK (TipoVendedor IN ('Particular','Inmobiliaria','Agente')),
    CONSTRAINT CK_Vendedores_3 CHECK ((EstadoAprobacion = 'Aprobado' AND CuentaAprobada = 1 AND FechaAprobacion IS NOT NULL) OR (EstadoAprobacion <> 'Aprobado' AND CuentaAprobada = 0))
);
CREATE TABLE dbo.[Administradores] (
    [IdAdministrador] int IDENTITY(1,1) NOT NULL,
    [IdUsuario] int NOT NULL,
    [NivelPermiso] nvarchar(20) NOT NULL,
    CONSTRAINT PK_Administradores PRIMARY KEY ([IdAdministrador]),
    CONSTRAINT CK_Administradores_1 CHECK (NivelPermiso IN ('Completo','Moderador','Soporte'))
);
CREATE TABLE dbo.[TiposPropiedad] (
    [IdTipoPropiedad] int IDENTITY(1,1) NOT NULL,
    [Nombre] nvarchar(50) NOT NULL,
    [Descripcion] nvarchar(500) NULL,
    CONSTRAINT PK_TiposPropiedad PRIMARY KEY ([IdTipoPropiedad])
);
CREATE TABLE dbo.[Provincias] (
    [IdProvincia] int IDENTITY(1,1) NOT NULL,
    [Nombre] nvarchar(100) NOT NULL,
    CONSTRAINT PK_Provincias PRIMARY KEY ([IdProvincia])
);
CREATE TABLE dbo.[Caracteristicas] (
    [IdCaracteristica] int IDENTITY(1,1) NOT NULL,
    [Nombre] nvarchar(100) NOT NULL,
    [Categoria] nvarchar(50) NOT NULL,
    CONSTRAINT PK_Caracteristicas PRIMARY KEY ([IdCaracteristica])
);
CREATE TABLE dbo.[Propiedades] (
    [IdPropiedad] int IDENTITY(1,1) NOT NULL,
    [IdVendedor] int NOT NULL,
    [IdTipoPropiedad] int NOT NULL,
    [Titulo] nvarchar(150) NOT NULL,
    [Descripcion] nvarchar(4000) NOT NULL,
    [IdProvincia] int NOT NULL,
    [Ciudad] nvarchar(100) NOT NULL,
    [Sector] nvarchar(100) NOT NULL,
    [Direccion] nvarchar(300) NOT NULL,
    [ReferenciaDireccion] nvarchar(500) NULL,
    [Latitud] decimal(10,7) NULL,
    [Longitud] decimal(10,7) NULL,
    [Habitaciones] int NOT NULL,
    [Banos] int NOT NULL,
    [Parqueos] int NOT NULL,
    [MetrosCuadrados] decimal(18,2) NOT NULL,
    [Amueblado] bit NOT NULL,
    [TienePiscina] bit NOT NULL,
    [TieneAscensor] bit NOT NULL,
    [EstadoPropiedad] nvarchar(20) NOT NULL,
    [FechaCreacion] datetime2(7) NOT NULL,
    [ActualizadoEn] datetime2(7) NULL,
    CONSTRAINT PK_Propiedades PRIMARY KEY ([IdPropiedad]),
    CONSTRAINT CK_Propiedades_1 CHECK (Habitaciones >= 0 AND Banos >= 0 AND Parqueos >= 0 AND MetrosCuadrados > 0),
    CONSTRAINT CK_Propiedades_2 CHECK (EstadoPropiedad IN ('Disponible','Ocupada','Vendida','Alquilada','Mantenimiento')),
    CONSTRAINT CK_Propiedades_3 CHECK (Latitud BETWEEN -90 AND 90),
    CONSTRAINT CK_Propiedades_4 CHECK (Longitud BETWEEN -180 AND 180)
);
CREATE TABLE dbo.[PropiedadCaracteristicas] (
    [IdPropiedad] int NOT NULL,
    [IdCaracteristica] int NOT NULL,
    [Valor] nvarchar(200) NULL,
    CONSTRAINT PK_PropiedadCaracteristicas PRIMARY KEY ([IdPropiedad], [IdCaracteristica])
);
CREATE TABLE dbo.[Publicaciones] (
    [IdPublicacion] int IDENTITY(1,1) NOT NULL,
    [IdPropiedad] int NOT NULL,
    [TipoOperacion] nvarchar(20) NOT NULL,
    [Precio] decimal(18,2) NOT NULL,
    [Moneda] nvarchar(3) NOT NULL,
    [Deposito] decimal(18,2) NULL,
    [MantenimientoMensual] decimal(18,2) NULL,
    [EstadoPublicacion] nvarchar(20) NOT NULL,
    [FechaCreacion] datetime2(7) NOT NULL,
    [FechaPublicacion] datetime2(7) NULL,
    [FechaExpiracion] datetime2(7) NULL,
    [MotivoRechazo] nvarchar(1000) NULL,
    [Destacada] bit NOT NULL,
    CONSTRAINT PK_Publicaciones PRIMARY KEY ([IdPublicacion]),
    CONSTRAINT CK_Publicaciones_1 CHECK (TipoOperacion IN ('Alquiler','Venta')),
    CONSTRAINT CK_Publicaciones_2 CHECK (Moneda IN ('DOP','USD')),
    CONSTRAINT CK_Publicaciones_3 CHECK (Precio > 0 AND (Deposito IS NULL OR Deposito >= 0) AND (MantenimientoMensual IS NULL OR MantenimientoMensual >= 0)),
    CONSTRAINT CK_Publicaciones_4 CHECK (EstadoPublicacion IN ('Borrador','Pendiente','Publicada','Pausada','Rechazada','Finalizada'))
);
CREATE TABLE dbo.[ImagenesPropiedades] (
    [IdImagen] int IDENTITY(1,1) NOT NULL,
    [IdPropiedad] int NOT NULL,
    [UrlImagen] nvarchar(1000) NOT NULL,
    [EsPrincipal] bit NOT NULL,
    [Orden] int NOT NULL,
    [TextoAlternativo] nvarchar(300) NULL,
    [FechaSubida] datetime2(7) NOT NULL,
    CONSTRAINT PK_ImagenesPropiedades PRIMARY KEY ([IdImagen])
);
CREATE TABLE dbo.[Favoritos] (
    [IdComprador] int NOT NULL,
    [IdPublicacion] int NOT NULL,
    [FechaAgregado] datetime2(7) NOT NULL,
    CONSTRAINT PK_Favoritos PRIMARY KEY ([IdComprador], [IdPublicacion])
);
CREATE TABLE dbo.[BusquedasGuardadas] (
    [IdBusqueda] int IDENTITY(1,1) NOT NULL,
    [IdComprador] int NOT NULL,
    [NombreBusqueda] nvarchar(100) NOT NULL,
    [FiltrosJson] nvarchar(4000) NOT NULL,
    [NotificarNuevosResultados] bit NOT NULL,
    [FechaCreacion] datetime2(7) NOT NULL,
    CONSTRAINT PK_BusquedasGuardadas PRIMARY KEY ([IdBusqueda])
);
CREATE TABLE dbo.[Solicitudes] (
    [IdSolicitud] int IDENTITY(1,1) NOT NULL,
    [IdComprador] int NOT NULL,
    [IdPublicacion] int NOT NULL,
    [TipoSolicitud] nvarchar(20) NOT NULL,
    [Mensaje] nvarchar(2000) NULL,
    [EstadoSolicitud] nvarchar(20) NOT NULL,
    [FechaSolicitud] datetime2(7) NOT NULL,
    [FechaRespuesta] datetime2(7) NULL,
    CONSTRAINT PK_Solicitudes PRIMARY KEY ([IdSolicitud]),
    CONSTRAINT CK_Solicitudes_1 CHECK (TipoSolicitud IN ('Informacion','Alquiler','Compra')),
    CONSTRAINT CK_Solicitudes_2 CHECK (EstadoSolicitud IN ('Pendiente','EnRevision','Aceptada','Rechazada','Cancelada','Completada'))
);
CREATE TABLE dbo.[Visitas] (
    [IdVisita] int IDENTITY(1,1) NOT NULL,
    [IdSolicitud] int NOT NULL,
    [FechaVisita] datetime2(7) NOT NULL,
    [EstadoVisita] nvarchar(20) NOT NULL,
    [Notas] nvarchar(1000) NULL,
    [FechaCreacion] datetime2(7) NOT NULL,
    CONSTRAINT PK_Visitas PRIMARY KEY ([IdVisita]),
    CONSTRAINT CK_Visitas_1 CHECK (EstadoVisita IN ('Solicitada','Confirmada','Reprogramar','Realizada','Cancelada','NoAsistio'))
);
CREATE TABLE dbo.[Reservaciones] (
    [IdReservacion] int IDENTITY(1,1) NOT NULL,
    [IdSolicitud] int NOT NULL,
    [MontoReserva] decimal(18,2) NOT NULL,
    [FechaInicio] datetime2(7) NOT NULL,
    [FechaFin] datetime2(7) NOT NULL,
    [EstadoReservacion] nvarchar(20) NOT NULL,
    [FechaCreacion] datetime2(7) NOT NULL,
    CONSTRAINT PK_Reservaciones PRIMARY KEY ([IdReservacion]),
    CONSTRAINT CK_Reservaciones_1 CHECK (MontoReserva >= 0 AND FechaFin > FechaInicio),
    CONSTRAINT CK_Reservaciones_2 CHECK (EstadoReservacion IN ('Pendiente','Confirmada','Cancelada','Expirada','Convertida'))
);
CREATE TABLE dbo.[Conversaciones] (
    [IdConversacion] int IDENTITY(1,1) NOT NULL,
    [IdComprador] int NOT NULL,
    [IdVendedor] int NOT NULL,
    [IdPublicacion] int NULL,
    [FechaCreacion] datetime2(7) NOT NULL,
    [UltimoMensajeEn] datetime2(7) NULL,
    CONSTRAINT PK_Conversaciones PRIMARY KEY ([IdConversacion])
);
CREATE TABLE dbo.[Mensajes] (
    [IdMensaje] int IDENTITY(1,1) NOT NULL,
    [IdConversacion] int NOT NULL,
    [IdRemitente] int NOT NULL,
    [Contenido] nvarchar(4000) NOT NULL,
    [FechaEnvio] datetime2(7) NOT NULL,
    [Leido] bit NOT NULL,
    [FechaLectura] datetime2(7) NULL,
    CONSTRAINT PK_Mensajes PRIMARY KEY ([IdMensaje])
);
CREATE TABLE dbo.[Reportes] (
    [IdReporte] int IDENTITY(1,1) NOT NULL,
    [IdUsuarioReporta] int NOT NULL,
    [IdPublicacion] int NULL,
    [IdUsuarioReportado] int NULL,
    [Categoria] nvarchar(40) NOT NULL,
    [Descripcion] nvarchar(2000) NOT NULL,
    [EstadoReporte] nvarchar(20) NOT NULL,
    [FechaReporte] datetime2(7) NOT NULL,
    [FechaResolucion] datetime2(7) NULL,
    [ResueltoPor] int NULL,
    [Resolucion] nvarchar(2000) NULL,
    CONSTRAINT PK_Reportes PRIMARY KEY ([IdReporte]),
    CONSTRAINT CK_Reportes_1 CHECK (IdPublicacion IS NOT NULL OR IdUsuarioReportado IS NOT NULL),
    CONSTRAINT CK_Reportes_2 CHECK (EstadoReporte IN ('Pendiente','EnRevision','Resuelto','Rechazado'))
);
CREATE TABLE dbo.[HistorialAprobaciones] (
    [IdHistorial] int IDENTITY(1,1) NOT NULL,
    [IdVendedor] int NOT NULL,
    [IdAdministrador] int NOT NULL,
    [Accion] nvarchar(20) NOT NULL,
    [Comentario] nvarchar(1000) NULL,
    [FechaAccion] datetime2(7) NOT NULL,
    CONSTRAINT PK_HistorialAprobaciones PRIMARY KEY ([IdHistorial])
);
CREATE TABLE dbo.[Notificaciones] (
    [IdNotificacion] int IDENTITY(1,1) NOT NULL,
    [IdUsuarioDestino] int NOT NULL,
    [TipoNotificacion] nvarchar(40) NOT NULL,
    [Titulo] nvarchar(150) NOT NULL,
    [Mensaje] nvarchar(2000) NOT NULL,
    [UrlDestino] nvarchar(1000) NULL,
    [Leida] bit NOT NULL,
    [FechaCreacion] datetime2(7) NOT NULL,
    [FechaLectura] datetime2(7) NULL,
    CONSTRAINT PK_Notificaciones PRIMARY KEY ([IdNotificacion])
);
CREATE TABLE dbo.[Pagos] (
    [IdPago] int IDENTITY(1,1) NOT NULL,
    [IdComprador] int NOT NULL,
    [IdSolicitud] int NULL,
    [IdReservacion] int NULL,
    [Monto] decimal(18,2) NOT NULL,
    [Moneda] nvarchar(3) NOT NULL,
    [Concepto] nvarchar(200) NOT NULL,
    [MetodoPago] nvarchar(30) NOT NULL,
    [ReferenciaExterna] nvarchar(200) NULL,
    [EstadoPago] nvarchar(20) NOT NULL,
    [FechaPago] datetime2(7) NULL,
    [FechaRegistro] datetime2(7) NOT NULL,
    CONSTRAINT PK_Pagos PRIMARY KEY ([IdPago]),
    CONSTRAINT CK_Pagos_1 CHECK (Monto > 0 AND Moneda IN ('DOP','USD')),
    CONSTRAINT CK_Pagos_2 CHECK (EstadoPago IN ('Pendiente','Procesando','Pagado','Rechazado','Reembolsado','Cancelado'))
);
CREATE TABLE dbo.[Contratos] (
    [IdContrato] int IDENTITY(1,1) NOT NULL,
    [IdSolicitud] int NOT NULL,
    [TipoContrato] nvarchar(20) NOT NULL,
    [NumeroContrato] nvarchar(100) NOT NULL,
    [FechaInicio] datetime2(7) NOT NULL,
    [FechaFin] datetime2(7) NULL,
    [MontoAcordado] decimal(18,2) NOT NULL,
    [Moneda] nvarchar(3) NOT NULL,
    [DocumentoUrl] nvarchar(1000) NULL,
    [EstadoContrato] nvarchar(20) NOT NULL,
    [FechaCreacion] datetime2(7) NOT NULL,
    CONSTRAINT PK_Contratos PRIMARY KEY ([IdContrato]),
    CONSTRAINT CK_Contratos_1 CHECK (MontoAcordado > 0 AND Moneda IN ('DOP','USD')),
    CONSTRAINT CK_Contratos_2 CHECK (TipoContrato IN ('Alquiler','Compraventa')),
    CONSTRAINT CK_Contratos_3 CHECK (FechaFin IS NULL OR FechaFin > FechaInicio)
);
CREATE TABLE dbo.[Suspensiones] (
    [IdSuspension] int IDENTITY(1,1) NOT NULL,
    [IdUsuario] int NOT NULL,
    [IdAdministrador] int NOT NULL,
    [Motivo] nvarchar(1000) NOT NULL,
    [FechaInicio] datetime2(7) NOT NULL,
    [FechaFin] datetime2(7) NULL,
    [Activa] bit NOT NULL,
    CONSTRAINT PK_Suspensiones PRIMARY KEY ([IdSuspension]),
    CONSTRAINT CK_Suspensiones_1 CHECK (FechaFin IS NULL OR FechaFin > FechaInicio)
);
CREATE TABLE dbo.[HistorialAcciones] (
    [IdAccion] int IDENTITY(1,1) NOT NULL,
    [IdUsuario] int NULL,
    [Accion] nvarchar(100) NOT NULL,
    [TablaAfectada] nvarchar(100) NOT NULL,
    [IdRegistro] int NULL,
    [Detalles] nvarchar(4000) NULL,
    [DireccionIP] nvarchar(64) NULL,
    [FechaAccion] datetime2(7) NOT NULL,
    CONSTRAINT PK_HistorialAcciones PRIMARY KEY ([IdAccion])
);
ALTER TABLE dbo.[Usuarios] ADD CONSTRAINT FK_Usuarios_IdRol FOREIGN KEY ([IdRol]) REFERENCES dbo.[Roles] ([IdRol]);
CREATE INDEX IX_Usuarios_IdRol ON dbo.[Usuarios] ([IdRol]);
ALTER TABLE dbo.[Compradores] ADD CONSTRAINT FK_Compradores_IdUsuario FOREIGN KEY ([IdUsuario]) REFERENCES dbo.[Usuarios] ([IdUsuario]);
CREATE INDEX IX_Compradores_IdUsuario ON dbo.[Compradores] ([IdUsuario]);
ALTER TABLE dbo.[Vendedores] ADD CONSTRAINT FK_Vendedores_IdUsuario FOREIGN KEY ([IdUsuario]) REFERENCES dbo.[Usuarios] ([IdUsuario]);
CREATE INDEX IX_Vendedores_IdUsuario ON dbo.[Vendedores] ([IdUsuario]);
ALTER TABLE dbo.[Vendedores] ADD CONSTRAINT FK_Vendedores_VerificadoPor FOREIGN KEY ([VerificadoPor]) REFERENCES dbo.[Usuarios] ([IdUsuario]);
CREATE INDEX IX_Vendedores_VerificadoPor ON dbo.[Vendedores] ([VerificadoPor]);
ALTER TABLE dbo.[Administradores] ADD CONSTRAINT FK_Administradores_IdUsuario FOREIGN KEY ([IdUsuario]) REFERENCES dbo.[Usuarios] ([IdUsuario]);
CREATE INDEX IX_Administradores_IdUsuario ON dbo.[Administradores] ([IdUsuario]);
ALTER TABLE dbo.[Propiedades] ADD CONSTRAINT FK_Propiedades_IdVendedor FOREIGN KEY ([IdVendedor]) REFERENCES dbo.[Vendedores] ([IdVendedor]);
CREATE INDEX IX_Propiedades_IdVendedor ON dbo.[Propiedades] ([IdVendedor]);
ALTER TABLE dbo.[Propiedades] ADD CONSTRAINT FK_Propiedades_IdTipoPropiedad FOREIGN KEY ([IdTipoPropiedad]) REFERENCES dbo.[TiposPropiedad] ([IdTipoPropiedad]);
CREATE INDEX IX_Propiedades_IdTipoPropiedad ON dbo.[Propiedades] ([IdTipoPropiedad]);
ALTER TABLE dbo.[Propiedades] ADD CONSTRAINT FK_Propiedades_IdProvincia FOREIGN KEY ([IdProvincia]) REFERENCES dbo.[Provincias] ([IdProvincia]);
CREATE INDEX IX_Propiedades_IdProvincia ON dbo.[Propiedades] ([IdProvincia]);
ALTER TABLE dbo.[PropiedadCaracteristicas] ADD CONSTRAINT FK_PropiedadCaracteristicas_IdPropiedad FOREIGN KEY ([IdPropiedad]) REFERENCES dbo.[Propiedades] ([IdPropiedad]);
CREATE INDEX IX_PropiedadCaracteristicas_IdPropiedad ON dbo.[PropiedadCaracteristicas] ([IdPropiedad]);
ALTER TABLE dbo.[PropiedadCaracteristicas] ADD CONSTRAINT FK_PropiedadCaracteristicas_IdCaracteristica FOREIGN KEY ([IdCaracteristica]) REFERENCES dbo.[Caracteristicas] ([IdCaracteristica]);
CREATE INDEX IX_PropiedadCaracteristicas_IdCaracteristica ON dbo.[PropiedadCaracteristicas] ([IdCaracteristica]);
ALTER TABLE dbo.[Publicaciones] ADD CONSTRAINT FK_Publicaciones_IdPropiedad FOREIGN KEY ([IdPropiedad]) REFERENCES dbo.[Propiedades] ([IdPropiedad]);
CREATE INDEX IX_Publicaciones_IdPropiedad ON dbo.[Publicaciones] ([IdPropiedad]);
ALTER TABLE dbo.[ImagenesPropiedades] ADD CONSTRAINT FK_ImagenesPropiedades_IdPropiedad FOREIGN KEY ([IdPropiedad]) REFERENCES dbo.[Propiedades] ([IdPropiedad]);
CREATE INDEX IX_ImagenesPropiedades_IdPropiedad ON dbo.[ImagenesPropiedades] ([IdPropiedad]);
ALTER TABLE dbo.[Favoritos] ADD CONSTRAINT FK_Favoritos_IdComprador FOREIGN KEY ([IdComprador]) REFERENCES dbo.[Compradores] ([IdComprador]);
CREATE INDEX IX_Favoritos_IdComprador ON dbo.[Favoritos] ([IdComprador]);
ALTER TABLE dbo.[Favoritos] ADD CONSTRAINT FK_Favoritos_IdPublicacion FOREIGN KEY ([IdPublicacion]) REFERENCES dbo.[Publicaciones] ([IdPublicacion]);
CREATE INDEX IX_Favoritos_IdPublicacion ON dbo.[Favoritos] ([IdPublicacion]);
ALTER TABLE dbo.[BusquedasGuardadas] ADD CONSTRAINT FK_BusquedasGuardadas_IdComprador FOREIGN KEY ([IdComprador]) REFERENCES dbo.[Compradores] ([IdComprador]);
CREATE INDEX IX_BusquedasGuardadas_IdComprador ON dbo.[BusquedasGuardadas] ([IdComprador]);
ALTER TABLE dbo.[Solicitudes] ADD CONSTRAINT FK_Solicitudes_IdComprador FOREIGN KEY ([IdComprador]) REFERENCES dbo.[Compradores] ([IdComprador]);
CREATE INDEX IX_Solicitudes_IdComprador ON dbo.[Solicitudes] ([IdComprador]);
ALTER TABLE dbo.[Solicitudes] ADD CONSTRAINT FK_Solicitudes_IdPublicacion FOREIGN KEY ([IdPublicacion]) REFERENCES dbo.[Publicaciones] ([IdPublicacion]);
CREATE INDEX IX_Solicitudes_IdPublicacion ON dbo.[Solicitudes] ([IdPublicacion]);
ALTER TABLE dbo.[Visitas] ADD CONSTRAINT FK_Visitas_IdSolicitud FOREIGN KEY ([IdSolicitud]) REFERENCES dbo.[Solicitudes] ([IdSolicitud]);
CREATE INDEX IX_Visitas_IdSolicitud ON dbo.[Visitas] ([IdSolicitud]);
ALTER TABLE dbo.[Reservaciones] ADD CONSTRAINT FK_Reservaciones_IdSolicitud FOREIGN KEY ([IdSolicitud]) REFERENCES dbo.[Solicitudes] ([IdSolicitud]);
CREATE INDEX IX_Reservaciones_IdSolicitud ON dbo.[Reservaciones] ([IdSolicitud]);
ALTER TABLE dbo.[Conversaciones] ADD CONSTRAINT FK_Conversaciones_IdComprador FOREIGN KEY ([IdComprador]) REFERENCES dbo.[Compradores] ([IdComprador]);
CREATE INDEX IX_Conversaciones_IdComprador ON dbo.[Conversaciones] ([IdComprador]);
ALTER TABLE dbo.[Conversaciones] ADD CONSTRAINT FK_Conversaciones_IdVendedor FOREIGN KEY ([IdVendedor]) REFERENCES dbo.[Vendedores] ([IdVendedor]);
CREATE INDEX IX_Conversaciones_IdVendedor ON dbo.[Conversaciones] ([IdVendedor]);
ALTER TABLE dbo.[Conversaciones] ADD CONSTRAINT FK_Conversaciones_IdPublicacion FOREIGN KEY ([IdPublicacion]) REFERENCES dbo.[Publicaciones] ([IdPublicacion]);
CREATE INDEX IX_Conversaciones_IdPublicacion ON dbo.[Conversaciones] ([IdPublicacion]);
ALTER TABLE dbo.[Mensajes] ADD CONSTRAINT FK_Mensajes_IdConversacion FOREIGN KEY ([IdConversacion]) REFERENCES dbo.[Conversaciones] ([IdConversacion]);
CREATE INDEX IX_Mensajes_IdConversacion ON dbo.[Mensajes] ([IdConversacion]);
ALTER TABLE dbo.[Mensajes] ADD CONSTRAINT FK_Mensajes_IdRemitente FOREIGN KEY ([IdRemitente]) REFERENCES dbo.[Usuarios] ([IdUsuario]);
CREATE INDEX IX_Mensajes_IdRemitente ON dbo.[Mensajes] ([IdRemitente]);
ALTER TABLE dbo.[Reportes] ADD CONSTRAINT FK_Reportes_IdUsuarioReporta FOREIGN KEY ([IdUsuarioReporta]) REFERENCES dbo.[Usuarios] ([IdUsuario]);
CREATE INDEX IX_Reportes_IdUsuarioReporta ON dbo.[Reportes] ([IdUsuarioReporta]);
ALTER TABLE dbo.[Reportes] ADD CONSTRAINT FK_Reportes_IdPublicacion FOREIGN KEY ([IdPublicacion]) REFERENCES dbo.[Publicaciones] ([IdPublicacion]);
CREATE INDEX IX_Reportes_IdPublicacion ON dbo.[Reportes] ([IdPublicacion]);
ALTER TABLE dbo.[Reportes] ADD CONSTRAINT FK_Reportes_IdUsuarioReportado FOREIGN KEY ([IdUsuarioReportado]) REFERENCES dbo.[Usuarios] ([IdUsuario]);
CREATE INDEX IX_Reportes_IdUsuarioReportado ON dbo.[Reportes] ([IdUsuarioReportado]);
ALTER TABLE dbo.[Reportes] ADD CONSTRAINT FK_Reportes_ResueltoPor FOREIGN KEY ([ResueltoPor]) REFERENCES dbo.[Usuarios] ([IdUsuario]);
CREATE INDEX IX_Reportes_ResueltoPor ON dbo.[Reportes] ([ResueltoPor]);
ALTER TABLE dbo.[HistorialAprobaciones] ADD CONSTRAINT FK_HistorialAprobaciones_IdVendedor FOREIGN KEY ([IdVendedor]) REFERENCES dbo.[Vendedores] ([IdVendedor]);
CREATE INDEX IX_HistorialAprobaciones_IdVendedor ON dbo.[HistorialAprobaciones] ([IdVendedor]);
ALTER TABLE dbo.[HistorialAprobaciones] ADD CONSTRAINT FK_HistorialAprobaciones_IdAdministrador FOREIGN KEY ([IdAdministrador]) REFERENCES dbo.[Administradores] ([IdAdministrador]);
CREATE INDEX IX_HistorialAprobaciones_IdAdministrador ON dbo.[HistorialAprobaciones] ([IdAdministrador]);
ALTER TABLE dbo.[Notificaciones] ADD CONSTRAINT FK_Notificaciones_IdUsuarioDestino FOREIGN KEY ([IdUsuarioDestino]) REFERENCES dbo.[Usuarios] ([IdUsuario]);
CREATE INDEX IX_Notificaciones_IdUsuarioDestino ON dbo.[Notificaciones] ([IdUsuarioDestino]);
ALTER TABLE dbo.[Pagos] ADD CONSTRAINT FK_Pagos_IdComprador FOREIGN KEY ([IdComprador]) REFERENCES dbo.[Compradores] ([IdComprador]);
CREATE INDEX IX_Pagos_IdComprador ON dbo.[Pagos] ([IdComprador]);
ALTER TABLE dbo.[Pagos] ADD CONSTRAINT FK_Pagos_IdSolicitud FOREIGN KEY ([IdSolicitud]) REFERENCES dbo.[Solicitudes] ([IdSolicitud]);
CREATE INDEX IX_Pagos_IdSolicitud ON dbo.[Pagos] ([IdSolicitud]);
ALTER TABLE dbo.[Pagos] ADD CONSTRAINT FK_Pagos_IdReservacion FOREIGN KEY ([IdReservacion]) REFERENCES dbo.[Reservaciones] ([IdReservacion]);
CREATE INDEX IX_Pagos_IdReservacion ON dbo.[Pagos] ([IdReservacion]);
ALTER TABLE dbo.[Contratos] ADD CONSTRAINT FK_Contratos_IdSolicitud FOREIGN KEY ([IdSolicitud]) REFERENCES dbo.[Solicitudes] ([IdSolicitud]);
CREATE INDEX IX_Contratos_IdSolicitud ON dbo.[Contratos] ([IdSolicitud]);
ALTER TABLE dbo.[Suspensiones] ADD CONSTRAINT FK_Suspensiones_IdUsuario FOREIGN KEY ([IdUsuario]) REFERENCES dbo.[Usuarios] ([IdUsuario]);
CREATE INDEX IX_Suspensiones_IdUsuario ON dbo.[Suspensiones] ([IdUsuario]);
ALTER TABLE dbo.[Suspensiones] ADD CONSTRAINT FK_Suspensiones_IdAdministrador FOREIGN KEY ([IdAdministrador]) REFERENCES dbo.[Administradores] ([IdAdministrador]);
CREATE INDEX IX_Suspensiones_IdAdministrador ON dbo.[Suspensiones] ([IdAdministrador]);
ALTER TABLE dbo.[HistorialAcciones] ADD CONSTRAINT FK_HistorialAcciones_IdUsuario FOREIGN KEY ([IdUsuario]) REFERENCES dbo.[Usuarios] ([IdUsuario]);
CREATE INDEX IX_HistorialAcciones_IdUsuario ON dbo.[HistorialAcciones] ([IdUsuario]);
CREATE UNIQUE INDEX UX_Roles_Nombre ON dbo.[Roles] ([Nombre]);
CREATE UNIQUE INDEX UX_Usuarios_Correo ON dbo.[Usuarios] ([Correo]);
CREATE UNIQUE INDEX UX_Compradores_IdUsuario ON dbo.[Compradores] ([IdUsuario]);
CREATE UNIQUE INDEX UX_Compradores_Cedula ON dbo.[Compradores] ([Cedula]);
CREATE UNIQUE INDEX UX_Vendedores_IdUsuario ON dbo.[Vendedores] ([IdUsuario]);
CREATE UNIQUE INDEX UX_Vendedores_Cedula ON dbo.[Vendedores] ([Cedula]);
CREATE UNIQUE INDEX UX_Administradores_IdUsuario ON dbo.[Administradores] ([IdUsuario]);
CREATE UNIQUE INDEX UX_TiposPropiedad_Nombre ON dbo.[TiposPropiedad] ([Nombre]);
CREATE UNIQUE INDEX UX_Provincias_Nombre ON dbo.[Provincias] ([Nombre]);
CREATE UNIQUE INDEX UX_Caracteristicas_Nombre ON dbo.[Caracteristicas] ([Nombre]);
CREATE UNIQUE INDEX UX_Reservaciones_IdSolicitud ON dbo.[Reservaciones] ([IdSolicitud]);
CREATE UNIQUE INDEX UX_Contratos_NumeroContrato ON dbo.[Contratos] ([NumeroContrato]);
CREATE UNIQUE INDEX UX_ImagenPrincipal ON dbo.ImagenesPropiedades(IdPropiedad) WHERE EsPrincipal = 1;
COMMIT;
