using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Inmobiliaria.Application;
using Inmobiliaria.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Inmobiliaria.Api.Controllers;

[ApiController, Route("api/v1/auth")]
public sealed class AuthController(CuentasService cuentas, AccesoService acceso, IConfiguration config) : ControllerBase
{
    [HttpPost("compradores")]
    public Task<IActionResult> Comprador(RegistroRequest solicitud) => Registrar(solicitud, "Comprador");

    [HttpPost("vendedores")]
    public Task<IActionResult> Vendedor(RegistroRequest solicitud) => Registrar(solicitud, "Vendedor");

    private async Task<IActionResult> Registrar(RegistroRequest solicitud, string rol)
    {
        var resultado = await cuentas.Registrar(solicitud, rol);
        if (resultado.IsSuccess)
            return StatusCode(201, resultado.Value);

        return resultado.Error.Code == "SIN_ROL"
            ? Problem(statusCode: 503, title: resultado.Error.Description)
            : Conflict(new { mensaje = resultado.Error.Description });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest solicitud)
    {
        var key = config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            return Problem(statusCode: 503, title: "Configura una clave JWT de al menos 32 bytes.");

        var usuario = await cuentas.IniciarSesion(solicitud);
        if (usuario is null)
            return Unauthorized();

        var expiraEn = DateTime.UtcNow.AddMinutes(30);
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"],
            [new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()), new Claim(ClaimTypes.Role, usuario.Rol)],
            expires: expiraEn,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));
        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiraEn, usuario));
    }

    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me() => Ok(await acceso.Usuario(User));
}
