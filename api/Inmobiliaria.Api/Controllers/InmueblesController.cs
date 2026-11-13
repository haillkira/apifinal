using Aplicada1.Core;
using Inmobiliaria.Application;
using Inmobiliaria.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Inmobiliaria.Api.Controllers;
[ApiController,Route("api/v1")]
public sealed class InmueblesController(
    PublicacionesService publicaciones,
    IServiceResult<PropiedadDto, int> propiedades) : ControllerBase
{
    [HttpGet("catalogos/tipos")]
    public async Task<IActionResult> Tipos() => Ok(await publicaciones.Tipos());

    [HttpGet("catalogos/provincias")]
    public async Task<IActionResult> Provincias() => Ok(await publicaciones.Provincias());

    [Authorize,HttpPost("propiedades")]
    public async Task<IActionResult> Crear(PropiedadRequest r)
    {
        var propiedad = DatosPropiedad(r);
        var resultado = await propiedades.Guardar(propiedad);
        return resultado.IsSuccess ? StatusCode(201, new { propiedad.IdPropiedad }) : ErrorPropiedad(resultado.Error);
    }

    [Authorize, HttpGet("propiedades")]
    public async Task<ActionResult<List<PropiedadDto>>> ListarPropiedades()
    {
        var resultado = await propiedades.GetList(_ => true);
        return resultado.IsSuccess ? Ok(resultado.Value) : ErrorPropiedad(resultado.Error);
    }

    [Authorize, HttpGet("propiedades/{id:int}")]
    public async Task<ActionResult<PropiedadDto>> BuscarPropiedad(int id)
    {
        var resultado = await propiedades.Buscar(id);
        if (resultado.IsFailure)
            return ErrorPropiedad(resultado.Error);

        return resultado.Value is { } propiedad ? Ok(propiedad) : NotFound();
    }

    [Authorize, HttpPut("propiedades/{id:int:min(1)}")]
    public async Task<IActionResult> ActualizarPropiedad(int id, PropiedadRequest r)
    {
        var resultado = await propiedades.Guardar(DatosPropiedad(r, id));
        return resultado.IsSuccess ? NoContent() : ErrorPropiedad(resultado.Error);
    }

    [Authorize, HttpDelete("propiedades/{id:int}")]
    public async Task<IActionResult> EliminarPropiedad(int id)
    {
        var resultado = await propiedades.Eliminar(id);
        return resultado.IsSuccess ? NoContent() : ErrorPropiedad(resultado.Error);
    }

    private ActionResult ErrorPropiedad(Error error)
    {
        var respuesta = new { codigo = error.Code, mensaje = error.Description };
        return error.Code switch
        {
            "NO_ENCONTRADA" => NotFound(respuesta),
            "CON_PUBLICACIONES" => Conflict(respuesta),
            "COMPROMISOS_ACTIVOS" => Conflict(respuesta),
            _ => BadRequest(respuesta)
        };
    }

    private static PropiedadDto DatosPropiedad(PropiedadRequest r, int id = 0) => new()
    {
        IdPropiedad = id,
        IdTipoPropiedad = r.IdTipoPropiedad,
        IdProvincia = r.IdProvincia,
        Titulo = r.Titulo.Trim(),
        Descripcion = r.Descripcion.Trim(),
        Ciudad = r.Ciudad.Trim(),
        Sector = r.Sector.Trim(),
        Direccion = r.Direccion.Trim(),
        Habitaciones = r.Habitaciones,
        Banos = r.Banos,
        Parqueos = r.Parqueos,
        MetrosCuadrados = r.MetrosCuadrados
    };

    [Authorize, HttpPost("publicaciones")]
    public async Task<IActionResult> Publicar(PublicacionRequest solicitud)
    {
        var resultado = await publicaciones.Publicar(User, solicitud);
        if (resultado.IsSuccess)
            return StatusCode(201, new { IdPublicacion = resultado.Value });

        return resultado.Error.Code switch
        {
            "NO_ENCONTRADA" => NotFound(),
            "NO_DISPONIBLE" => Conflict(),
            _ => BadRequest()
        };
    }

    [HttpGet("publicaciones")]
    public async Task<IActionResult> Catalogo(string? moneda = null, int pagina = 1)
    {
        var resultado = await publicaciones.Catalogo(moneda, pagina);
        return resultado.IsSuccess ? Ok(resultado.Value) : BadRequest();
    }
}
