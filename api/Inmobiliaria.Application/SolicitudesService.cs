using System.Security.Claims;
using Aplicada1.Core;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
namespace Inmobiliaria.Application;

public sealed class SolicitudesService(ISolicitudesRepository solicitudes, AccesoService acceso)
{
    public async Task<string?> Responder(ClaimsPrincipal principal, int id, ResponderSolicitudRequest request)
    {
        var vendedor = await acceso.Vendedor(principal);
        if ((!request.Aceptar && string.IsNullOrWhiteSpace(request.Motivo)) || request.Motivo?.Length > 1000) return "MOTIVO_INVALIDO";
        return await solicitudes.Responder(vendedor, id, request.Aceptar, request.Motivo?.Trim());
    }
    public async Task<Result<int>> Crear(ClaimsPrincipal principal, SolicitudAlquilerRequest solicitud)
    {
        var usuario = await acceso.Usuario(principal);
        if (usuario.Rol != "Comprador") throw new AccesoDenegadoException();
        if (solicitud.IdPublicacion < 1 || solicitud.Mensaje?.Length > 2000)
            return Result.Failure<int>(new Error("DATOS_INVALIDOS", "Revisa la publicación y el mensaje (máximo 2000 caracteres)."));
        var (estado, id) = await solicitudes.Crear(usuario.IdUsuario,
            solicitud with { Mensaje = solicitud.Mensaje?.Trim() }, DateTime.UtcNow);
        return estado switch
        {
            EstadoSolicitudCreada.Creada => Result.Success(id),
            EstadoSolicitudCreada.Duplicada => Result.Failure<int>(new Error("DUPLICADA", "Ya tienes una solicitud activa para esta publicación.")),
            EstadoSolicitudCreada.SinComprador => throw new AccesoDenegadoException(),
            _ => Result.Failure<int>(new Error("NO_DISPONIBLE", "Esta publicación ya no está disponible."))
        };
    }

    public async Task<List<SolicitudDto>> Listar(ClaimsPrincipal principal)
    {
        var usuario = await acceso.Usuario(principal);
        if (usuario.Rol is not ("Comprador" or "Vendedor")) throw new AccesoDenegadoException();
        if (usuario.Rol == "Vendedor") await acceso.Vendedor(principal);
        return await solicitudes.Listar(usuario.IdUsuario, usuario.Rol == "Vendedor");
    }
}
