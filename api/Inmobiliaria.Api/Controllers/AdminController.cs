using Inmobiliaria.Api.Data;
using Inmobiliaria.Api.Entities;
using Inmobiliaria.Api.Services;
using Inmobiliaria.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Inmobiliaria.Api.Controllers;
[Authorize,ApiController,Route("api/v1/admin/vendedores")]
public sealed class AdminController(InmobiliariaDbContext db,AccesoService acceso) : ControllerBase
{
    [HttpGet("pendientes")]
    public async Task<IActionResult> Pendientes(){await acceso.Administrador(User);return Ok(await db.Vendedores.AsNoTracking().Where(x=>x.EstadoAprobacion=="Pendiente").Select(x=>new{x.IdVendedor,x.IdUsuario,x.FechaSolicitud}).ToListAsync());}
    [HttpPost("{id:int}/aprobar")]
    public Task<IActionResult> Aprobar(int id,DecisionRequest r)=>Decidir(id,r,true);
    [HttpPost("{id:int}/rechazar")]
    public Task<IActionResult> Rechazar(int id,DecisionRequest r)=>Decidir(id,r,false);
    private async Task<IActionResult> Decidir(int id,DecisionRequest r,bool aprobar)
    {
        var admin=await acceso.Administrador(User);
        if(!aprobar && string.IsNullOrWhiteSpace(r.Motivo))return BadRequest(new{mensaje="Indica el motivo."});
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var v=await db.Vendedores.FindAsync(id);if(v is null)return NotFound();
        if(v.EstadoAprobacion!="Pendiente")return Conflict(new{mensaje="La decision ya fue tomada."});
        v.EstadoAprobacion=aprobar?"Aprobado":"Rechazado";v.CuentaAprobada=aprobar;v.FechaAprobacion=aprobar?DateTime.UtcNow:null;v.VerificadoPor=admin.IdUsuario;v.MotivoRechazo=aprobar?null:r.Motivo?.Trim();
        db.HistorialAprobaciones.Add(new HistorialAprobaciones{IdVendedor=id,IdAdministrador=admin.IdAdministrador,Accion=v.EstadoAprobacion,Comentario=r.Motivo,FechaAccion=DateTime.UtcNow});
        db.Notificaciones.Add(new Notificaciones{IdUsuarioDestino=v.IdUsuario,TipoNotificacion="AprobacionVendedor",Titulo="Decision de cuenta",Mensaje=v.EstadoAprobacion,FechaCreacion=DateTime.UtcNow});
        db.HistorialAcciones.Add(new HistorialAcciones{IdUsuario=admin.IdUsuario,Accion=v.EstadoAprobacion,TablaAfectada="Vendedores",IdRegistro=id,FechaAccion=DateTime.UtcNow});
        await db.SaveChangesAsync();await tx.CommitAsync();return NoContent();
    }
}
