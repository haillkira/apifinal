using System.Security.Claims;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;

namespace Inmobiliaria.Application;

public sealed class AccesoService(ICuentasRepository cuentas)
{
    public async Task<UsuarioResponse> Usuario(ClaimsPrincipal principal)
    {
        if (!int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id))
            throw new UnauthorizedAccessException();

        var cuenta = await cuentas.BuscarPorId(id);
        if (cuenta is null || cuenta.Estado != "Activa")
            throw new UnauthorizedAccessException("Cuenta no activa.");

        if (await cuentas.EstaSuspendida(id, DateTime.UtcNow))
            throw new UnauthorizedAccessException("Cuenta suspendida.");

        return cuenta.Usuario;
    }

    public async Task<int> Vendedor(ClaimsPrincipal principal)
    {
        var usuario = await Usuario(principal);
        if (usuario.Rol != "Vendedor")
            throw new AccesoDenegadoException();

        return await cuentas.BuscarVendedorAprobado(usuario.IdUsuario)
            ?? throw new AccesoDenegadoException("Vendedor pendiente de aprobacion.");
    }

    public async Task<AdministradorAutorizado> Administrador(ClaimsPrincipal principal)
    {
        var usuario = await Usuario(principal);
        if (usuario.Rol != "Administrador")
            throw new AccesoDenegadoException();

        var id = await cuentas.BuscarAdministradorAutorizado(usuario.IdUsuario)
            ?? throw new AccesoDenegadoException();
        return new AdministradorAutorizado(usuario.IdUsuario, id);
    }
}

public sealed class AccesoDenegadoException(string message = "No tienes permiso.") : Exception(message);
