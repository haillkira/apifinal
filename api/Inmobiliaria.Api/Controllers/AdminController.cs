using Inmobiliaria.Application;
using Inmobiliaria.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inmobiliaria.Api.Controllers;

[Authorize, ApiController, Route("api/v1/admin/vendedores")]
public sealed class AdminController(CuentasService cuentas) : ControllerBase
{
    [HttpGet("pendientes")]
    public async Task<IActionResult> Pendientes() => Ok(await cuentas.Pendientes(User));

    [HttpPost("{id:int}/aprobar")]
    public Task<IActionResult> Aprobar(int id, DecisionRequest solicitud) => Decidir(id, solicitud, true);

    [HttpPost("{id:int}/rechazar")]
    public Task<IActionResult> Rechazar(int id, DecisionRequest solicitud) => Decidir(id, solicitud, false);

    private async Task<IActionResult> Decidir(int id, DecisionRequest solicitud, bool aprobar)
    {
        var resultado = await cuentas.Decidir(User, id, aprobar, solicitud.Motivo);
        if (resultado.IsSuccess)
            return NoContent();

        return resultado.Error.Code switch
        {
            "NO_ENCONTRADO" => NotFound(),
            "YA_DECIDIDO" => Conflict(new { mensaje = resultado.Error.Description }),
            _ => BadRequest(new { mensaje = resultado.Error.Description })
        };
    }
}
