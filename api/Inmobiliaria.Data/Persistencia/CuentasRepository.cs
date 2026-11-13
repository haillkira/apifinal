using System.Data;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Microsoft.EntityFrameworkCore;
#if DATABASE_FIRST_GENERATED
using Inmobiliaria.Data.Entities.DatabaseFirst;
#else
using Inmobiliaria.Data.Entities;
#endif

namespace Inmobiliaria.Data.Persistencia;

internal sealed class CuentasRepository(ContextoDatos db) : ICuentasRepository
{
    public Task<Cuenta?> BuscarPorId(int id) => ProyectarCuentas(db.Usuarios.Where(u => u.IdUsuario == id))
        .SingleOrDefaultAsync();

    public Task<Cuenta?> BuscarPorCorreo(string correo) => ProyectarCuentas(db.Usuarios.Where(u => u.Correo == correo))
        .SingleOrDefaultAsync();

    public Task<bool> EstaSuspendida(int id, DateTime ahora) => db.Suspensiones.AnyAsync(s =>
        s.IdUsuario == id && s.Activa && s.FechaInicio <= ahora && (s.FechaFin == null || s.FechaFin > ahora));

    public Task<int?> BuscarVendedorAprobado(int idUsuario) => db.Vendedores
        .Where(v => v.IdUsuario == idUsuario && v.CuentaAprobada && v.EstadoAprobacion == "Aprobado")
        .Select(v => (int?)v.IdVendedor).SingleOrDefaultAsync();

    public Task<int?> BuscarAdministradorAutorizado(int idUsuario) => db.Administradores
        .Where(a => a.IdUsuario == idUsuario && (a.NivelPermiso == "Completo" || a.NivelPermiso == "Moderador"))
        .Select(a => (int?)a.IdAdministrador).SingleOrDefaultAsync();

    public async Task<(EstadoRegistro Estado, int IdUsuario)> Registrar(NuevaCuenta cuenta, DateTime ahora)
    {
        if (await db.Usuarios.AnyAsync(u => u.Correo == cuenta.Correo) || await DocumentoExiste(cuenta.Cedula!))
            return (EstadoRegistro.Duplicado, 0);

        var rol = await db.Roles.SingleOrDefaultAsync(r => r.Nombre == cuenta.Rol);
        if (rol is null)
            return (EstadoRegistro.SinRol, 0);

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await DocumentoExiste(cuenta.Cedula!))
            return (EstadoRegistro.DocumentoDuplicado, 0);

        var usuario = NuevoUsuario(cuenta, rol.IdRol, ahora);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        if (cuenta.Rol == "Comprador")
            db.Compradores.Add(new Compradores { IdUsuario = usuario.IdUsuario, Cedula = cuenta.Cedula!, CuentaAprobada = false });
        else
            db.Vendedores.Add(new Vendedores
            {
                IdUsuario = usuario.IdUsuario,
                Cedula = cuenta.Cedula!,
                TipoVendedor = "Particular",
                EstadoAprobacion = "Pendiente",
                FechaSolicitud = ahora
            });

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return (EstadoRegistro.Creado, usuario.IdUsuario);
    }

    public async Task ActualizarUltimoAcceso(int id, DateTime ahora)
    {
        var usuario = await db.Usuarios.SingleAsync(u => u.IdUsuario == id);
        usuario.UltimoAcceso = ahora;
        await db.SaveChangesAsync();
    }

    public Task<List<VendedorPendienteDto>> Pendientes() => db.Vendedores.AsNoTracking()
        .Where(v => v.EstadoAprobacion == "Pendiente")
        .Select(v => new VendedorPendienteDto(v.IdVendedor, v.IdUsuario, v.FechaSolicitud)).ToListAsync();

    public async Task<EstadoDecision> Decidir(int idVendedor, AdministradorAutorizado admin, bool aprobar,
        string? motivo, DateTime ahora)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var vendedor = await db.Vendedores.FindAsync(idVendedor);
        if (vendedor is null)
            return EstadoDecision.NoEncontrado;
        if (vendedor.EstadoAprobacion != "Pendiente")
            return EstadoDecision.YaDecidido;

        var estado = aprobar ? "Aprobado" : "Rechazado";
        vendedor.EstadoAprobacion = estado;
        vendedor.CuentaAprobada = aprobar;
        vendedor.FechaAprobacion = aprobar ? ahora : null;
        vendedor.VerificadoPor = admin.IdUsuario;
        vendedor.MotivoRechazo = aprobar ? null : motivo?.Trim();
        db.HistorialAprobaciones.Add(new HistorialAprobaciones
        {
            IdVendedor = idVendedor, IdAdministrador = admin.IdAdministrador, Accion = estado,
            Comentario = motivo, FechaAccion = ahora
        });
        db.Notificaciones.Add(new Notificaciones
        {
            IdUsuarioDestino = vendedor.IdUsuario, TipoNotificacion = "AprobacionVendedor",
            Titulo = "Decision de cuenta", Mensaje = estado, FechaCreacion = ahora
        });
        db.HistorialAcciones.Add(new HistorialAcciones
        {
            IdUsuario = admin.IdUsuario, Accion = estado, TablaAfectada = "Vendedores",
            IdRegistro = idVendedor, FechaAccion = ahora
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return EstadoDecision.Guardada;
    }

    public async Task<bool> CrearPrimerAdministrador(NuevaCuenta cuenta, DateTime ahora)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await db.Administradores.AnyAsync())
            return false;

        var rol = await db.Roles.SingleAsync(r => r.Nombre == cuenta.Rol);
        var usuario = NuevoUsuario(cuenta, rol.IdRol, ahora);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        db.Administradores.Add(new Administradores { IdUsuario = usuario.IdUsuario, NivelPermiso = "Completo" });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }

    private IQueryable<Cuenta> ProyectarCuentas(IQueryable<Usuarios> usuarios) =>
        from usuario in usuarios.AsNoTracking()
        join rol in db.Roles on usuario.IdRol equals rol.IdRol
        select new Cuenta(new UsuarioResponse(usuario.IdUsuario, usuario.Nombre, usuario.Apellido, rol.Nombre),
            usuario.EstadoCuenta, usuario.PasswordHash);

    private async Task<bool> DocumentoExiste(string cedula) => await db.Compradores.AnyAsync(c => c.Cedula == cedula)
        || await db.Vendedores.AnyAsync(v => v.Cedula == cedula);

    private static Usuarios NuevoUsuario(NuevaCuenta cuenta, int idRol, DateTime ahora) => new()
    {
        IdRol = idRol,
        Nombre = cuenta.Nombre,
        Apellido = cuenta.Apellido,
        Correo = cuenta.Correo,
        Telefono = cuenta.Telefono,
        PasswordHash = cuenta.PasswordHash,
        EstadoCuenta = "Activa",
        FechaRegistro = ahora
    };
}
