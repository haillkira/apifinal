using Inmobiliaria.Contracts;

namespace Inmobiliaria.Application.Persistencia;

public interface ICuentasRepository
{
    Task<Cuenta?> BuscarPorId(int id);
    Task<Cuenta?> BuscarPorCorreo(string correo);
    Task<bool> EstaSuspendida(int id, DateTime ahora);
    Task<int?> BuscarVendedorAprobado(int idUsuario);
    Task<int?> BuscarAdministradorAutorizado(int idUsuario);
    Task<(EstadoRegistro Estado, int IdUsuario)> Registrar(NuevaCuenta cuenta, DateTime ahora);
    Task ActualizarUltimoAcceso(int id, DateTime ahora);
    Task<List<VendedorPendienteDto>> Pendientes();
    Task<EstadoDecision> Decidir(int idVendedor, AdministradorAutorizado admin, bool aprobar, string? motivo, DateTime ahora);
    Task<bool> CrearPrimerAdministrador(NuevaCuenta cuenta, DateTime ahora);
}

public interface IClavesCuenta
{
    string Hashear(string password);
    bool Verificar(string hash, string password);
}

public sealed record Cuenta(UsuarioResponse Usuario, string Estado, string PasswordHash);
public sealed record NuevaCuenta(string Correo, string Nombre, string Apellido, string Telefono, string PasswordHash, string Rol, string? Cedula = null);
public sealed record AdministradorAutorizado(int IdUsuario, int IdAdministrador);
public sealed record VendedorPendienteDto(int IdVendedor, int IdUsuario, DateTime FechaSolicitud);
public enum EstadoRegistro { Creado, Duplicado, DocumentoDuplicado, SinRol }
public enum EstadoDecision { Guardada, NoEncontrado, YaDecidido }
