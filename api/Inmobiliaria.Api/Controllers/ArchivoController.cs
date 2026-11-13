using Inmobiliaria.Application;
using Inmobiliaria.Application.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Inmobiliaria.Api.Controllers;
[Authorize, ApiController, Route("api/v1")]
public sealed class ArchivoController(ArchivoService archivo) : ControllerBase
{
    [HttpGet("propiedades/archivadas")]
    public async Task<IActionResult> Archivadas() => Ok(await archivo.Archivadas(User));
    [HttpPost("propiedades/{id:int}/restaurar")]
    public async Task<IActionResult> Restaurar(int id) => await archivo.Restaurar(User, id) ? NoContent() : NotFound();
    [HttpGet("publicaciones/mias")]
    public async Task<IActionResult> Propias() => Ok(await archivo.Publicaciones(User));
    [HttpPost("publicaciones/{id:int}/retirar")]
    public Task<IActionResult> Retirar(int id) => Cambiar(id, false);
    [HttpPost("publicaciones/{id:int}/reactivar")]
    public Task<IActionResult> Reactivar(int id) => Cambiar(id, true);
    private async Task<IActionResult> Cambiar(int id, bool publicar) => await archivo.Cambiar(User, id, publicar) switch
    {
        EstadoCambioPublicacion.Guardada => NoContent(),
        EstadoCambioPublicacion.NoEncontrada => NotFound(),
        _ => Conflict(new { mensaje = "La propiedad no está disponible o la publicación no se puede reactivar." })
    };
}
