using System.Security.Claims;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
namespace Inmobiliaria.Application;
public sealed class VisitasService(IVisitasRepository repositorio, AccesoService acceso)
{
    private async Task<int> Usuario(ClaimsPrincipal principal) {
        var usuario = await acceso.Usuario(principal);
        if(usuario.Rol is not ("Comprador" or "Vendedor")) throw new AccesoDenegadoException();
        if(usuario.Rol == "Vendedor") await acceso.Vendedor(principal);
        return usuario.IdUsuario;
    }
    public async Task<List<VisitaDto>?> Listar(ClaimsPrincipal principal,int solicitud) => await repositorio.Listar(await Usuario(principal),solicitud);
    public async Task<string?> Cambiar(ClaimsPrincipal principal,int solicitud,ProgramarVisitaRequest? nueva,int? visita,bool confirmar) {
        var usuario = await Usuario(principal);
        if(nueva is not null && (nueva.Fecha <= DateTimeOffset.UtcNow || nueva.Notas?.Length > 1000)) return "DATOS_INVALIDOS";
        return await repositorio.Cambiar(usuario,solicitud,nueva,visita,confirmar);
    }
}
