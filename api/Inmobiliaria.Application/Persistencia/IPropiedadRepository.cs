using System.Linq.Expressions;
using Inmobiliaria.Contracts;

namespace Inmobiliaria.Application.Persistencia;

public interface IPropiedadRepository
{
    Task<string?> NombreTipo(int id);
    Task<bool> ExisteProvincia(int id);
    Task<int?> Guardar(int idVendedor, PropiedadDto propiedad, DateTime ahora);
    Task<PropiedadDto?> Buscar(int idVendedor, int id);
    Task<List<PropiedadDto>> Listar(int idVendedor, Expression<Func<PropiedadDto, bool>> criterio);
    Task<EstadoEliminacion> Eliminar(int idVendedor, int id);
    Task<List<TipoPropiedadDto>> TiposDeAlquiler();
    Task<List<ProvinciaDto>> Provincias();
    Task<PropiedadPublicable?> BuscarPublicable(int idVendedor, int idPropiedad);
    Task<int> Publicar(PublicacionRequest solicitud, DateTime ahora);
    Task<List<PublicacionResumenDto>> Catalogo(string? moneda, int pagina, DateTime ahora);
}

public enum EstadoEliminacion { Eliminada, NoEncontrada, ConPublicaciones, ConCompromisosActivos }

public sealed record TipoPropiedadDto(int IdTipoPropiedad, string Nombre);
public sealed record ProvinciaDto(int IdProvincia, string Nombre);
public sealed record PropiedadPublicable(string Estado, string? Tipo);
public sealed record PublicacionResumenDto(int IdPublicacion, string Titulo, string Ciudad, string Sector,
    int Habitaciones, int Banos, decimal Precio, string Moneda, decimal? Deposito, decimal? MantenimientoMensual);
