using Inmobiliaria.Contracts;
namespace Inmobiliaria.Application.Persistencia;
public interface IVisitasRepository
{
    Task<List<VisitaDto>?> Listar(int usuario, int solicitud);
    Task<string?> Cambiar(int usuario, int solicitud, ProgramarVisitaRequest? nueva, int? visita, bool confirmar);
}
