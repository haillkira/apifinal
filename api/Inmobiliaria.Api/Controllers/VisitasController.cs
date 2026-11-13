using Inmobiliaria.Application;
using Inmobiliaria.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Inmobiliaria.Api.Controllers;
[Authorize,ApiController,Route("api/v1/solicitudes/{solicitud:int}/visitas")]
public sealed class VisitasController(VisitasService visitas) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Listar(int solicitud) {
        var lista=await visitas.Listar(User,solicitud); return lista is null ? NotFound() : Ok(lista);
    }
    [HttpPost] public async Task<IActionResult> Crear(int solicitud,ProgramarVisitaRequest request) => Resultado(await visitas.Cambiar(User,solicitud,request,null,false));
    [HttpPost("{id:int}/confirmar")] public async Task<IActionResult> Confirmar(int solicitud,int id) => Resultado(await visitas.Cambiar(User,solicitud,null,id,true));
    [HttpPost("{id:int}/cancelar")] public async Task<IActionResult> Cancelar(int solicitud,int id) => Resultado(await visitas.Cambiar(User,solicitud,null,id,false));
    private IActionResult Resultado(string? error) => error switch {
        null => NoContent(), "NO_ENCONTRADA" => NotFound(), "PROHIBIDO" => Forbid(),
        "DATOS_INVALIDOS" => BadRequest(new { mensaje="Selecciona una fecha futura y notas de máximo 1000 caracteres." }),
        _ => Conflict(new { mensaje="No se puede cambiar la visita. Revisa el estado de la solicitud, publicación y visitas existentes." })
    };
}
