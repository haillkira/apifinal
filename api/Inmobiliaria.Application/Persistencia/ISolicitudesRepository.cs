using Inmobiliaria.Contracts;
namespace Inmobiliaria.Application.Persistencia;

public enum EstadoSolicitudCreada { Creada, NoDisponible, Duplicada, SinComprador }
public interface ISolicitudesRepository
{
    Task<string?> Responder(int vendedor, int solicitud, bool aceptar, string? motivo);
    Task<(EstadoSolicitudCreada Estado, int Id)> Crear(int idUsuario, SolicitudAlquilerRequest solicitud, DateTime ahora);
    Task<List<SolicitudDto>> Listar(int idUsuario, bool vendedor);
}
