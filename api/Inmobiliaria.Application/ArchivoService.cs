using System.Security.Claims;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
namespace Inmobiliaria.Application;
public sealed class ArchivoService(IArchivoRepository repositorio, AccesoService acceso)
{
    public async Task<List<PropiedadDto>> Archivadas(ClaimsPrincipal usuario) => await repositorio.Archivadas(await acceso.Vendedor(usuario));
    public async Task<bool> Restaurar(ClaimsPrincipal usuario, int id) => await repositorio.Restaurar(await acceso.Vendedor(usuario), id);
    public async Task<List<PublicacionGestionDto>> Publicaciones(ClaimsPrincipal usuario) => await repositorio.PublicacionesPropias(await acceso.Vendedor(usuario));
    public async Task<EstadoCambioPublicacion> Cambiar(ClaimsPrincipal usuario, int id, bool publicar) => await repositorio.CambiarPublicacion(await acceso.Vendedor(usuario), id, publicar);
}
