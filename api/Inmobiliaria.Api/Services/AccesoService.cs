using System.Security.Claims;
using Inmobiliaria.Api.Data;
using Inmobiliaria.Api.Entities;
using Microsoft.EntityFrameworkCore;
namespace Inmobiliaria.Api.Services;
public sealed class AccesoService(InmobiliariaDbContext db)
{
    public async Task<Usuarios> Usuario(ClaimsPrincipal principal)
    {
        if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier),out var id)) throw new UnauthorizedAccessException();
        var u=await db.Usuarios.FindAsync(id);
        if (u is null || u.EstadoCuenta!="Activa") throw new UnauthorizedAccessException("Cuenta no activa.");
        if(await db.Suspensiones.AnyAsync(x=>x.IdUsuario==id && x.Activa && x.FechaInicio<=DateTime.UtcNow && (x.FechaFin==null || x.FechaFin>DateTime.UtcNow))) throw new UnauthorizedAccessException("Cuenta suspendida.");
        return u;
    }
    public async Task<Vendedores> Vendedor(ClaimsPrincipal p)
    {
        var u=await Usuario(p);
        if (!await db.Roles.AnyAsync(x=>x.IdRol==u.IdRol && x.Nombre=="Vendedor")) throw new AccesoDenegadoException();
        return await db.Vendedores.SingleOrDefaultAsync(x=>x.IdUsuario==u.IdUsuario && x.CuentaAprobada && x.EstadoAprobacion=="Aprobado") ?? throw new AccesoDenegadoException("Vendedor pendiente de aprobacion.");
    }
    public async Task<Administradores> Administrador(ClaimsPrincipal p)
    {
        var u=await Usuario(p);
        if (!await db.Roles.AnyAsync(x=>x.IdRol==u.IdRol && x.Nombre=="Administrador")) throw new AccesoDenegadoException();
        return await db.Administradores.SingleOrDefaultAsync(x=>x.IdUsuario==u.IdUsuario && (x.NivelPermiso=="Completo" || x.NivelPermiso=="Moderador")) ?? throw new AccesoDenegadoException();
    }
}
public sealed class AccesoDenegadoException(string message="No tienes permiso.") : Exception(message);
