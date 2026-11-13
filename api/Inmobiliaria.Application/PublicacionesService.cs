using System.Security.Claims;
using Aplicada1.Core;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;

namespace Inmobiliaria.Application;

public sealed class PublicacionesService(IPropiedadRepository propiedades, AccesoService acceso)
{
    public Task<List<TipoPropiedadDto>> Tipos() => propiedades.TiposDeAlquiler();

    public Task<List<ProvinciaDto>> Provincias() => propiedades.Provincias();

    public async Task<Result<int>> Publicar(ClaimsPrincipal principal, PublicacionRequest solicitud)
    {
        ReglasAlquiler.ValidarOferta(solicitud.TipoOperacion, solicitud.Moneda, solicitud.Precio,
            solicitud.Deposito, solicitud.MantenimientoMensual);

        var idVendedor = await acceso.Vendedor(principal);
        var propiedad = await propiedades.BuscarPublicable(idVendedor, solicitud.IdPropiedad);
        if (propiedad is null)
            return Result.Failure<int>(new Error("NO_ENCONTRADA", "Propiedad no encontrada."));
        if (propiedad.Estado != "Disponible")
            return Result.Failure<int>(new Error("NO_DISPONIBLE", "Propiedad no disponible."));
        if (propiedad.Tipo is null || !ReglasAlquiler.TipoPermitido(propiedad.Tipo))
            return Result.Failure<int>(new Error("TIPO_INVALIDO", "Tipo de propiedad invalido."));

        return Result.Success(await propiedades.Publicar(solicitud, DateTime.UtcNow));
    }

    public async Task<Result<List<PublicacionResumenDto>>> Catalogo(string? moneda, int pagina)
    {
        if (pagina < 1 || pagina > 10000 || (moneda is not null && moneda is not ("DOP" or "USD")))
            return Result.Failure<List<PublicacionResumenDto>>(new Error("FILTRO_INVALIDO", "Filtro invalido."));

        return Result.Success(await propiedades.Catalogo(moneda, pagina, DateTime.UtcNow));
    }
}
