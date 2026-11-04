using Inmobiliaria.Api.Data;
using Inmobiliaria.Api.Entities;
using Inmobiliaria.Api.Services;
using Inmobiliaria.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Inmobiliaria.Api.Controllers;
[ApiController,Route("api/v1")]
public sealed class InmueblesController(InmobiliariaDbContext db,AccesoService acceso) : ControllerBase
{
    [HttpGet("catalogos/tipos")]
    public async Task<IActionResult> Tipos()=>Ok(await db.TiposPropiedad.AsNoTracking().Where(x=>x.Nombre=="Apartamento" || x.Nombre=="Casa").Select(x=>new{x.IdTipoPropiedad,x.Nombre}).ToListAsync());
    [HttpGet("catalogos/provincias")]
    public async Task<IActionResult> Provincias()=>Ok(await db.Provincias.AsNoTracking().OrderBy(x=>x.Nombre).Select(x=>new{x.IdProvincia,x.Nombre}).ToListAsync());
    [Authorize,HttpPost("propiedades")]
    public async Task<IActionResult> Crear(PropiedadRequest r)
    {
        var vendedor=await acceso.Vendedor(User);
        var tipo=await db.TiposPropiedad.FindAsync(r.IdTipoPropiedad);
        if(tipo is null || !ReglasAlquiler.TipoPermitido(tipo.Nombre))return BadRequest(new{mensaje="Solo Apartamento y Casa."});
        if(!await db.Provincias.AnyAsync(x=>x.IdProvincia==r.IdProvincia))return BadRequest(new{mensaje="Provincia invalida."});
        var p=new Propiedades{IdVendedor=vendedor.IdVendedor,IdTipoPropiedad=r.IdTipoPropiedad,Titulo=r.Titulo.Trim(),Descripcion=r.Descripcion.Trim(),IdProvincia=r.IdProvincia,Ciudad=r.Ciudad.Trim(),Sector=r.Sector.Trim(),Direccion=r.Direccion.Trim(),Habitaciones=r.Habitaciones,Banos=r.Banos,Parqueos=r.Parqueos,MetrosCuadrados=r.MetrosCuadrados,EstadoPropiedad="Disponible",FechaCreacion=DateTime.UtcNow};
        db.Propiedades.Add(p);await db.SaveChangesAsync();return StatusCode(201,new{p.IdPropiedad});
    }
    [Authorize,HttpPost("publicaciones")]
    public async Task<IActionResult> Publicar(PublicacionRequest r)
    {
        ReglasAlquiler.ValidarOferta(r.TipoOperacion,r.Moneda,r.Precio,r.Deposito,r.MantenimientoMensual);
        var v=await acceso.Vendedor(User);
        var p=await db.Propiedades.SingleOrDefaultAsync(x=>x.IdPropiedad==r.IdPropiedad && x.IdVendedor==v.IdVendedor);
        if(p is null)return NotFound();if(p.EstadoPropiedad!="Disponible")return Conflict();
        var tipo=await db.TiposPropiedad.FindAsync(p.IdTipoPropiedad);
        if(tipo is null || !ReglasAlquiler.TipoPermitido(tipo.Nombre))return BadRequest();
        var pub=new Publicaciones{IdPropiedad=p.IdPropiedad,TipoOperacion="Alquiler",Precio=r.Precio,Moneda=r.Moneda,Deposito=r.Deposito,MantenimientoMensual=r.MantenimientoMensual,EstadoPublicacion="Publicada",FechaCreacion=DateTime.UtcNow,FechaPublicacion=DateTime.UtcNow};
        db.Publicaciones.Add(pub);await db.SaveChangesAsync();return StatusCode(201,new{pub.IdPublicacion});
    }
    [HttpGet("publicaciones")]
    public async Task<IActionResult> Catalogo(string? moneda=null,int pagina=1)
    {
        if(pagina<1 || pagina>10000 || (moneda is not null && moneda is not ("DOP" or "USD")))return BadRequest();
        var q=from a in db.Publicaciones.AsNoTracking() join p in db.Propiedades on a.IdPropiedad equals p.IdPropiedad join v in db.Vendedores on p.IdVendedor equals v.IdVendedor join u in db.Usuarios on v.IdUsuario equals u.IdUsuario join t in db.TiposPropiedad on p.IdTipoPropiedad equals t.IdTipoPropiedad
        where a.TipoOperacion=="Alquiler" && a.EstadoPublicacion=="Publicada" && p.EstadoPropiedad=="Disponible" && v.CuentaAprobada && v.EstadoAprobacion=="Aprobado" && u.EstadoCuenta=="Activa" && (t.Nombre=="Apartamento" || t.Nombre=="Casa") && (moneda==null || a.Moneda==moneda)
        && !db.Suspensiones.Any(s=>s.IdUsuario==u.IdUsuario && s.Activa && s.FechaInicio<=DateTime.UtcNow && (s.FechaFin==null || s.FechaFin>DateTime.UtcNow))
        select new{a.IdPublicacion,p.Titulo,p.Ciudad,p.Sector,p.Habitaciones,p.Banos,a.Precio,a.Moneda,a.Deposito,a.MantenimientoMensual};
        return Ok(await q.OrderByDescending(x=>x.IdPublicacion).Skip((pagina-1)*20).Take(20).ToListAsync());
    }
}
