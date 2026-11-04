using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inmobiliaria.Api.Entities;

[Table("Roles")]
public sealed class Roles
{
    [Key]
    public int IdRol { get; set; }
    [MaxLength(50)]
    public string Nombre { get; set; } = "";
    [MaxLength(500)]
    public string? Descripcion { get; set; }
}

[Table("Usuarios")]
public sealed class Usuarios
{
    [Key]
    public int IdUsuario { get; set; }
    public int IdRol { get; set; }
    [MaxLength(100)]
    public string Nombre { get; set; } = "";
    [MaxLength(100)]
    public string Apellido { get; set; } = "";
    [MaxLength(254)]
    public string Correo { get; set; } = "";
    [MaxLength(1000)]
    public string PasswordHash { get; set; } = "";
    [MaxLength(30)]
    public string Telefono { get; set; } = "";
    [MaxLength(1000)]
    public string? FotoPerfilUrl { get; set; }
    [MaxLength(20)]
    public string EstadoCuenta { get; set; } = "";
    public bool CorreoVerificado { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public DateTime? ActualizadoEn { get; set; }
}

[Table("Compradores")]
public sealed class Compradores
{
    [Key]
    public int IdComprador { get; set; }
    public int IdUsuario { get; set; }
    [MaxLength(20)]
    public string Cedula { get; set; } = "";
    public bool CuentaAprobada { get; set; }
    public DateTime? FechaVerificacion { get; set; }
    [MaxLength(30)]
    public string? PreferenciaContacto { get; set; }
}

[Table("Vendedores")]
public sealed class Vendedores
{
    [Key]
    public int IdVendedor { get; set; }
    public int IdUsuario { get; set; }
    [MaxLength(20)]
    public string Cedula { get; set; } = "";
    [MaxLength(30)]
    public string TipoVendedor { get; set; } = "";
    [MaxLength(150)]
    public string? NombreComercial { get; set; }
    [MaxLength(100)]
    public string? NumeroLicencia { get; set; }
    public bool CuentaAprobada { get; set; }
    [MaxLength(20)]
    public string EstadoAprobacion { get; set; } = "";
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    [MaxLength(1000)]
    public string? MotivoRechazo { get; set; }
    public int? VerificadoPor { get; set; }
}

[Table("Administradores")]
public sealed class Administradores
{
    [Key]
    public int IdAdministrador { get; set; }
    public int IdUsuario { get; set; }
    [MaxLength(20)]
    public string NivelPermiso { get; set; } = "";
}

[Table("TiposPropiedad")]
public sealed class TiposPropiedad
{
    [Key]
    public int IdTipoPropiedad { get; set; }
    [MaxLength(50)]
    public string Nombre { get; set; } = "";
    [MaxLength(500)]
    public string? Descripcion { get; set; }
}

[Table("Provincias")]
public sealed class Provincias
{
    [Key]
    public int IdProvincia { get; set; }
    [MaxLength(100)]
    public string Nombre { get; set; } = "";
}

[Table("Caracteristicas")]
public sealed class Caracteristicas
{
    [Key]
    public int IdCaracteristica { get; set; }
    [MaxLength(100)]
    public string Nombre { get; set; } = "";
    [MaxLength(50)]
    public string Categoria { get; set; } = "";
}

[Table("Propiedades")]
public sealed class Propiedades
{
    [Key]
    public int IdPropiedad { get; set; }
    public int IdVendedor { get; set; }
    public int IdTipoPropiedad { get; set; }
    [MaxLength(150)]
    public string Titulo { get; set; } = "";
    [MaxLength(4000)]
    public string Descripcion { get; set; } = "";
    public int IdProvincia { get; set; }
    [MaxLength(100)]
    public string Ciudad { get; set; } = "";
    [MaxLength(100)]
    public string Sector { get; set; } = "";
    [MaxLength(300)]
    public string Direccion { get; set; } = "";
    [MaxLength(500)]
    public string? ReferenciaDireccion { get; set; }
    [Column(TypeName = "decimal(10,7)")]
    public decimal? Latitud { get; set; }
    [Column(TypeName = "decimal(10,7)")]
    public decimal? Longitud { get; set; }
    public int Habitaciones { get; set; }
    public int Banos { get; set; }
    public int Parqueos { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal MetrosCuadrados { get; set; }
    public bool Amueblado { get; set; }
    public bool TienePiscina { get; set; }
    public bool TieneAscensor { get; set; }
    [MaxLength(20)]
    public string EstadoPropiedad { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public DateTime? ActualizadoEn { get; set; }
}

[Table("PropiedadCaracteristicas")]
public sealed class PropiedadCaracteristicas
{
    public int IdPropiedad { get; set; }
    public int IdCaracteristica { get; set; }
    [MaxLength(200)]
    public string? Valor { get; set; }
}

[Table("Publicaciones")]
public sealed class Publicaciones
{
    [Key]
    public int IdPublicacion { get; set; }
    public int IdPropiedad { get; set; }
    [MaxLength(20)]
    public string TipoOperacion { get; set; } = "";
    [Column(TypeName = "decimal(18,2)")]
    public decimal Precio { get; set; }
    [MaxLength(3)]
    public string Moneda { get; set; } = "";
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Deposito { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MantenimientoMensual { get; set; }
    [MaxLength(20)]
    public string EstadoPublicacion { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaPublicacion { get; set; }
    public DateTime? FechaExpiracion { get; set; }
    [MaxLength(1000)]
    public string? MotivoRechazo { get; set; }
    public bool Destacada { get; set; }
}

[Table("ImagenesPropiedades")]
public sealed class ImagenesPropiedades
{
    [Key]
    public int IdImagen { get; set; }
    public int IdPropiedad { get; set; }
    [MaxLength(1000)]
    public string UrlImagen { get; set; } = "";
    public bool EsPrincipal { get; set; }
    public int Orden { get; set; }
    [MaxLength(300)]
    public string? TextoAlternativo { get; set; }
    public DateTime FechaSubida { get; set; }
}

[Table("Favoritos")]
public sealed class Favoritos
{
    public int IdComprador { get; set; }
    public int IdPublicacion { get; set; }
    public DateTime FechaAgregado { get; set; }
}

[Table("BusquedasGuardadas")]
public sealed class BusquedasGuardadas
{
    [Key]
    public int IdBusqueda { get; set; }
    public int IdComprador { get; set; }
    [MaxLength(100)]
    public string NombreBusqueda { get; set; } = "";
    [MaxLength(4000)]
    public string FiltrosJson { get; set; } = "";
    public bool NotificarNuevosResultados { get; set; }
    public DateTime FechaCreacion { get; set; }
}

[Table("Solicitudes")]
public sealed class Solicitudes
{
    [Key]
    public int IdSolicitud { get; set; }
    public int IdComprador { get; set; }
    public int IdPublicacion { get; set; }
    [MaxLength(20)]
    public string TipoSolicitud { get; set; } = "";
    [MaxLength(2000)]
    public string? Mensaje { get; set; }
    [MaxLength(20)]
    public string EstadoSolicitud { get; set; } = "";
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaRespuesta { get; set; }
}

[Table("Visitas")]
public sealed class Visitas
{
    [Key]
    public int IdVisita { get; set; }
    public int IdSolicitud { get; set; }
    public DateTime FechaVisita { get; set; }
    [MaxLength(20)]
    public string EstadoVisita { get; set; } = "";
    [MaxLength(1000)]
    public string? Notas { get; set; }
    public DateTime FechaCreacion { get; set; }
}

[Table("Reservaciones")]
public sealed class Reservaciones
{
    [Key]
    public int IdReservacion { get; set; }
    public int IdSolicitud { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoReserva { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    [MaxLength(20)]
    public string EstadoReservacion { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
}

[Table("Conversaciones")]
public sealed class Conversaciones
{
    [Key]
    public int IdConversacion { get; set; }
    public int IdComprador { get; set; }
    public int IdVendedor { get; set; }
    public int? IdPublicacion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? UltimoMensajeEn { get; set; }
}

[Table("Mensajes")]
public sealed class Mensajes
{
    [Key]
    public int IdMensaje { get; set; }
    public int IdConversacion { get; set; }
    public int IdRemitente { get; set; }
    [MaxLength(4000)]
    public string Contenido { get; set; } = "";
    public DateTime FechaEnvio { get; set; }
    public bool Leido { get; set; }
    public DateTime? FechaLectura { get; set; }
}

[Table("Reportes")]
public sealed class Reportes
{
    [Key]
    public int IdReporte { get; set; }
    public int IdUsuarioReporta { get; set; }
    public int? IdPublicacion { get; set; }
    public int? IdUsuarioReportado { get; set; }
    [MaxLength(40)]
    public string Categoria { get; set; } = "";
    [MaxLength(2000)]
    public string Descripcion { get; set; } = "";
    [MaxLength(20)]
    public string EstadoReporte { get; set; } = "";
    public DateTime FechaReporte { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public int? ResueltoPor { get; set; }
    [MaxLength(2000)]
    public string? Resolucion { get; set; }
}

[Table("HistorialAprobaciones")]
public sealed class HistorialAprobaciones
{
    [Key]
    public int IdHistorial { get; set; }
    public int IdVendedor { get; set; }
    public int IdAdministrador { get; set; }
    [MaxLength(20)]
    public string Accion { get; set; } = "";
    [MaxLength(1000)]
    public string? Comentario { get; set; }
    public DateTime FechaAccion { get; set; }
}

[Table("Notificaciones")]
public sealed class Notificaciones
{
    [Key]
    public int IdNotificacion { get; set; }
    public int IdUsuarioDestino { get; set; }
    [MaxLength(40)]
    public string TipoNotificacion { get; set; } = "";
    [MaxLength(150)]
    public string Titulo { get; set; } = "";
    [MaxLength(2000)]
    public string Mensaje { get; set; } = "";
    [MaxLength(1000)]
    public string? UrlDestino { get; set; }
    public bool Leida { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaLectura { get; set; }
}

[Table("Pagos")]
public sealed class Pagos
{
    [Key]
    public int IdPago { get; set; }
    public int IdComprador { get; set; }
    public int? IdSolicitud { get; set; }
    public int? IdReservacion { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }
    [MaxLength(3)]
    public string Moneda { get; set; } = "";
    [MaxLength(200)]
    public string Concepto { get; set; } = "";
    [MaxLength(30)]
    public string MetodoPago { get; set; } = "";
    [MaxLength(200)]
    public string? ReferenciaExterna { get; set; }
    [MaxLength(20)]
    public string EstadoPago { get; set; } = "";
    public DateTime? FechaPago { get; set; }
    public DateTime FechaRegistro { get; set; }
}

[Table("Contratos")]
public sealed class Contratos
{
    [Key]
    public int IdContrato { get; set; }
    public int IdSolicitud { get; set; }
    [MaxLength(20)]
    public string TipoContrato { get; set; } = "";
    [MaxLength(100)]
    public string NumeroContrato { get; set; } = "";
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoAcordado { get; set; }
    [MaxLength(3)]
    public string Moneda { get; set; } = "";
    [MaxLength(1000)]
    public string? DocumentoUrl { get; set; }
    [MaxLength(20)]
    public string EstadoContrato { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
}

[Table("Suspensiones")]
public sealed class Suspensiones
{
    [Key]
    public int IdSuspension { get; set; }
    public int IdUsuario { get; set; }
    public int IdAdministrador { get; set; }
    [MaxLength(1000)]
    public string Motivo { get; set; } = "";
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public bool Activa { get; set; }
}

[Table("HistorialAcciones")]
public sealed class HistorialAcciones
{
    [Key]
    public int IdAccion { get; set; }
    public int? IdUsuario { get; set; }
    [MaxLength(100)]
    public string Accion { get; set; } = "";
    [MaxLength(100)]
    public string TablaAfectada { get; set; } = "";
    public int? IdRegistro { get; set; }
    [MaxLength(4000)]
    public string? Detalles { get; set; }
    [MaxLength(64)]
    public string? DireccionIP { get; set; }
    public DateTime FechaAccion { get; set; }
}
