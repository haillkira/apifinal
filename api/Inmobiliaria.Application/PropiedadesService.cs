using System.Linq.Expressions;
using System.Security.Claims;
using Aplicada1.Core;
using Inmobiliaria.Contracts;
using Inmobiliaria.Application.Persistencia;

namespace Inmobiliaria.Application;

public sealed class PropiedadesService(
    IPropiedadRepository repositorio,
    AccesoService acceso,
    ClaimsPrincipal usuario) : IServiceResult<PropiedadDto, int>, IService<PropiedadDto, int>
{
    public async Task<Result> Guardar(PropiedadDto propiedad)
    {
        var idVendedor = await acceso.Vendedor(usuario);
        var tipo = await repositorio.NombreTipo(propiedad.IdTipoPropiedad);
        if (tipo is null || !ReglasAlquiler.TipoPermitido(tipo))
            return Result.Failure(new Error("TIPO_INVALIDO", "Solo Apartamento y Casa."));

        if (!await repositorio.ExisteProvincia(propiedad.IdProvincia))
            return Result.Failure(new Error("PROVINCIA_INVALIDA", "Provincia invalida."));

        var id = await repositorio.Guardar(idVendedor, propiedad, DateTime.UtcNow);
        if (id is null)
            return Result.Failure(new Error("NO_ENCONTRADA", "Propiedad no encontrada."));

        propiedad.IdPropiedad = id.Value;
        return Result.Success();
    }

    public async Task<Result<PropiedadDto?>> Buscar(int idPropiedad)
    {
        var idVendedor = await acceso.Vendedor(usuario);
        return Result.Success<PropiedadDto?>(await repositorio.Buscar(idVendedor, idPropiedad));
    }

    public async Task<Result<List<PropiedadDto>>> GetList(Expression<Func<PropiedadDto, bool>> criterio)
    {
        var idVendedor = await acceso.Vendedor(usuario);
        return Result.Success(await repositorio.Listar(idVendedor, criterio));
    }

    public async Task<Result> Eliminar(int idPropiedad)
    {
        var idVendedor = await acceso.Vendedor(usuario);
        return await repositorio.Eliminar(idVendedor, idPropiedad) switch
        {
            EstadoEliminacion.Eliminada => Result.Success(),
            EstadoEliminacion.ConPublicaciones => Result.Failure(new Error("CON_PUBLICACIONES", "La propiedad tiene publicaciones asociadas.")),
            _ => Result.Failure(new Error("NO_ENCONTRADA", "Propiedad no encontrada."))
        };
    }

    async Task<bool> IService<PropiedadDto, int>.Guardar(PropiedadDto propiedad) =>
        (await Guardar(propiedad)).IsSuccess;

    async Task<PropiedadDto?> IService<PropiedadDto, int>.Buscar(int idPropiedad) =>
        (await Buscar(idPropiedad)).Value;

    async Task<List<PropiedadDto>> IService<PropiedadDto, int>.GetList(Expression<Func<PropiedadDto, bool>> criterio) =>
        (await GetList(criterio)).Value;

    async Task<bool> IService<PropiedadDto, int>.Eliminar(int idPropiedad) =>
        (await Eliminar(idPropiedad)).IsSuccess;

}
