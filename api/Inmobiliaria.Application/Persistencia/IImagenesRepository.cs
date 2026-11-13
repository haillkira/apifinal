using Inmobiliaria.Contracts;
namespace Inmobiliaria.Application.Persistencia;
public interface IImagenesRepository
{
    Task<bool> EsPropietario(int propiedad, int vendedor);
    Task<List<ImagenDto>> Listar(int propiedad);
    Task<bool> Agregar(int propiedad, int vendedor, IReadOnlyList<string> nombres);
    Task<string?> Eliminar(int propiedad, int vendedor, int imagen);
    Task<int?> PropiedadPublica(int publicacion);
}
