using System.Security.Claims;
using Aplicada1.Core;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;

namespace Inmobiliaria.Application;

public sealed class CuentasService(ICuentasRepository cuentas, IClavesCuenta claves, AccesoService acceso)
{
    public async Task<Result<UsuarioResponse>> Registrar(RegistroRequest solicitud, string rol)
    {
        var cuenta = new NuevaCuenta(NormalizarCorreo(solicitud.Correo), solicitud.Nombre.Trim(),
            solicitud.Apellido.Trim(), solicitud.Telefono.Trim(), claves.Hashear(solicitud.Password), rol, solicitud.Cedula);
        var (estado, id) = await cuentas.Registrar(cuenta, DateTime.UtcNow);
        return estado switch
        {
            EstadoRegistro.Creado => Result.Success(new UsuarioResponse(id, cuenta.Nombre, cuenta.Apellido, rol)),
            EstadoRegistro.SinRol => Result.Failure<UsuarioResponse>(new Error("SIN_ROL", "Ejecuta los catalogos SQL antes de registrar usuarios.")),
            EstadoRegistro.DocumentoDuplicado => Result.Failure<UsuarioResponse>(new Error("DUPLICADO", "Documento ya registrado.")),
            _ => Result.Failure<UsuarioResponse>(new Error("DUPLICADO", "Correo o documento ya registrado."))
        };
    }

    public async Task<UsuarioResponse?> IniciarSesion(LoginRequest solicitud)
    {
        var cuenta = await cuentas.BuscarPorCorreo(NormalizarCorreo(solicitud.Correo));
        if (cuenta is null || cuenta.Estado != "Activa" || !claves.Verificar(cuenta.PasswordHash, solicitud.Password))
            return null;

        var id = cuenta.Usuario.IdUsuario;
        if (await cuentas.EstaSuspendida(id, DateTime.UtcNow))
            return null;

        await cuentas.ActualizarUltimoAcceso(id, DateTime.UtcNow);
        return cuenta.Usuario;
    }

    public async Task<List<VendedorPendienteDto>> Pendientes(ClaimsPrincipal principal)
    {
        await acceso.Administrador(principal);
        return await cuentas.Pendientes();
    }

    public async Task<Result> Decidir(ClaimsPrincipal principal, int id, bool aprobar, string? motivo)
    {
        var admin = await acceso.Administrador(principal);
        if (!aprobar && string.IsNullOrWhiteSpace(motivo))
            return Result.Failure(new Error("MOTIVO_REQUERIDO", "Indica el motivo."));

        var estado = await cuentas.Decidir(id, admin, aprobar, motivo, DateTime.UtcNow);
        return estado switch
        {
            EstadoDecision.Guardada => Result.Success(),
            EstadoDecision.NoEncontrado => Result.Failure(new Error("NO_ENCONTRADO", "Vendedor no encontrado.")),
            _ => Result.Failure(new Error("YA_DECIDIDO", "La decision ya fue tomada."))
        };
    }

    public Task<bool> CrearPrimerAdministrador(string correo, string password)
    {
        var cuenta = new NuevaCuenta(NormalizarCorreo(correo), "Administrador", "Equipo", "",
            claves.Hashear(password), "Administrador");
        return cuentas.CrearPrimerAdministrador(cuenta, DateTime.UtcNow);
    }

    private static string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();
}
