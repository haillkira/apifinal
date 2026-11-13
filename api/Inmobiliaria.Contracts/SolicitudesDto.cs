using System.ComponentModel.DataAnnotations;
namespace Inmobiliaria.Contracts;

public sealed record SolicitudAlquilerRequest([Range(1, int.MaxValue)] int IdPublicacion,
    [MaxLength(2000)] string? Mensaje);
public sealed record SolicitudDto(int IdSolicitud, int IdPublicacion, string Titulo,
    string Comprador, string Estado, string? Mensaje, DateTime FechaSolicitud, bool PublicacionRetirada = false,
    string? Respuesta = null, DateTime? FechaRespuesta = null);
public sealed record ResponderSolicitudRequest(bool Aceptar, [MaxLength(1000)] string? Motivo);
