using Inmobiliaria.Contracts;
namespace Inmobiliaria.Application.Persistencia;
public enum EstadoCambioPublicacion { Guardada, NoEncontrada, NoDisponible }
public interface IArchivoRepository
{
    Task<List<PropiedadDto>> Archivadas(int vendedor);
    Task<bool> Restaurar(int vendedor, int propiedad);
    Task<List<PublicacionGestionDto>> PublicacionesPropias(int vendedor);
    Task<EstadoCambioPublicacion> CambiarPublicacion(int vendedor, int publicacion, bool publicar);
}
