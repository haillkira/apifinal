using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Inmobiliaria.Api.Data;
using Inmobiliaria.Api.Entities;
using Inmobiliaria.Api.Services;
using Inmobiliaria.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
namespace Inmobiliaria.Api.Controllers;
[ApiController,Route("api/v1/auth")]
public sealed class AuthController(InmobiliariaDbContext db,IConfiguration config,AccesoService acceso) : ControllerBase
{
    [HttpPost("compradores")]
    public Task<IActionResult> Comprador(RegistroRequest r)=>Registrar(r,"Comprador");
    [HttpPost("vendedores")]
    public Task<IActionResult> Vendedor(RegistroRequest r)=>Registrar(r,"Vendedor");
    private async Task<IActionResult> Registrar(RegistroRequest r,string rol)
    {
        var correo=r.Correo.Trim().ToLowerInvariant();
        if(await db.Usuarios.AnyAsync(x=>x.Correo==correo) || await db.Compradores.AnyAsync(x=>x.Cedula==r.Cedula) || await db.Vendedores.AnyAsync(x=>x.Cedula==r.Cedula)) return Conflict(new {mensaje="Correo o documento ya registrado."});
        var role=await db.Roles.SingleOrDefaultAsync(x=>x.Nombre==rol);
        if(role is null) return Problem(statusCode:503,title:"Ejecuta los catalogos SQL antes de registrar usuarios.");
        // SaveChanges aplica usuario y perfil en una sola transaccion.
        var u=new Usuarios{IdRol=role.IdRol,Nombre=r.Nombre.Trim(),Apellido=r.Apellido.Trim(),Correo=correo,Telefono=r.Telefono.Trim(),EstadoCuenta="Activa",FechaRegistro=DateTime.UtcNow};
        u.PasswordHash=new PasswordHasher<Usuarios>().HashPassword(u,r.Password);
        // Se usa una transaccion serializable contra registros simultaneos de cedula.
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if(await db.Compradores.AnyAsync(x=>x.Cedula==r.Cedula) || await db.Vendedores.AnyAsync(x=>x.Cedula==r.Cedula)) return Conflict(new{mensaje="Documento ya registrado."});
        db.Usuarios.Add(u);await db.SaveChangesAsync();
        if(rol=="Comprador") db.Compradores.Add(new Compradores{IdUsuario=u.IdUsuario,Cedula=r.Cedula,CuentaAprobada=false});
        else db.Vendedores.Add(new Vendedores{IdUsuario=u.IdUsuario,Cedula=r.Cedula,TipoVendedor="Particular",EstadoAprobacion="Pendiente",FechaSolicitud=DateTime.UtcNow});
        await db.SaveChangesAsync();await tx.CommitAsync();
        return StatusCode(201,new UsuarioResponse(u.IdUsuario,u.Nombre,u.Apellido,rol));
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest r)
    {
        var key=config["Jwt:Key"];
        if(string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key)<32) return Problem(statusCode:503,title:"Configura una clave JWT de al menos 32 bytes.");
        var correo=r.Correo.Trim().ToLowerInvariant();
        var u=await db.Usuarios.SingleOrDefaultAsync(x=>x.Correo==correo);
        if(u is null || u.EstadoCuenta!="Activa" || new PasswordHasher<Usuarios>().VerifyHashedPassword(u,u.PasswordHash,r.Password)==PasswordVerificationResult.Failed) return Unauthorized();
        if(await db.Suspensiones.AnyAsync(x=>x.IdUsuario==u.IdUsuario && x.Activa && x.FechaInicio<=DateTime.UtcNow && (x.FechaFin==null || x.FechaFin>DateTime.UtcNow))) return Unauthorized();
        var rol=await db.Roles.Where(x=>x.IdRol==u.IdRol).Select(x=>x.Nombre).SingleAsync();
        var exp=DateTime.UtcNow.AddMinutes(30);
        var token=new JwtSecurityToken(config["Jwt:Issuer"],config["Jwt:Audience"],new[]{new Claim(ClaimTypes.NameIdentifier,u.IdUsuario.ToString()),new Claim(ClaimTypes.Role,rol)},expires:exp,signingCredentials:new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),SecurityAlgorithms.HmacSha256));
        u.UltimoAcceso=DateTime.UtcNow;await db.SaveChangesAsync();
        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token),exp,new(u.IdUsuario,u.Nombre,u.Apellido,rol)));
    }
    [Authorize,HttpGet("me")]
    public async Task<IActionResult> Me(){var u=await acceso.Usuario(User);var rol=await db.Roles.Where(x=>x.IdRol==u.IdRol).Select(x=>x.Nombre).SingleAsync();return Ok(new UsuarioResponse(u.IdUsuario,u.Nombre,u.Apellido,rol));}
}
