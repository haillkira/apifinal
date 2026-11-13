using Inmobiliaria.Application;
using Inmobiliaria.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Inmobiliaria.Api.Controllers;

[Authorize, ApiController, Route("api/v1/solicitudes")]
public sealed class SolicitudesController(SolicitudesService solicitudes) : ControllerBase
{
    [HttpPost("{id:int}/responder")]
    public async Task<IActionResult> Responder(int id, ResponderSolicitudRequest request) => await solicitudes.Responder(User,id,request) switch
    {
        null => NoContent(),
        "NO_ENCONTRADA" => NotFound(),
        "MOTIVO_INVALIDO" => BadRequest(new { mensaje = "Indica un motivo de rechazo, de máximo 1000 caracteres." }),
        _ => Conflict(new { mensaje = "La solicitud ya fue respondida o la publicación ya no está disponible." })
    };
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await solicitudes.Listar(User));

    [HttpPost]
    public async Task<IActionResult> Crear(SolicitudAlquilerRequest request)
    {
        var resultado = await solicitudes.Crear(User, request);
        if (resultado.IsSuccess) return StatusCode(201, new { IdSolicitud = resultado.Value });
        var error = new { mensaje = resultado.Error.Description };
        return resultado.Error.Code == "DATOS_INVALIDOS" ? BadRequest(error) : Conflict(error);
    }
}
